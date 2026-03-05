using System.Windows;

namespace FlashMemo.Services;

public enum DialogButtons
{   
    OK, OKCancel,
    AbortRetryIgnore, YesNoCancel, 
    YesNo, RetryCancel, 
    CancelTryContinue
}
public enum DialogIcons
{
    None, Error,
    Hand, Stop,
    Question, Exclamation,
    Warning, Asterisk,
    Information
}
public enum DialogResult
{
    None, OK,
    Cancel, Abort, 
    Retry, Ignore, 
    Yes, No,
    TryAgain, Continue
}

public static class DialogService
{
    private static MessageBoxButton ConvertButtonType(DialogButtons db)
    {
        return db switch
        {
            DialogButtons.OK => MessageBoxButton.OK,
            DialogButtons.OKCancel => MessageBoxButton.OKCancel,
            DialogButtons.YesNoCancel => MessageBoxButton.YesNoCancel,
            DialogButtons.YesNo => MessageBoxButton.YesNo,
            DialogButtons.AbortRetryIgnore => MessageBoxButton.AbortRetryIgnore,
            DialogButtons.RetryCancel => MessageBoxButton.RetryCancel,
            DialogButtons.CancelTryContinue => MessageBoxButton.CancelTryContinue,

            _ => throw new ArgumentOutOfRangeException(nameof(db), db, null)
        };
    }
    private static MessageBoxImage ConvertIconType(DialogIcons di)
    {
        return di switch
        {
            DialogIcons.None => MessageBoxImage.None,
            DialogIcons.Error => MessageBoxImage.Error,
            DialogIcons.Hand => MessageBoxImage.Hand,
            DialogIcons.Stop => MessageBoxImage.Stop,
            DialogIcons.Question => MessageBoxImage.Question,
            DialogIcons.Exclamation => MessageBoxImage.Exclamation,
            DialogIcons.Warning => MessageBoxImage.Warning,
            DialogIcons.Asterisk => MessageBoxImage.Asterisk,
            DialogIcons.Information => MessageBoxImage.Information,

            _ => throw new ArgumentOutOfRangeException(nameof(di), di, null)
        };
    }
    private static DialogResult ConvertResultType(MessageBoxResult result)
    {
        return result switch
        {
            MessageBoxResult.None => DialogResult.None,
            MessageBoxResult.OK => DialogResult.OK,
            MessageBoxResult.Cancel => DialogResult.Cancel,
            MessageBoxResult.Abort => DialogResult.Abort,
            MessageBoxResult.Retry => DialogResult.Retry,
            MessageBoxResult.Ignore => DialogResult.Ignore,
            MessageBoxResult.Yes => DialogResult.Yes,
            MessageBoxResult.No => DialogResult.No,
            MessageBoxResult.TryAgain => DialogResult.TryAgain,
            MessageBoxResult.Continue => DialogResult.Continue,

            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }
    public static DialogResult Show(string title, string message,
                    DialogButtons buttons, DialogIcons icon)
    {
        var btn = ConvertButtonType(buttons);
        var img = ConvertIconType(icon);

        var result = MessageBox.Show(message, title, btn, img);

        return ConvertResultType(result);
    }
} 
