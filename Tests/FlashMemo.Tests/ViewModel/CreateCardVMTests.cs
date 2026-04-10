using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq.AutoMock;

namespace FlashMemo.Tests.ViewModel;

public class CreateCardVMTests : IDisposable
{
    private readonly FakeDbFactory factory = new();
    private CreateCardVM Init()
    {
        var mocker = new AutoMocker();
        var db = factory.CreateDbContext();
        
        var cardRepo = new CardRepo(factory);
        var targetDeck = new Deck()
        {
            Id = 7,
            UserId = 7,
            Name = "lol"
        };

        var user = new UserEntity()
        {
            Id = 7,
            Name = "lol",
        };

        db.Users.Add(user);
        db.Decks.Add(targetDeck);
        db.SaveChanges();

        mocker.Use<IDeckMeta>(targetDeck);
        mocker.Use<ICardRepo>(cardRepo);
        
        return mocker.CreateInstance<CreateCardVM>();
    }

    [Fact] public async Task AddingNewCardNoTags()
    {
        throw new NotImplementedException();

        var vm = Init();

        var note = new StandardNote()
        {
            FrontContent = "front",
            BackContent = "back"
        };

        vm.WipCard.Note = new StandardNoteVM(note);

        await vm.AddCardCommand.ExecuteAsync(null);
        
        var db = factory.CreateDbContext();
        var card = db.Cards.SingleOrDefault();

        Assert.NotNull(card);
        Assert.Equal(note, card.Note); // skip id checking and use fluent assertions for this.
    }

    public void Dispose()
    {
        factory.Dispose();
    }
}