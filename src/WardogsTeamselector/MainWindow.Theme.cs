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
    private enum ActionIcon { Info, Stop, Save, Undo, Search, Refresh, Target, Play, Settings, Diagnose, Image, Export, Import, Bug, Link, Shield, Close, Keyboard, Setup, Group, GroupAdd, Join, Leave, Delete, UserRemove, Check, Reject, Copy, Invite, Key, AdminImport, AdminTransfer, Download, Restart, Dialog, Hud }

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
            ActionIcon.Export => "M12,15 L12,3 M7,8 L12,3 17,8 M4,15 L4,21 20,21 20,15",
            ActionIcon.Import => "M12,3 L12,15 M7,10 L12,15 17,10 M4,15 L4,21 20,21 20,15",
            ActionIcon.Group => "M9,3 A3,3 0 1 1 8.99,3 M3,21 L3,18 A6,6 0 0 1 15,18 L15,21 M17,4 A3,3 0 0 1 17,10 M18,13 Q22,14 22,18 L22,21",
            ActionIcon.GroupAdd => "M8,3 A3,3 0 1 1 7.99,3 M2,21 L2,18 A6,6 0 0 1 14,18 L14,21 M18,6 L18,14 M14,10 L22,10",
            ActionIcon.Join => "M14,3 L21,3 21,21 14,21 M3,12 L16,12 M11,7 L16,12 11,17",
            ActionIcon.Leave => "M9,3 L3,3 3,21 9,21 M10,12 L22,12 M17,7 L22,12 17,17",
            ActionIcon.Delete => "M3,6 L21,6 M9,6 L9,3 15,3 15,6 M5,6 L6,21 18,21 19,6 M10,10 L10,17 M14,10 L14,17",
            ActionIcon.UserRemove => "M8,3 A3,3 0 1 1 7.99,3 M2,21 L2,18 A6,6 0 0 1 14,18 L14,21 M16,10 L22,10",
            ActionIcon.Check => "M4,12 L9,17 20,6",
            ActionIcon.Reject => "M12,3 A9,9 0 1 1 11.99,3 M6,6 L18,18",
            ActionIcon.Copy => "M8,8 L21,8 21,21 8,21 Z M16,8 L16,3 3,3 3,16 8,16",
            ActionIcon.Invite => "M10,14 L14,10 M8,16 L6,18 A4,4 0 0 1 0.5,12.5 L5,8 A4,4 0 0 1 10.5,8 M14,8 L16,6 A4,4 0 0 1 21.5,11.5 L18,15 M19,17 L19,23 M16,20 L22,20",
            ActionIcon.Key => "M7,3 A4,4 0 1 1 6.99,3 M10,10 L21,21 M15,15 L18,12 M18,18 L21,15",
            ActionIcon.AdminImport => "M12,2 L21,6 20,14 Q18,20 12,22 Q6,20 4,14 L3,6 Z M12,6 L12,16 M8,12 L12,16 16,12",
            ActionIcon.AdminTransfer => "M12,2 L21,6 20,14 Q18,20 12,22 Q6,20 4,14 L3,6 Z M7,10 L17,10 M14,7 L17,10 14,13 M17,16 L7,16 M10,13 L7,16 10,19",
            ActionIcon.Download => "M12,2 L12,16 M7,11 L12,16 17,11 M3,18 L3,22 21,22 21,18",
            ActionIcon.Restart => "M12,2 L12,11 M6,5 A9,9 0 1 0 18,5",
            ActionIcon.Dialog => "M3,4 L21,4 21,18 8,18 4,22 4,18 3,18 Z M3,8 L21,8 M7,12 L17,12 M7,15 L13,15",
            ActionIcon.Hud => "M3,4 L21,4 21,20 3,20 Z M10,9 L18,9 M10,11.5 L18,11.5 M10,14 L18,14 M10,16.5 L18,16.5",
            ActionIcon.Bug => "M8,9 L16,9 16,16 A4,4 0 0 1 8,16 Z M9,9 L9,6 A3,3 0 0 1 15,6 L15,9 M4,10 L8,12 M20,10 L16,12 M3,16 L8,16 M21,16 L16,16 M5,22 L9,19 M19,22 L15,19",
            ActionIcon.Link => "M14,4 L21,4 21,11 M21,4 L11,14 M10,4 L3,4 3,21 20,21 20,14",
            ActionIcon.Shield => "M12,2 L21,6 20,14 Q18,20 12,22 Q6,20 4,14 L3,6 Z M8,12 L11,15 17,9",
            ActionIcon.Close => "M5,5 L19,19 M19,5 L5,19",
            ActionIcon.Keyboard => "M2,5 L22,5 22,19 2,19 Z M6,9 L7,9 M11,9 L12,9 M16,9 L17,9 M6,13 L7,13 M11,13 L12,13 M16,13 L17,13 M8,16 L16,16",
            ActionIcon.Setup => "M3,4 L21,4 21,17 3,17 Z M8,21 L16,21 M12,17 L12,21",
            _ => throw new ArgumentOutOfRangeException(nameof(icon))
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
        var label when label.StartsWith("Stopp · ", StringComparison.Ordinal) => ActionIcon.Stop,
        "Änderungen verwerfen" or "Standardwerte laden …" => ActionIcon.Undo,
        "Einstellungen speichern" or "Bild speichern" or "Update-Einstellungen speichern" or "Dienstadresse speichern" => ActionIcon.Save,
        "Spiel suchen / Bild laden" => ActionIcon.Search,
        "Aktualisieren" or "Monitore aktualisieren" or "Jetzt auf Updates prüfen" => ActionIcon.Refresh,
        "Update" => ActionIcon.Download,
        "Update installieren und neu starten" => ActionIcon.Restart,
        "Teamfläche übernehmen" or "Spiel / Klickflächen prüfen" or "Übernehmen & in Einrichtung prüfen" => ActionIcon.Target,
        "" or "Speichern & zum Betrieb" or "Einmal beitreten" => ActionIcon.Play,
        "Intervall / Hotkeys ändern" => ActionIcon.Keyboard,
        "Diagnose / Testmodus" => ActionIcon.Diagnose,
        "Dialogreferenz prüfen" => ActionIcon.Dialog,
        "HUD-Referenz prüfen" => ActionIcon.Hud,
        "Live-Bild laden" => ActionIcon.Image,
        "Diagnose exportieren" or "Wiederherstellungscode exportieren" => ActionIcon.Export,
        "Wiederherstellungscode importieren" => ActionIcon.Import,
        "Gruppen verwalten" => ActionIcon.Group,
        "Gruppe erstellen" => ActionIcon.GroupAdd,
        "Gruppe beitreten" => ActionIcon.Join,
        "Gruppe verlassen" => ActionIcon.Leave,
        "Aus Liste entfernen" or "Gruppe löschen" => ActionIcon.Delete,
        "Mitglied entfernen" => ActionIcon.UserRemove,
        "Bestätigen" => ActionIcon.Check,
        "Ablehnen" => ActionIcon.Reject,
        "Abbrechen" => ActionIcon.Close,
        "Einladungslink kopieren" or "Adminzugang kopieren" => ActionIcon.Copy,
        "Neuen Einladungslink erzeugen" => ActionIcon.Invite,
        "Auswahl zurücknehmen" => ActionIcon.Undo,
        "Eigenen Zugangscode ersetzen" => ActionIcon.Key,
        "Adminzugang importieren" => ActionIcon.AdminImport,
        "Adminzugang exklusiv übernehmen" => ActionIcon.AdminTransfer,
        "Fehler auf GitHub melden" => ActionIcon.Bug,
        "Projekt auf GitHub" => ActionIcon.Link,
        "Lizenz: GNU GPL v3.0" => ActionIcon.Shield,
        "Schließen" => ActionIcon.Close,
        _ => throw new ArgumentException("Kein Icon für diese Aktion hinterlegt: " + text, nameof(text))
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
