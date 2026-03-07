using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlashMemo.ViewModel.Popups;

namespace FlashMemo.View;

public partial class RescheduleUC : UserControl
{
    public RescheduleUC()
    {
        InitializeComponent();
    }

    private void Root_Loaded(object sender, RoutedEventArgs e)
        => FocusActiveInput();

    private async void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not RescheduleVM vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        if (vm.ConfirmCommand.CanExecute(null))
            await vm.ConfirmCommand.ExecuteAsync(null);

        e.Handled = true;
    }

    private void DateOption_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is RescheduleVM vm)
            vm.DatepickerActive = true;

        FocusActiveInput();
    }

    private void PostponeOption_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is RescheduleVM vm)
            vm.DatepickerActive = false;

        FocusActiveInput();
    }

    private void FocusActiveInput()
    {
        if (DataContext is not RescheduleVM vm)
            return;

        Dispatcher.BeginInvoke(() =>
        {
            if (vm.DatepickerActive)
            {
                DatePickerControl.Focus();
                Keyboard.Focus(DatePickerControl);
                return;
            }

            PostponeByDaysControl.Focus();
            Keyboard.Focus(PostponeByDaysControl);
        });
    }
}
