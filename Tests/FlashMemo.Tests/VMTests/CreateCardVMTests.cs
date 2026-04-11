using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq.AutoMock;

namespace FlashMemo.Tests.VMTests;

public class CreateCardVMTests : IDisposable
{
    private readonly FakeDbFactory factory = new();
    private async Task<CreateCardVM> Init()
    {
        var mocker = new AutoMocker();
        var seeder = new TestsDbSeeder(factory);
        var cardRepo = new CardRepo(factory);

        await seeder.SeedDefault();
        seeder.SeedUser();
        var targetDeck = seeder.SeedDeck(); 

        mocker.Use<IDeckMeta>(targetDeck);
        mocker.Use<ICardRepo>(cardRepo);
        
        return mocker.CreateInstance<CreateCardVM>();
    }
    private static CardEntity PrepareTaglessCard(CreateCardVM vm)
    {
        var expectedNote = new StandardNote()
        {
            FrontContent = "front",
            BackContent = "back"
        };

        var expectedCard = new CardEntity()
        {
            UserId = 7,
            Note = expectedNote
        };

        vm.WipCard.Note = new StandardNoteVM(expectedNote);

        return expectedCard;
    }
    private static List<Tag> GetSampleTags(AppDbContext db)
    {
        List<Tag> tags = [];

        for (int i = 0; i < 10; i++)
            tags.Add(new Tag()
            {
                Name = $"tag{i}",
                Id = i,
                UserId = 7,
                IntColor = 0
            });

        db.Tags.AddRange(tags);
        db.SaveChanges();

        return tags;
    }
    private static void AssertionHelper(CardEntity? resultCard, CardEntity expectedCard, StandardNote expectedNote)
    {
        resultCard.Should().NotBeNull()
            .And.BeEquivalentTo(expectedCard, opt =>
            opt.Including(c => c.UserId));

        var resultNote = resultCard.Note.Should()
            .BeOfType<StandardNote>().Subject;

        resultNote.Should().BeEquivalentTo(expectedNote, opt => opt
            .Including(n => n.FrontContent)
            .Including(n => n.BackContent));
    }

    [Fact] public async Task AddingNewCardNoTags()
    {
        var vm = await Init();
        var db = factory.CreateDbContext();

        var expectedCard = PrepareTaglessCard(vm);
        var expectedNote = expectedCard.Note as StandardNote;

        await vm.AddCardCommand.ExecuteAsync(null);
        
        var resultCard = db.Cards
            .Include(c => c.Note)
            .SingleOrDefault();

        AssertionHelper(resultCard, expectedCard, expectedNote!);
    }

    [Fact] public async Task AddingNewCardWithTags()
    {
        var vm = await Init();
        var db = factory.CreateDbContext();

        var expectedCard = PrepareTaglessCard(vm);
        var expectedNote = expectedCard.Note as StandardNote;

        var tags = GetSampleTags(db);

        vm.WipCard.Tags.AddRange(tags.ToVMs());

        await vm.AddCardCommand.ExecuteAsync(null);

        var resultCard = db.Cards
            .Include(c => c.Note)
            .Include(c => c.Tags)
            .SingleOrDefault();

        AssertionHelper(resultCard, expectedCard, expectedNote!);

        resultCard!.Tags.Should()
            .HaveCount(tags.Count)
            .And.AllSatisfy(t => t.UserId = 7);
    }

    public void Dispose() => factory.Dispose();
}