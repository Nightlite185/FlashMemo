using System.Windows;

namespace FlashMemo.Helpers;

public static class TreeViewSelection
{
    public static readonly DependencyProperty IsMultiSelectedProperty =
        DependencyProperty.RegisterAttached(
            "IsMultiSelected",
            typeof(bool),
            typeof(TreeViewSelection),
            new FrameworkPropertyMetadata(false));

    public static bool GetIsMultiSelected(DependencyObject element)
        => (bool)element.GetValue(IsMultiSelectedProperty);

    public static void SetIsMultiSelected(DependencyObject element, bool value)
        => element.SetValue(IsMultiSelectedProperty, value);
}
