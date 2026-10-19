using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class DecksVMTests
{
    [Fact]
    public async Task ReloadDomain_ClearsSelectionBeforeReplacingTree()
    {
        DeckNode oldDeck = CreateDeck(1);
        DeckNode newDeck = CreateDeck(2);
        var builder = new Mock<IDeckTreeBuilder>();
        builder.Setup(service => service.BuildCountedAsync(1))
            .ReturnsAsync(new[] { newDeck });
        var vm = new DecksVM(null!, builder.Object, null!, null!, null!, 1, null!, null!);
        vm.DeckTree.Add(oldDeck);
        vm.SelectedDecks.Add(oldDeck);

        await vm.ReloadDomainCommand.ExecuteAsync(null);

        vm.SelectedDecks.Should().BeEmpty();
        vm.DeckTree.Should().Equal(newDeck);
    }

    [Fact]
    public async Task RemoveDecks_WithNoSelection_DoesNothing()
    {
        var repo = new Mock<IDeckRepo>(MockBehavior.Strict);
        var vm = new DecksVM(repo.Object, null!, null!, null!, null!, 1, null!, null!);

        await vm.RemoveDeckCommand.ExecuteAsync(null);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveDecks_WhenSelectionContainsDescendant_DeletesEachSubtreeOnce()
    {
        var deckRepo = new Mock<IDeckRepo>();
        deckRepo
            .Setup(repo => repo.RemoveDeck(1))
            .ReturnsAsync(new HashSet<long> { 1, 2 });
        deckRepo
            .Setup(repo => repo.RemoveDeck(3))
            .ReturnsAsync(new HashSet<long> { 3 });

        var userOptions = new Mock<IUserOptionsService>();
        userOptions
            .Setup(service => service.GetFromUser(1))
            .ReturnsAsync(new UserOptions { ConfirmDeckDeletion = false });

        var vm = new DecksVM(
            deckRepo.Object,
            null!,
            null!,
            Mock.Of<ILastSessionService>(),
            userOptions.Object,
            1,
            null!,
            null!);

        DeckNode parent = CreateDeck(1);
        DeckNode child = CreateDeck(2);
        DeckNode otherRoot = CreateDeck(3);
        parent.AddChild(child);
        vm.DeckTree.Add(parent);
        vm.DeckTree.Add(otherRoot);
        vm.SelectedDecks.Add(parent);
        vm.SelectedDecks.Add(child);
        vm.SelectedDecks.Add(otherRoot);

        await vm.RemoveDeckCommand.ExecuteAsync(null);

        deckRepo.Verify(repo => repo.RemoveDeck(1), Times.Once);
        deckRepo.Verify(repo => repo.RemoveDeck(2), Times.Never);
        deckRepo.Verify(repo => repo.RemoveDeck(3), Times.Once);
        vm.DeckTree.Should().BeEmpty();
        vm.SelectedDecks.Should().BeEmpty();
    }

    private static DeckNode CreateDeck(long id)
        => new(
            new Deck
            {
                Id = id,
                UserId = 1,
                Name = $"Deck {id}"
            },
            []);
}
