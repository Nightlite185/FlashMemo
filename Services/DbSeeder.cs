using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class DbSeeder(AppDbContext db, IMapper m)
{
    private readonly AppDbContext db = db;
    private readonly IMapper mapper = m;

    public async Task MigrateAndSeedAsync()
    {
        // await db.Database.MigrateAsync();

        await SeedDeckOptions();
        await SeedLastSessionData();
    }

    private async Task SeedDeckOptions()
    {
        if (await db.DeckOptions.AnyAsync(d => d.Id == -1))
            return;

        var entity = mapper
            .Map<DeckOptionsEntity>(DeckOptions.Default);

        await db.DeckOptions.AddAsync(entity);
        await db.SaveChangesAsync();
    }
    private async Task SeedLastSessionData()
    {
        if (await db.LastSessionData.AnyAsync())
            return;

        await db.LastSessionData.AddAsync(
            new LastSessionData() { Id = -1 });

        await db.SaveChangesAsync();
    }

    private async Task SeedUserOptions()
    {
        throw new NotImplementedException();
    }
}