using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlashMemo.ViewModel.Popups;

namespace FlashMemo.Helpers;

public static class PopupInputBehavior
{
    public static readonly DependencyProperty EnableCreateDeckNameBehaviorProperty =
        DependencyProperty.RegisterAttached(
            "EnableCreateDeckNameBehavior",
            typeof(bool),
            typeof(PopupInputBehavior),
            new PropertyMetadata(false, OnEnableCreateDeckNameBehaviorChanged));

    public static bool GetEnableCreateDeckNameBehavior(DependencyObject obj)
        => (bool)obj.GetValue(EnableCreateDeckNameBehaviorProperty);

    public static void SetEnableCreateDeckNameBehavior(DependencyObject obj, bool value)
        => obj.SetValue(EnableCreateDeckNameBehaviorProperty, value);

    private static void OnEnableCreateDeckNameBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb)
            return;

        if ((bool)e.NewValue)
        {
            tb.KeyDown += CreateDeckNameBox_KeyDown;
            tb.IsVisibleChanged += CreateDeckNameBox_IsVisibleChanged;
            tb.Loaded += CreateDeckNameBox_Loaded;
        }
        else
        {
            tb.KeyDown -= CreateDeckNameBox_KeyDown;
            tb.IsVisibleChanged -= CreateDeckNameBox_IsVisibleChanged;
            tb.Loaded -= CreateDeckNameBox_Loaded;
        }
    }

    private static async void CreateDeckNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb
        || tb.DataContext is not CreateDeckVM vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        if (string.IsNullOrWhiteSpace(vm.NameField))
            return;

        else if (vm.ConfirmCommand.CanExecute(null))
            await vm.ConfirmCommand.ExecuteAsync(null);

        e.Handled = true;
    }

    private static void CreateDeckNameBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox tb || !tb.IsVisible)
            return;

        FocusDeckNameBox(tb);
    }

    private static void CreateDeckNameBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || !tb.IsVisible)
            return;

        FocusDeckNameBox(tb);
    }

    private static void FocusDeckNameBox(TextBox tb)
    {
        tb.Dispatcher.BeginInvoke(() =>
        {
            tb.Focus();
            Keyboard.Focus(tb);
        });
    }
}
