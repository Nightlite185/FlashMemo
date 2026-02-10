using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public class AreYouSureVM(Action cancel, Func<Task> confirmFunc): PopupVMBase(cancel)
{
    private readonly Func<Task> confirm = confirmFunc;
    public override async Task Confirm()
    {
        await confirm();
        Close();
    }
}