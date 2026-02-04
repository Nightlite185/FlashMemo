using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class DeckOptionsRepo(IDbContextFactory<AppDbContext> dbFactory, IMapper mapper)
    : DbDependentClass(dbFactory), IDeckOptionsRepo
{
    private readonly IMapper mapper = mapper;

    public async Task<IEnumerable<DeckOptions>> GetAllFromUser(long userId)
    {
        var db = GetDb;

        var options = await db.DeckOptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync();

        return options.Select(mapper.Map<DeckOptions>);
    }
    public async Task Remove(long presetId)
    {
        //* id -1 here means that its default preset
        if (presetId == -1) throw new InvalidOperationException(
            "Cannot delete default deck options preset.");

        var db = GetDb;

        //* replacing all references to removed preset's id with -1 (default one).
        await db.Decks
            .Where(d => d.OptionsId == presetId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(d => d.OptionsId, -1));
        
        //* then removing the preset, after there are no more decks pointing to it.
        await db.DeckOptions
            .Where(x => x.Id == presetId)
            .ExecuteDeleteAsync();

    }
    public async Task AssignToDecks(IEnumerable<long> deckIds, long newPresetId = -1)
    {
        var db = GetDb;

        await db.Decks
            .Where(d => deckIds.Contains(d.Id))
            .ExecuteUpdateAsync(d => d.SetProperty(o => 
                o.OptionsId, newPresetId));
    }
    public async Task CreateNew(DeckOptions newRecord)
    {
        if (newRecord.Id == -1) throw new InvalidOperationException(
            @"Cannot add new deck options preset with id -1,
            as it's reserved for the default preset only.");

        var db = GetDb;

        var newEntity = new DeckOptionsEntity();
        newEntity.NewOwnedTypes();

        mapper.Map(newRecord, newEntity);

        await db.DeckOptions.AddAsync(newEntity);
        await db.SaveChangesAsync();
    }
    public async Task SaveEditedPreset(DeckOptions updatedRecord)
    {
        if (updatedRecord.Id == -1) throw new InvalidOperationException(
            @"Cannot edit deck options with id -1,
            as default preset is read-only");

        var db = GetDb;

        var tracked = await db.DeckOptions
            .SingleAsync(o => o.Id == updatedRecord.Id);

        tracked.NewOwnedTypes();

        mapper.Map(updatedRecord, tracked);
        await db.SaveChangesAsync();
    }
}