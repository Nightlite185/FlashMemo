using FlashMemo.Model.Persistence;

namespace FlashMemo.Services;

public class DomainEventBus: IDomainEventBus
{
    public event Func<DomainChangedArgs, Task>? DomainChanged;

    public async Task Notify(DomainChangedArgs args)
    {
        if (DomainChanged is not null)
            DomainChanged?.Invoke(args);
    }
}

public abstract record DomainChangedArgs;

public record CardsDeletedArgs(IEnumerable<long> CardIds): DomainChangedArgs;
public record CardsRelocatedArgs(IEnumerable<long> CardIds, long NewDeckId): DomainChangedArgs;
public record NoteModifiedArgs(CardEntity UpdatedCard): DomainChangedArgs;
public record CardsRescheduledArgs(IEnumerable<CardEntity> Cards): DomainChangedArgs; //* "Forget" action counts as reschedule
public record CardsSuspendBuryArgs(IEnumerable<CardEntity> Cards): DomainChangedArgs; //* both suspend and bury count here
