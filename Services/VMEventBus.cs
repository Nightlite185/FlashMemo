namespace FlashMemo.Services;

public class VMEventBus: IVMEventBus
{
    public event Action? DomainChanged;
    public event Action? UserOptionsChanged;
    public event Action? DeckOptionsChanged;

    public void NotifyDomain() => DomainChanged?.Invoke();
    public void NotifyUserOpt() => UserOptionsChanged?.Invoke();
    public void NotifyDeckOpt() => DeckOptionsChanged?.Invoke();
}