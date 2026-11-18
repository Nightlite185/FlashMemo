namespace FlashMemo.Services;

public sealed class WpfDialogService : IDialogService
{
    public DialogResult Show(string title, string message, DialogButtons buttons, DialogIcons icon)
        => DialogService.Show(title, message, buttons, icon);

    public DialogResult ShowError(string message, string title = "Error", DialogButtons buttons = DialogButtons.OK)
        => DialogService.ShowError(message, title, buttons);

    public DialogResult ShowInfo(string message, string title = "Info", DialogButtons buttons = DialogButtons.OK)
        => DialogService.ShowInfo(message, title, buttons);

    public DialogResult ShowWarning(string message, string title = "Warning", DialogButtons buttons = DialogButtons.OK)
        => DialogService.ShowWarning(message, title, buttons);

    public void ShowNoCardsFoundToExport() => DialogService.ShowNoCardsFoundToExport();
}
