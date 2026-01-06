using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlashMemo.Model.Persistence
{
    public class AppDbContext: DbContext
    {
        public DbSet<CardEntity> Cards { get; set; }
        public DbSet<DeckEntity> Decks { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<TagEntity> Tags { get; set; }
        public DbSet<SchedulerEntity> Schedulers { get; set; }
        public DbSet<CardLogEntity> CardLogs { get; set; }
    
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            #region Tables definitions and don't generate PK 
            mb.Entity<CardEntity>()
                .ToTable("Cards")
                .Property(c => c.Id)
                .ValueGeneratedNever();

            mb.Entity<DeckEntity>()
                .ToTable("Decks")
                .Property(d => d.Id)
                .ValueGeneratedNever();

            mb.Entity<UserEntity>()
                .ToTable("Users")
                .Property(u => u.Id)
                .ValueGeneratedNever();

            mb.Entity<TagEntity>()
                .ToTable("Tags")
                .Property(t => t.Id)
                .ValueGeneratedNever();

            mb.Entity<SchedulerEntity>()
                .ToTable("Schedulers")
                .Property(s => s.Id)
                .ValueGeneratedNever();

            mb.Entity<CardLogEntity>()
                .ToTable("CardLogs")
                .Property(cl => cl.Id)
                .ValueGeneratedNever();
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
                .HasOne(d => d.Scheduler)
                .WithMany(s => s.Decks)
                .HasForeignKey(d => d.SchedulerId)
                .OnDelete(DeleteBehavior.SetNull); // TO DO: Decide later on app behaviour when user deletes a scheduler preset
                                                // most likely set all decks that were using it to constant non-deletable default preset
            
            #endregion
        }
    }
}