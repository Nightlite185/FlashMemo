using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Model.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<CardEntity> Cards { get; set; }
        public DbSet<DeckEntity> Decks { get; set; }
        public DbSet<DeckOptions> DeckOptions { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<CardLogEntity> CardLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            #region Tables definitions and don't generate PK
            mb.Entity<CardEntity>()
                .ToTable("Cards")
                .Property(c => c.Id)
                .ValueGeneratedNever();

            mb.Entity<DeckOptions>(mb =>
            {
                mb.ToTable("DeckOptions")
                .Property(c => c.Id)
                .ValueGeneratedNever();

                mb.OwnsOne(d => d.Scheduling);
                mb.OwnsOne(d => d.DailyLimits);
                mb.OwnsOne(d => d.Sorting);
            });

            mb.Entity<DeckEntity>()
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

            mb.Entity<CardLogEntity>()
                .ToTable("CardLogs")
                .Property(cl => cl.Id)
                .ValueGeneratedNever();
            #endregion

            #region auto includes
            mb.Entity<DeckEntity>()
                .Navigation(d => d.Cards)
                .AutoInclude();

            mb.Entity<CardEntity>()
                .Navigation(c => c.Tags)
                .AutoInclude();
            #endregion
            
            #region cascade deletion
            mb.Entity<CardEntity>()
                .HasOne(c => c.Deck)
                .WithMany(d => d.Cards)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<CardLogEntity>()
                .HasOne(cl => cl.Card)
                .WithMany(c => c.CardLogs)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<DeckEntity>()
                .HasOne(d => d.ParentDeck)
                .WithMany(d => d.ChildrenDecks)
                .HasForeignKey(d => d.ParentDeckId)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<DeckEntity>()
                .HasOne(d => d.Options)
                .WithMany(o => o.DecksUsingThis)
                .HasForeignKey(d => d.OptionsId)
                .OnDelete(DeleteBehavior.SetNull); // TO DO: Decide later on app behaviour when user deletes a scheduler preset
                                                // most likely set all decks that were using it to constant non-deletable default preset
            #endregion
        }
    }
}