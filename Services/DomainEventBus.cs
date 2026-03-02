namespace FlashMemo.Services;

public class DomainEventBus: IDomainEventBus
{
    public event Func<Task>? DomainChanged;

    public async Task Notify()
    {
        if (DomainChanged is not null)
            await DomainChanged.Invoke();
    }
}