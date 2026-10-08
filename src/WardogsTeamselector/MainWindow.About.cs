using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WardogsTeamselector.Updates;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    private Window? aboutWindow;

    private void ShowAboutWindow()
    {
        if (aboutWindow == null)
        {
            aboutWindow = BuildAboutWindow();
            aboutWindow.Closed += (_, _) => aboutWindow = null;
            aboutWindow.Show();
        }
        aboutWindow.Activate();
    }

    private static string ProjectUrl => "https://github.com/" + ReleaseUpdate.Repository;
    private static string BuildDescription => CurrentBuild > 0 ? $"Release-Build {CurrentBuild}" : "Lokaler Build";
    private static string VariantDescription => Metadata("UpdateVariant") switch
    {
        "with-runtime" => "mit Runtime",
        "without-runtime" => "ohne Runtime",
        _ => "Entwicklungsbuild"
    };

    private Window BuildAboutWindow()
    {
        var dialog = new Window
        {
            Title = "Über WardogsTeamselector", Owner = this, Icon = Icon,
            Width = 600, Height = 600, MinWidth = 440, MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Background, Foreground = Foreground, FontFamily = FontFamily, FontSize = FontSize
        };
        dialog.Resources.MergedDictionaries.Add(CreateTheme());
        dialog.UseLayoutRounding = true;
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(PageTitle("WardogsTeamselector", "Ein quelloffenes Windows-Tool zur Teamauswahl in Wardogs."));
        panel.Children.Add(Hint($"{BuildDescription} · {VariantDescription}"));
        panel.Children.Add(Heading("Herkunft & Lizenz"));
        panel.Children.Add(Hint("Projekt von Gamewalker. Quellcode, Änderungen und Downloads werden auf GitHub veröffentlicht."));
        panel.Children.Add(Hint(ProjectUrl));
        var links = new WrapPanel();
        links.Children.Add(Button("Projekt auf GitHub", () => OpenProjectLink(ProjectUrl)));
        links.Children.Add(Button("Lizenz: GNU GPL v3.0", () => OpenProjectLink(ProjectUrl + "/blob/main/LICENSE")));
        panel.Children.Add(links);
        panel.Children.Add(Heading("Fehler melden"));
        panel.Children.Add(Hint("Beschreibe die Schritte zum Fehler, das erwartete Ergebnis und was tatsächlich passiert. Ergänze möglichst den Statusgrund und einen Diagnoseexport aus dem Bereich Diagnose."));
        panel.Children.Add(Hint("Der Button öffnet ein vorbereitetes GitHub-Issue im Browser. Prüfe den Text und sende ihn dort ab; eine GitHub-Anmeldung ist erforderlich. Diagnose und Bilder kannst du selbst anhängen."));
        panel.Children.Add(Button("Fehler auf GitHub melden", () => OpenProjectLink(BugReportUrl())));
        var close = Button("Schließen", () => dialog.Close());
        close.IsCancel = true;
        close.HorizontalAlignment = HorizontalAlignment.Right;
        panel.Children.Add(close);
        dialog.Content = Scroll(panel);
        return dialog;
    }

    private static string BugReportUrl()
    {
        string body = $"## Fehlerbeschreibung\n\n## Schritte zum Reproduzieren\n1. \n\n## Erwartetes Ergebnis\n\n## Tatsächliches Ergebnis\n\n## App und System\n- App: WardogsTeamselector\n- Build: {BuildDescription}\n- Variante: {VariantDescription}\n- Windows: {Environment.OSVersion.VersionString}\n\n## Statusgrund und Diagnose\nBitte Statusgrund ergänzen und bei Bedarf einen Diagnoseexport oder Screenshot anhängen.\n";
        return ProjectUrl + "/issues/new?title=" + Uri.EscapeDataString("Fehler: ") + "&body=" + Uri.EscapeDataString(body);
    }

    private void OpenProjectLink(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose(); }
        catch { MessageBox.Show(this, "Der Browser konnte nicht geöffnet werden. Öffne diesen Link manuell:\n\n" + url, "GitHub-Link", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
}
