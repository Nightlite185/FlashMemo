using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq.AutoMock;
using Moq;

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
        mocker.Use(Helpers.GetSafetyNet());
        
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
                Id = i + 1,
                UserId = 7
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

        (await vm.AddCard()).Should().BeTrue();
        
        var resultCard = db.Cards
            .Include(c => c.Note)
            .SingleOrDefault();

        AssertionHelper(resultCard, expectedCard, expectedNote!);
        resultCard!.Id.Should().BePositive();
        resultCard.Note.Id.Should().BePositive();
    }

    [Fact] public async Task AddingNewCardWithTags()
    {
        var vm = await Init();
        var db = factory.CreateDbContext();

        var expectedCard = PrepareTaglessCard(vm);
        var expectedNote = expectedCard.Note as StandardNote;

        var tags = GetSampleTags(db);

        vm.WipCard.Tags.AddRange(tags.ToVMs());

        (await vm.AddCard()).Should().BeTrue();

        var resultCard = db.Cards
            .Include(c => c.Note)
            .Include(c => c.Tags)
            .SingleOrDefault();

        AssertionHelper(resultCard, expectedCard, expectedNote!);

        resultCard!.Tags.Should()
            .HaveCount(tags.Count)
            .And.AllSatisfy(t => t.UserId = 7);
    }

    [Fact]
    public async Task FailedSave_KeepsDraftAndHistoryAndDoesNotNotifyDomain()
    {
        var mocker = new AutoMocker();
        var dialogs = mocker.GetMock<IDialogService>();
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        mocker.Use<IDeckMeta>(new Deck { Id = 1, UserId = 7, Name = "Deck" });
        mocker.Use(Helpers.GetSafetyNet(dialogs.Object));
        mocker.GetMock<ICardRepo>().Setup(repo => repo.AddCard(It.IsAny<CardEntity>()))
            .ThrowsAsync(new IOException());
        var vm = mocker.CreateInstance<CreateCardVM>();
        PrepareTaglessCard(vm);
        var draft = vm.WipCard;
        var tag = new TagVM(new Tag { Id = 2, UserId = 7, Name = "Tag" });
        draft.Tags.Add(tag);
        var previous = new CardEntity { Id = 3 };
        vm.History.Add(previous);

        bool saved = await vm.AddCard();

        saved.Should().BeFalse();
        vm.WipCard.Should().BeSameAs(draft);
        vm.WipCard.Tags.Should().Equal(tag);
        vm.History.Should().Equal(previous);
        mocker.GetMock<IVMEventBus>().Verify(bus => bus.NotifyDomain(), Times.Never);
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);
    }

    public void Dispose() => factory.Dispose();
}
