using FlashMemo.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Model.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<DeckOptionsEntity> DeckOptions => Set<DeckOptionsEntity>();
        public DbSet<AppSessionData> AppSessionData => Set<AppSessionData>();
        public DbSet<UserSessionCache> UserSessionCaches => Set<UserSessionCache>();
        public DbSet<CardEntity> Cards => Set<CardEntity>();
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<CardLog> CardLogs => Set<CardLog>();
        public DbSet<Deck> Decks => Set<Deck>();
        public DbSet<Tag> Tags => Set<Tag>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            DefineTables(mb);
            ConfigureDeletion(mb);
            DefineAutoIncludes(mb);
        }

        private static void DefineTables(ModelBuilder mb)
        {
            mb.Entity<CardEntity>(mb =>
            {
                mb.ToTable("Cards")
                    .Property(c => c.Id)
                    .ValueGeneratedNever();
            });

            mb.Entity<Note>(mb =>
            {
                mb.HasKey(n => n.Id);

                mb.ToTable("Notes")
                    .Property(n => n.Id)
                    .ValueGeneratedNever();
                    
                mb.HasDiscriminator<string>("NoteType")
                    .HasValue<StandardNote>("Standard");
            });
                

            mb.Entity<AppSessionData>(mb =>
            {
                mb.Property(s => s.Id)
                .ValueGeneratedNever();

                mb.HasOne<UserEntity>()
                    .WithMany()
                    .HasForeignKey(s => s.LastLoadedUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            mb.Entity<UserSessionCache>(mb =>
            {
                mb.HasKey(s => s.UserId);

                mb.HasOne<UserEntity>()
                    .WithOne()
                    .HasForeignKey<UserSessionCache>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                mb.HasOne<Deck>()
                    .WithOne()
                    .HasForeignKey<UserSessionCache>(s => new
                    {
                        s.UserId,
                        s.LastUsedDeckId
                    })
                    .HasPrincipalKey<Deck>(d => new
                    {
                        d.UserId,
                        LastUsedDeckId = d.Id
                    })
                    .OnDelete(DeleteBehavior.Restrict);

                mb.OwnsOne(s => s.LastFilters, mb =>
                {
                    mb.Property(f => f.TagIds)
                    .HasConversion(EFConverters.ImmutableLongList);
                    
                    mb.Property(f => f.DeckIds)
                    .HasConversion(EFConverters.ImmutableLongList);
                    
                    mb.Property(f => f.States)
                    .HasConversion(EFConverters.ImmutableCardStateList);
                });
            });

            mb.Entity<DeckOptionsEntity>(mb =>
            {
                mb.ToTable("DeckOptions")
                .Property(c => c.Id)
                .ValueGeneratedNever();

                mb.OwnsOne(d => d.Scheduling)
                    .OwnsOne(s => s.LearningStages);
                    
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
                

            mb.Entity<Tag>(mb =>
            {
                mb.ToTable("Tags");

                mb.Property(t => t.Id)
                    .ValueGeneratedNever();

                mb.Property(t => t.Name)
                    .IsRequired()
                    .UseCollation("NOCASE"); // SQLite case-insensitive

                mb.HasIndex(t => new { t.UserId, t.Name })
                    .IsUnique();
            });

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
                .OnDelete(DeleteBehavior.SetNull);

            mb.Entity<Deck>()
                .HasOne(d => d.ParentDeck)
                .WithMany(d => d.ChildrenDecks)
                .HasForeignKey(d => d.ParentDeckId)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Deck>()
                .HasOne(d => d.Options)
                .WithMany(o => o.Decks)
                .HasForeignKey(d => d.OptionsId)    //* restrict on preset delete when there are still decks pointing to it.
                .OnDelete(DeleteBehavior.Restrict); //* First manually set it to -1 for each deck, then remove preset :)
        }                                           
        private static void DefineAutoIncludes(ModelBuilder mb)
        {
            mb.Entity<CardEntity>()
                .Navigation(c => c.Note)
                .AutoInclude();

            // mb.Entity<Deck>()
            //     .Navigation(d => d.Cards)
            //     .AutoInclude();

            // mb.Entity<CardEntity>()
            //     .Navigation(c => c.Tags)
            //     .AutoInclude();
        }
    }
}
