using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class MainVMTests
{
    [Fact]
    public async Task ShowCreateCard_WhenOneDeckIsSelected_UsesSelectedDeck()
    {
        var deckRepo = new Mock<IDeckRepo>(MockBehavior.Strict);
        var deck = new DeckNode(new Deck { Id = 10, UserId = 7, Name = "Selected deck" }, []);
        var decksVM = new DecksVM(null!, null!, null!, null!, null!, 7, null!, null!);
        decksVM.SelectedDecks.Add(deck);
        var vm = new MainVM(
            Mock.Of<ILastSessionService>(), deckRepo.Object, Mock.Of<IVMEventBus>(), 7)
        {
            CurrentDisplay = decksVM
        };
        NavigationRequest? navigation = null;
        vm.NavRequested += request =>
        {
            navigation = request;
            return Task.CompletedTask;
        };

        await vm.ShowCreateCardCommand.ExecuteAsync(null);

        navigation.Should().BeOfType<CreateCardNavRequest>()
            .Subject.TargetDeck.Should().BeSameAs(deck);
        deckRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShowCreateCard_WhenDecksDisplayHasNoSelection_UsesFallbackDeck()
    {
        const long userId = 7;
        var fallbackDeck = new Deck
        {
            Id = 10,
            UserId = userId,
            Name = "Fallback deck"
        };

        var deckRepo = new Mock<IDeckRepo>();
        deckRepo
            .Setup(repo => repo.GetFirst(userId))
            .ReturnsAsync(fallbackDeck);

        var vm = new MainVM(
            Mock.Of<ILastSessionService>(),
            deckRepo.Object,
            Mock.Of<IVMEventBus>(),
            userId)
        {
            CurrentDisplay = new DecksVM(
                null!, null!, null!, null!, null!, userId, null!, null!)
        };

        NavigationRequest? navigation = null;
        vm.NavRequested += request =>
        {
            navigation = request;
            return Task.CompletedTask;
        };

        await vm.ShowCreateCardCommand.ExecuteAsync(null);

        CreateCardNavRequest request = navigation.Should()
            .BeOfType<CreateCardNavRequest>()
            .Subject;
        request.TargetDeck.Should().BeSameAs(fallbackDeck);
    }
}
