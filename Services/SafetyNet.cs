using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Services;

public class SafetyNet(ILogger<SafetyNet> logger, IDialogService dialogs)
{
    public async Task<bool> TryUnsafe(
        Func<Task> action, string actionTried, bool retry,
        [CallerMemberName] string caller = "",
        [CallerArgumentExpression(nameof(action))] string operation = "")
    {
        var result = await TryUnsafe(
            async () =>
            {
                await action();
                return ValueTuple.Create();
            },
            actionTried, retry,
            caller, operation);

        return result.Success;
    }

    public async Task<UnsafeActionResult<T>> TryUnsafe<T>(
        Func<Task<T>> action, string actionTried, bool retry,
        [CallerMemberName] string caller = "",
        [CallerArgumentExpression(nameof(action))] string operation = "")
    {
        DialogResult ans = DialogResult.None;
        bool firstPass = true;

        while (firstPass || ans is DialogResult.Retry or DialogResult.TryAgain)
        {
            try
            {
                var res = await action();
                return new(true, res);
            }

            catch (Exception e)
            {
                logger.LogError(e,
                    message: "{Caller}: {Operation} failed ({Attempt})",
                    args: [caller, operation, firstPass ? "initial attempt" : "retry"]);

                ans = dialogs.ShowError(
                    message: "Something went wrong while trying to " + actionTried,
                    buttons: retry ? DialogButtons.RetryCancel : DialogButtons.OK);
            }

            firstPass = false;
        }

        return new(Success: false);
    }
}

public record struct UnsafeActionResult<T>(
    bool Success,
    T Value = default!); // mute nullable warning since value prop should be only accessed when success == true.
                         // So its their fault for accessing it with success == false.
                         // nullable T would only cause unnecessary safety checks.
