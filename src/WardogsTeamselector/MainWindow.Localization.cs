using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    private readonly ComboBox languageSelector = new();
    private readonly ConditionalWeakTable<DependencyObject, Dictionary<DependencyProperty, LocalizedValue>> localizedValues = new();
    private bool localizing;
    private sealed record LocalizedValue(string Source, string Display);

    private UIElement BuildLanguageSelector()
    {
        languageSelector.ItemsSource = Localization.Languages;
        languageSelector.DisplayMemberPath = "Name";
        languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == Localization.CurrentLanguage);
        languageSelector.Width = 155;
        languageSelector.MinHeight = 36;
        languageSelector.Margin = new Thickness(12, 0, 0, 4);
        languageSelector.Foreground = Brushes.Black;
        languageSelector.ToolTip = "Language";
        AutomationProperties.SetName(languageSelector, "Language");
        languageSelector.SelectionChanged += (_, _) =>
        {
            if (languageSelector.SelectedItem is not Localization.Language language) return;
            Localization.SetLanguage(language.Code);
            LocalizeInterface();
            if (!smokeMode)
            {
                try { Localization.SavePreference(language.Code); }
                catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
                { ShowError(Localization.Text("Language preference could not be saved.")); }
            }
        };
        return languageSelector;
    }

    // Retain original text independently of displayed text, including inactive tabs.
    // This avoids rebuilding controls, losing drafts or stopping automation on a switch.
    private void LocalizeInterface(Window? additionalWindow = null)
    {
        if (localizing) return;
        localizing = true;
        try
        {
            var visited = new HashSet<DependencyObject>();
            Visit(this);
            if (aboutWindow != null) Visit(aboutWindow);
            if (additionalWindow != null) Visit(additionalWindow);
            void Visit(DependencyObject element)
            {
                if (!visited.Add(element)) return;
                LocalizeProperty(element, FrameworkElement.ToolTipProperty);
                LocalizeProperty(element, AutomationProperties.NameProperty);
                // Language names are always shown in their own language.
                if (element == languageSelector) return;
                if (element is TextBlock text)
                {
                    LocalizeProperty(text, TextBlock.TextProperty);
                    text.FlowDirection = Localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                }
                if (element is TextBox box && box.IsReadOnly) LocalizeProperty(box, TextBox.TextProperty);
                if (element is ContentControl) LocalizeProperty(element, ContentControl.ContentProperty);
                if (element is HeaderedContentControl) LocalizeProperty(element, HeaderedContentControl.HeaderProperty);
                if (element is Window window)
                {
                    LocalizeProperty(window, Window.TitleProperty);
                    window.Language = XmlLanguage.GetLanguage(Localization.CurrentLanguage);
                }
                if (element is DataGrid table)
                    foreach (var column in table.Columns) LocalizeProperty(column, DataGridColumn.HeaderProperty);
                foreach (var child in LogicalTreeHelper.GetChildren(element).OfType<DependencyObject>()) Visit(child);
                if (element is Visual)
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) Visit(VisualTreeHelper.GetChild(element, i));
            }
        }
        finally { localizing = false; }
    }
    private void LocalizeProperty(DependencyObject element, DependencyProperty property)
    {
        if (element.GetValue(property) is not string value || value.Length == 0) return;
        var values = localizedValues.GetOrCreateValue(element);
        string source = values.TryGetValue(property, out var previous) && value == previous.Display ? previous.Source : value;
        string display = Localization.Text(source);
        values[property] = new(source, display);
        if (value != display) element.SetCurrentValue(property, display);
    }
}
