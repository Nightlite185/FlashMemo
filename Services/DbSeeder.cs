using System.IO;
using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class DbSeeder(AppDbContext db, IMapper m, SafetyNet net)
{
    private readonly AppDbContext db = db;
    private readonly IMapper mapper = m;
    private readonly string dbDirectory = AppDbContext.DbDirPath;

    internal DbSeeder(AppDbContext db, IMapper mapper, SafetyNet net, string dbDirectory)
        : this(db, mapper, net)
    {
        this.dbDirectory = dbDirectory;
    }

    /// <summary> This method already handles its own failures, do not wrap it. </summary>
    public Task<bool> TrySeedAsync() => net.TryUnsafe(
        action: async () =>
        {
            Directory.CreateDirectory(dbDirectory);
            await db.Database.MigrateAsync();

            await SeedDeckOptions();
            await SeedAppSessionData();
        },
        actionTried: "initialize the database. FlashMemo cannot start without its database and will close.",
        retry: false);

    private async Task SeedDeckOptions()
    {
        if (await db.DeckOptions.AnyAsync(d => d.Id == -1))
            return;

        var entity = mapper
            .Map<DeckOptionsEntity>(DeckOptions.Default);

        await db.DeckOptions.AddAsync(entity);
        await db.SaveChangesAsync();
    }
    private async Task SeedAppSessionData()
    {
        if (await db.AppSessionData.AnyAsync())
            return;

        await db.AppSessionData.AddAsync(
            new AppSessionData() { Id = -1 });

        await db.SaveChangesAsync();
    }
}