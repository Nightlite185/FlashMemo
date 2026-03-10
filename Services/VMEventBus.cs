namespace FlashMemo.Services;

public class VMEventBus: IVMEventBus
{
    public event Action? DomainChanged;
    public event Action? UserOptionsChanged;
    public event Action? DeckOptionsChanged;

    public async void NotifyDomain() => DomainChanged?.Invoke();
    public async void NotifyUserOpt() => UserOptionsChanged?.Invoke();
    public async void NotifyDeckOpt() => DeckOptionsChanged?.Invoke();
}