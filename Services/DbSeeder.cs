using System.IO;
using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class DbSeeder(AppDbContext db, IMapper m)
{
    private readonly AppDbContext db = db;
    private readonly IMapper mapper = m;
    private readonly string dbDirectory = AppDbContext.DbDirPath;

    internal DbSeeder(AppDbContext db, IMapper mapper, string dbDirectory)
        : this(db, mapper)
    {
        this.dbDirectory = dbDirectory;
    }

    public async Task SeedAsync()
    {
        Directory.CreateDirectory(dbDirectory);
        await db.Database.MigrateAsync();

        await SeedDeckOptions();
        await SeedAppSessionData();
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
    private async Task SeedAppSessionData()
    {
        if (await db.AppSessionData.AnyAsync())
            return;

        await db.AppSessionData.AddAsync(
            new AppSessionData() { Id = -1 });

        await db.SaveChangesAsync();
    }
}
