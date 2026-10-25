namespace FlashMemo.Services;

public interface IDialogService
{
    DialogResult Show(string title, string message, DialogButtons buttons, DialogIcons icon);
    DialogResult ShowError(string message, string title = "Error", DialogButtons buttons = DialogButtons.OK);
    void ShowNoCardsFoundToExport();
}

public sealed class WpfDialogService : IDialogService
{
    public DialogResult Show(string title, string message, DialogButtons buttons, DialogIcons icon)
        => DialogService.Show(title, message, buttons, icon);

    public DialogResult ShowError(string message, string title = "Error", DialogButtons buttons = DialogButtons.OK)
        => DialogService.ShowError(message, title, buttons);

    public void ShowNoCardsFoundToExport() => DialogService.ShowNoCardsFoundToExport();
}
