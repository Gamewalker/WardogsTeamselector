using System.Windows;

namespace WardogsTeamselector;

internal static class LocalizedMessageBox
{
    internal static MessageBoxResult Show(Window owner, string text, string caption,
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None) =>
        MessageBox.Show(owner, Localization.Text(text), Localization.Text(caption), buttons, icon);
    internal static MessageBoxResult Show(string text, string caption) =>
        MessageBox.Show(Localization.Text(text), Localization.Text(caption));
}
