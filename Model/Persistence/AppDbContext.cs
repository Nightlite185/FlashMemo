using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Model.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<CardEntity> Cards { get; set; }
        public DbSet<Deck> Decks { get; set; }
        public DbSet<DeckOptionsEntity> DeckOptions { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<CardLog> CardLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            DefineTables(mb);
            ConfigureDeletion(mb);
            DefineAutoIncludes(mb);
            DefineDefaultEntries(mb);
        }

        private static void DefineTables(ModelBuilder mb)
        {
            mb.Entity<CardEntity>()
                .ToTable("Cards")
                .Property(c => c.Id)
                .ValueGeneratedNever();

            mb.Entity<DeckOptionsEntity>(mb =>
            {
                mb.ToTable("DeckOptions")
                .Property(c => c.Id)
                .ValueGeneratedNever();

                mb.OwnsOne(d => d.Scheduling);
                mb.OwnsOne(d => d.DailyLimits);
                mb.OwnsOne(d => d.Sorting);
            });

            mb.Entity<Deck>()
                .ToTable("Decks")
                .Property(d => d.Id)
                .ValueGeneratedNever();

            mb.Entity<UserEntity>(mb =>
            {
                mb.ToTable("Users")
                .Property(u => u.Id)
                .ValueGeneratedNever();

                mb.OwnsOne(u => u.Options);
            });
                

            mb.Entity<Tag>()
                .ToTable("Tags")
                .Property(t => t.Id)
                .ValueGeneratedNever();

            mb.Entity<CardLog>()
                .ToTable("CardLogs")
                .Property(cl => cl.Id)
                .ValueGeneratedNever();
        }
        private static void ConfigureDeletion(ModelBuilder mb)
        {
            mb.Entity<CardEntity>()
                .HasOne(c => c.Deck)
                .WithMany(d => d.Cards)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<CardLog>()
                .HasOne(cl => cl.Card)
                .WithMany(c => c.CardLogs)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Deck>()
                .HasOne(d => d.ParentDeck)
                .WithMany(d => d.ChildrenDecks)
                .HasForeignKey(d => d.ParentDeckId)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Deck>()
                .HasOne(d => d.Options)
                .WithMany(o => o.DecksUsingThis)
                .HasForeignKey(d => d.OptionsId)
                .OnDelete(DeleteBehavior.SetNull); // TODO: Decide later on app behaviour when user deletes a scheduler preset
                                                // most likely set all decks that were using it to constant non-deletable default preset
        }
        private static void DefineAutoIncludes(ModelBuilder mb)
        {
            // mb.Entity<Deck>()
            //     .Navigation(d => d.Cards)
            //     .AutoInclude();

            // mb.Entity<CardEntity>()
            //     .Navigation(c => c.Tags)
            //     .AutoInclude();
        }
        private static void DefineDefaultEntries(ModelBuilder mb)
        {
            mb.Entity<DeckOptionsEntity>()
                .HasData(Persistence.DeckOptionsEntity.Default);
        }
    }
}