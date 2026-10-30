using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class MainVMTests
{
    [Fact]
    public async Task ShowCreateCard_WhenOneDeckIsSelected_UsesSelectedDeck()
    {
        var deckRepo = new Mock<IDeckRepo>(MockBehavior.Strict);
        var deck = new DeckNode(new Deck { Id = 10, UserId = 7, Name = "Selected deck" }, []);
        var decksVM = new DecksVM(null!, null!, null!, null!, null!, 7, null!, null!,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!);
        decksVM.SelectedDecks.Add(deck);
        var vm = new MainVM(
            Mock.Of<ILastSessionService>(), deckRepo.Object, Mock.Of<IVMEventBus>(), 7,
            NullLogger<MainVM>.Instance, Mock.Of<IDialogService>())
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
            userId,
            NullLogger<MainVM>.Instance, Mock.Of<IDialogService>())
        {
            CurrentDisplay = new DecksVM(
                null!, null!, null!, null!, null!, userId, null!, null!,
                NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!)
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShowCreateCard_WhenDeckLookupFails_DoesNotNavigateOrFallBack(bool hasCachedDeck)
    {
        var deckRepo = new Mock<IDeckRepo>(MockBehavior.Strict);
        if (hasCachedDeck)
            deckRepo.Setup(repo => repo.GetDeckMetaById(10)).ThrowsAsync(new IOException());
        else
            deckRepo.Setup(repo => repo.GetFirst(7)).ThrowsAsync(new IOException());

        var lastSession = new Mock<ILastSessionService>();
        lastSession.SetupGet(service => service.LastDeckId).Returns(hasCachedDeck ? 10L : null);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
            .Returns(DialogResult.Cancel);
        var vm = new MainVM(lastSession.Object, deckRepo.Object, Mock.Of<IVMEventBus>(), 7,
            NullLogger<MainVM>.Instance, dialogs.Object);
        bool navigated = false;
        vm.NavRequested += _ =>
        {
            navigated = true;
            return Task.CompletedTask;
        };

        await vm.ShowCreateCardCommand.ExecuteAsync(null);

        navigated.Should().BeFalse();
        deckRepo.Verify(repo => repo.GetDeckMetaById(10), hasCachedDeck ? Times.Once() : Times.Never());
        deckRepo.Verify(repo => repo.GetFirst(7), hasCachedDeck ? Times.Never() : Times.Once());
        deckRepo.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
            Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShowCreateCard_WhenDeckLookupSucceedsAfterRetry_NavigatesOnce()
    {
        var deck = new Deck { Id = 10, UserId = 7, Name = "Deck" };
        var deckRepo = new Mock<IDeckRepo>(MockBehavior.Strict);
        deckRepo.SetupSequence(repo => repo.GetFirst(7))
            .ThrowsAsync(new IOException())
            .ReturnsAsync(deck);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
            .Returns(DialogResult.Retry);
        var vm = new MainVM(Mock.Of<ILastSessionService>(), deckRepo.Object, Mock.Of<IVMEventBus>(), 7,
            NullLogger<MainVM>.Instance, dialogs.Object);
        var navigations = new List<NavigationRequest>();
        vm.NavRequested += request =>
        {
            navigations.Add(request);
            return Task.CompletedTask;
        };

        await vm.ShowCreateCardCommand.ExecuteAsync(null);

        navigations.Should().ContainSingle().Which.Should().BeOfType<CreateCardNavRequest>()
            .Subject.TargetDeck.Should().BeSameAs(deck);
        deckRepo.Verify(repo => repo.GetFirst(7), Times.Exactly(2));
        deckRepo.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
            Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, "No decks yet")]
    [InlineData(2, "Choose one deck")]
    public async Task ShowCreateCard_WhenDeckCannotBeChosen_ShowsInjectedDialogWithoutNavigating(
        int selectedCount, string expectedTitle)
    {
        const long userId = 7;
        var deckRepo = new Mock<IDeckRepo>(MockBehavior.Strict);
        if (selectedCount == 0)
            deckRepo.Setup(repo => repo.GetFirst(userId)).ReturnsAsync((Deck?)null);

        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.Show(expectedTitle, It.IsAny<string>(),
                DialogButtons.OK, DialogIcons.Information))
            .Returns(DialogResult.OK);

        var decksVM = new DecksVM(null!, null!, null!, null!, null!, userId, null!, null!,
            NullLogger<DecksVM>.Instance, dialogs.Object, null!);
        for (int i = 0; i < selectedCount; i++)
            decksVM.SelectedDecks.Add(new DeckNode(
                new Deck { Id = i + 1, UserId = userId, Name = $"Deck {i + 1}" }, []));

        var vm = new MainVM(Mock.Of<ILastSessionService>(), deckRepo.Object, Mock.Of<IVMEventBus>(),
            userId, NullLogger<MainVM>.Instance, dialogs.Object)
        {
            CurrentDisplay = decksVM
        };
        bool navigated = false;
        vm.NavRequested += _ =>
        {
            navigated = true;
            return Task.CompletedTask;
        };

        await vm.ShowCreateCardCommand.ExecuteAsync(null);

        navigated.Should().BeFalse();
        dialogs.Verify(service => service.Show(expectedTitle, It.IsAny<string>(),
            DialogButtons.OK, DialogIcons.Information), Times.Once);
        dialogs.VerifyNoOtherCalls();
        deckRepo.Verify(repo => repo.GetFirst(userId), selectedCount == 0 ? Times.Once() : Times.Never());
        deckRepo.VerifyNoOtherCalls();
    }
}
