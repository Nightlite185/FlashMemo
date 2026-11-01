using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class CardEditorSaveWorkflowTests
{
    [Fact]
    public void PreparingEdits_PreservesSavedValuesAndKeepsNoteAndTagsIndependent()
    {
        var saved = CreateSavedCard();
        var vm = CreateEditable(saved);
        Edit(vm);

        var candidate = vm.ToEntity();

        candidate.Should().NotBeSameAs(saved);
        candidate.Should().BeEquivalentTo(saved, options => options
            .Excluding(card => card.Note).Excluding(card => card.Deck)
            .Excluding(card => card.DeckId).Excluding(card => card.Tags));
        candidate.Note.Should().NotBeSameAs(saved.Note);
        candidate.Note.Id.Should().Be(saved.NoteId);
        ((StandardNote)candidate.Note).FrontContent.Should().Be("Edited front");
        candidate.Tags.Single().Should().NotBeSameAs(saved.Tags.Single());
        candidate.Tags.Single().Name.Should().Be("Edited tag");
        candidate.DeckId.Should().Be(79);
        candidate.Deck.Id.Should().Be(79);
        ((StandardNote)saved.Note).FrontContent.Should().Be("Original front");
        saved.Tags.Single().Name.Should().Be("Original tag");
        vm.IsSameAsSavedNote("Original front", "Original back").Should().BeTrue();
        vm.IsSameAsSavedNote("Edited front", "Edited back").Should().BeFalse();

        vm.RevertChanges();

        ((StandardNoteVM)vm.Note).FrontContent.Should().Be("Original front");
        vm.Tags.Single().Name.Should().Be("Original tag");
        vm.Deck.Id.Should().Be(saved.DeckId);
    }

    [Fact]
    public void LaterEdits_DoNotMutateSuccessfullySavedCandidate()
    {
        var vm = CreateEditable(CreateSavedCard());
        Edit(vm);
        var committed = vm.ToEntity();
        vm.UpdateLastSaved(committed);

        ((StandardNoteVM)vm.Note).FrontContent = "Later front";
        vm.Tags.Single().Name = "Later tag";
        var nextCandidate = vm.ToEntity();

        ((StandardNote)committed.Note).FrontContent.Should().Be("Edited front");
        committed.Tags.Single().Name.Should().Be("Edited tag");
        nextCandidate.Note.Should().NotBeSameAs(committed.Note);
        vm.IsSameAsSavedNote("Edited front", "Edited back").Should().BeTrue();
        vm.RevertChanges();
        ((StandardNoteVM)vm.Note).FrontContent.Should().Be("Edited front");
        vm.Tags.Single().Name.Should().Be("Edited tag");
        vm.Deck.Id.Should().Be(79);
    }

    [Fact]
    public void Refresh_ReplacesEditorBackingNoteWithoutSharingNewSnapshot()
    {
        var vm = CreateEditable(CreateSavedCard());
        var refreshed = CreateSavedCard();
        refreshed.NoteId = 90;
        refreshed.Note = new StandardNote { Id = 90, FrontContent = "Fresh front", BackContent = "Fresh back" };
        vm.Refresh(refreshed);
        ((StandardNoteVM)vm.Note).FrontContent = "Changed after refresh";

        var candidate = vm.ToEntity();

        candidate.Note.Id.Should().Be(90);
        ((StandardNote)refreshed.Note).FrontContent.Should().Be("Fresh front");
        vm.IsSameAsSavedNote("Fresh front", "Fresh back").Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_UpdatesSnapshotAndClosesOnlyAfterPersistenceSucceeds(bool succeeds)
    {
        var vm = CreateEditable(CreateSavedCard());
        Edit(vm);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new Mock<ICardService>(MockBehavior.Strict);
        service.Setup(repo => repo.SaveEditedCard(It.IsAny<CardEntity>(), CardAction.Modify, null))
            .Returns(completion.Task);
        var bus = new Mock<IVMEventBus>();
        var dialogs = new Mock<IDialogService>();
        var editor = new CardEditorVM(service.Object, Mock.Of<ICardRepo>(), bus.Object,
            Mock.Of<IDeckRepo>(), null!, vm, NullLogger<CardEditorVM>.Instance, dialogs.Object,
            Helpers.GetSafetyNet(dialogs.Object));
        bool closed = false;
        editor.OnCloseRequest += () => closed = true;

        var saving = editor.SaveChangesCommand.ExecuteAsync(null);
        vm.IsSameAsSavedNote("Original front", "Original back").Should().BeTrue();
        closed.Should().BeFalse();
        if (succeeds)
            completion.SetResult();
        else
            completion.SetException(new IOException());
        await saving;

        closed.Should().Be(succeeds);
        vm.IsSameAsSavedNote("Edited front", "Edited back").Should().Be(succeeds);
        vm.IsSameAsSavedNote("Original front", "Original back").Should().Be(!succeeds);
        bus.Verify(events => events.NotifyDomain(), succeeds ? Times.Once() : Times.Never());
        service.Verify(repo => repo.SaveEditedCard(It.IsAny<CardEntity>(), CardAction.Modify, null), Times.Once);
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
        vm.RevertChanges();
        ((StandardNoteVM)vm.Note).FrontContent.Should().Be(succeeds ? "Edited front" : "Original front");
    }

    [Fact]
    public async Task SaveToDatabase_PreservesSchedulingAndForeignKeysWhileUpdatingEdits()
    {
        using var factory = new FakeDbFactory();
        await new TestsDbSeeder(factory).SeedDefault();
        var original = CreateSavedCard();
        var newDeck = new Deck { Id = 79, UserId = 71, Name = "New deck", OptionsId = DeckOptions.DefaultId };
        var newTag = new Tag { Id = 75, UserId = 71, Name = "New tag" };
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new UserEntity { Id = 71, Name = "User", Options = UserOptions.CreateDefault() });
            db.Decks.AddRange(original.Deck, newDeck);
            db.Tags.Add(newTag);
            db.Cards.Add(original);
            await db.SaveChangesAsync();
        }

        var mapper = Helpers.GetMapper();
        var vmFactory = new EditableCardVMF(new CardRepo(factory), new TagRepo(factory), new XamlNoteComparer(), mapper);
        var vm = (await vmFactory.CreateAsync(original.Id))!;
        ((StandardNoteVM)vm.Note).FrontContent = "Persisted front";
        vm.ChangeDeck(newDeck);
        vm.Tags.Clear();
        vm.Tags.Add(new TagVM(newTag));
        var candidate = vm.ToEntity();
        await new CardService(factory, mapper).SaveEditedCard(candidate, CardAction.Modify);
        vm.UpdateLastSaved(candidate);

        await using var verificationDb = factory.CreateDbContext();
        var persisted = await verificationDb.Cards.AsNoTracking().Include(card => card.Tags)
            .SingleAsync(card => card.Id == original.Id);
        persisted.Should().BeEquivalentTo(original, options => options
            .Excluding(card => card.Note).Excluding(card => card.Deck)
            .Excluding(card => card.DeckId).Excluding(card => card.Tags).Excluding(card => card.CardLogs));
        persisted.DeckId.Should().Be(newDeck.Id);
        persisted.Note.Id.Should().Be(original.NoteId);
        ((StandardNote)persisted.Note).FrontContent.Should().Be("Persisted front");
        persisted.Tags.Select(tag => tag.Id).Should().Equal(newTag.Id);
        (await verificationDb.Set<Note>().CountAsync()).Should().Be(1);
        (await verificationDb.CardLogs.CountAsync()).Should().Be(1);
        vm.IsSameAsSavedNote("Persisted front", "Original back").Should().BeTrue();
    }

    private static EditableCardVM CreateEditable(CardEntity saved)
        => new(saved, new XamlNoteComparer(), Helpers.GetMapper());

    private static void Edit(EditableCardVM vm)
    {
        ((StandardNoteVM)vm.Note).FrontContent = "Edited front";
        ((StandardNoteVM)vm.Note).BackContent = "Edited back";
        vm.Tags.Single().Name = "Edited tag";
        vm.ChangeDeck(new DeckNode(new Deck { Id = 79, UserId = 71, Name = "New deck" }, []));
    }

    private static CardEntity CreateSavedCard() => new()
    {
        Id = 73,
        UserId = 71,
        NoteId = 76,
        Note = new StandardNote { Id = 76, FrontContent = "Original front", BackContent = "Original back" },
        DeckId = 72,
        Deck = new Deck { Id = 72, UserId = 71, Name = "Original deck", OptionsId = DeckOptions.DefaultId },
        Tags = [new Tag { Id = 74, UserId = 71, Name = "Original tag" }],
        State = CardState.Learning,
        LearningStage = LearningStage.II,
        Interval = TimeSpan.FromMinutes(8),
        Created = new DateTime(2026, 8, 1),
        LastModified = new DateTime(2026, 9, 1),
        LastReviewed = new DateTime(2026, 9, 2),
        Due = new DateTime(2026, 9, 3),
        IsBuried = true,
        BuriedDate = new DateTime(2026, 9, 2),
        IsSuspended = true
    };
}
