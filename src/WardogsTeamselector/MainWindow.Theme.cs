using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    // Small, authored line icons use the same 24-unit grid and stroke throughout.
    private enum ActionIcon { Info, Stop, Save, Undo, Search, Refresh, Target, Play, Settings, Diagnose, Image, Export, Bug, Link, Shield, Close, Keyboard, Setup }

    private static ResourceDictionary CreateTheme() => new()
    {
        Source = new Uri("pack://application:,,,/Theme.xaml")
    };

    private static Path IconPath(ActionIcon icon, double size = 18)
    {
        string data = icon switch
        {
            ActionIcon.Info => "M12,3 A9,9 0 1 1 11.99,3 M12,10 L12,17 M12,7 L12,7.2",
            ActionIcon.Stop => "M5,5 L19,5 19,19 5,19 Z",
            ActionIcon.Save => "M4,3 L17,3 21,7 21,21 3,21 3,3 Z M7,3 L7,9 16,9 16,3 M7,21 L7,14 17,14 17,21",
            ActionIcon.Undo => "M9,5 L4,10 9,15 M4,10 L14,10 C22,10 22,20 14,20",
            ActionIcon.Search => "M10,3 A7,7 0 1 1 9.99,3 M15,15 L21,21",
            ActionIcon.Refresh => "M20,9 A8,8 0 0 0 5,6 M5,2 L5,6 9,6 M4,15 A8,8 0 0 0 19,18 M19,22 L19,18 15,18",
            ActionIcon.Target => "M12,5 A7,7 0 1 1 11.99,5 M12,9 A3,3 0 1 1 11.99,9 M12,2 L12,5 M12,19 L12,22 M2,12 L5,12 M19,12 L22,12",
            ActionIcon.Play => "M7,4 L20,12 7,20 Z",
            ActionIcon.Settings => "M4,6 L20,6 M4,12 L20,12 M4,18 L20,18 M8,3 L8,9 M16,9 L16,15 M10,15 L10,21",
            ActionIcon.Diagnose => "M3,12 L7,12 10,5 14,19 17,12 21,12",
            ActionIcon.Image => "M3,4 L21,4 21,20 3,20 Z M3,16 L9,10 15,16 18,13 21,16 M16,8 L16,8.2",
            ActionIcon.Export => "M12,3 L12,15 M7,10 L12,15 17,10 M4,15 L4,21 20,21 20,15",
            ActionIcon.Bug => "M8,9 L16,9 16,16 A4,4 0 0 1 8,16 Z M9,9 L9,6 A3,3 0 0 1 15,6 L15,9 M4,10 L8,12 M20,10 L16,12 M3,16 L8,16 M21,16 L16,16 M5,22 L9,19 M19,22 L15,19",
            ActionIcon.Link => "M14,4 L21,4 21,11 M21,4 L11,14 M10,4 L3,4 3,21 20,21 20,14",
            ActionIcon.Shield => "M12,2 L21,6 20,14 Q18,20 12,22 Q6,20 4,14 L3,6 Z M8,12 L11,15 17,9",
            ActionIcon.Close => "M5,5 L19,19 M19,5 L5,19",
            ActionIcon.Keyboard => "M2,5 L22,5 22,19 2,19 Z M6,9 L7,9 M11,9 L12,9 M16,9 L17,9 M6,13 L7,13 M11,13 L12,13 M16,13 L17,13 M8,16 L16,16",
            _ => "M3,4 L21,4 21,17 3,17 Z M8,21 L16,21 M12,17 L12,21"
        };
        var path = new Path
        {
            Data = Geometry.Parse(data), Width = size, Height = size, Stretch = Stretch.Uniform,
            StrokeThickness = 1.7, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round, VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false, Focusable = false
        };
        path.SetBinding(Shape.StrokeProperty, new Binding { Path = new PropertyPath(TextElement.ForegroundProperty), RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
        return path;
    }

    private static ActionIcon IconForAction(string text) => text switch
    {
        "Über die App" => ActionIcon.Info,
        "Stopp · ESC" => ActionIcon.Stop,
        "Änderungen verwerfen" or "Standardwerte laden …" => ActionIcon.Undo,
        "Einstellungen speichern" or "Bild speichern" or "Update-Einstellungen speichern" => ActionIcon.Save,
        "Spiel suchen / Bild laden" => ActionIcon.Search,
        "Monitore aktualisieren" or "Jetzt auf Updates prüfen" or "Update installieren und neu starten" => ActionIcon.Refresh,
        "Teamfläche übernehmen" or "Spiel / Klickflächen prüfen" or "Übernehmen & in Einrichtung prüfen" => ActionIcon.Target,
        "Speichern & zum Betrieb" => ActionIcon.Play,
        "Intervall / Hotkeys ändern" => ActionIcon.Keyboard,
        "Diagnose / Testmodus" or "Dialogreferenz prüfen" or "HUD-Referenz prüfen" => ActionIcon.Diagnose,
        "Live-Bild laden" => ActionIcon.Image,
        "Diagnose exportieren" => ActionIcon.Export,
        "Fehler auf GitHub melden" => ActionIcon.Bug,
        "Projekt auf GitHub" => ActionIcon.Link,
        "Lizenz: GNU GPL v3.0" => ActionIcon.Shield,
        "Schließen" => ActionIcon.Close,
        _ => ActionIcon.Settings
    };

    private static StackPanel IconLabel(string text, ActionIcon icon)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var image = IconPath(icon);
        image.Margin = new Thickness(0, 0, 9, 0);
        panel.Children.Add(image);
        panel.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
        return panel;
    }

    private static void UseAccent(Button button)
    {
        button.SetResourceReference(FrameworkElement.StyleProperty, "AccentButton");
    }

    private static Border Section(UIElement content) => new()
    {
        Child = content, Background = SurfaceBrush, BorderBrush = BorderBrushColor,
        BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(18)
    };
}
