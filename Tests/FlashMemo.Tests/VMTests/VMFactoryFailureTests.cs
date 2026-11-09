using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class VMFactoryFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CardEditor_DistinguishesMissingCardFromFailedLookup(bool lookupFails)
    {
        var cards = new Mock<ICardRepo>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        if (lookupFails)
        {
            cards.Setup(repo => repo.GetById(1)).ThrowsAsync(new IOException());
            dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Returns(DialogResult.Cancel);
        }
        else
        {
            cards.Setup(repo => repo.GetById(1)).ReturnsAsync((CardEntity?)null);
            dialogs.Setup(service => service.Show("Error", It.Is<string>(message => message.Contains("doesn't exist")),
                DialogButtons.OK, DialogIcons.Error)).Returns(DialogResult.OK);
        }
        var editable = new EditableCardVMF(cards.Object, null!, null!, null!);
        var factory = new CardEditorVMF(null!, cards.Object, null!, editable, null!, null!, null!,
            NullLogger<CardEditorVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));

        var vm = await factory.TryCreateAsync(1, 7);

        vm.Should().BeNull();
        cards.Verify(repo => repo.GetById(1), Times.Once);
        dialogs.VerifyAll();
        dialogs.Invocations.Should().ContainSingle();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeckSelector_RetriesOrPreservesTheExistingPopupOnCancel(bool retry)
    {
        var builder = new Mock<IDeckTreeBuilder>(MockBehavior.Strict);
        builder.SetupSequence(service => service.BuildAsync(7))
            .ThrowsAsync(new IOException())
            .ReturnsAsync(Array.Empty<DeckNode>());
        var dialogs = ErrorDialogs(retry);
        var net = Helpers.GetSafetyNet(dialogs.Object);
        var factory = new DeckSelectVMF(builder.Object, NullLogger<DeckSelectVM>.Instance, dialogs.Object, net);
        var deck = new Deck { Id = 1, UserId = 7, Name = "Deck" };
        var vm = new CreateCardVM(null!, deck, factory, null!, null!, null!,
            NullLogger<CreateCardVM>.Instance, dialogs.Object, net);
        var original = new RescheduleVMF(NullLogger<RescheduleVM>.Instance, dialogs.Object, net)
            .Create(_ => Task.CompletedTask, () => { });
        vm.CurrentPopup = original;

        await vm.ShowDeckSelectCommand.ExecuteAsync(null);

        if (retry)
            vm.CurrentPopup.Should().BeOfType<DeckSelectVM>();
        else
            vm.CurrentPopup.Should().BeSameAs(original);
        builder.Verify(service => service.BuildAsync(7), Times.Exactly(retry ? 2 : 1));
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedDisplayCreation_PreservesTheCurrentDisplay(bool review)
    {
        var dialogs = ErrorDialogs(retry: false);
        var net = Helpers.GetSafetyNet(dialogs.Object);
        var bus = new Mock<IVMEventBus>();
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        options.Setup(service => service.GetFromUser(7)).ThrowsAsync(new IOException());
        var reviewFactory = new ReviewVMF(null!, null!, ContextMenuFactory(bus.Object, dialogs.Object, net),
            null!, null!, bus.Object, options.Object, NullLogger<ReviewVM>.Instance, dialogs.Object, net);
        var stats = new Mock<IStatsQueryService>(MockBehavior.Strict);
        stats.Setup(service => service.GetAnswerRatio(Answers.Good, TimePeriod.Month, 7))
            .ThrowsAsync(new IOException());
        var statsFactory = new StatsVMF(stats.Object, bus.Object, NullLogger<StatsVM>.Instance,
            NullLogger<AnswerRatioVM>.Instance, dialogs.Object, net);
        var original = new Mock<IClosedHandler>(MockBehavior.Strict);
        var host = new Mock<IDisplayHost>();
        host.SetupProperty(value => value.CurrentDisplay, original.Object);
        var control = new DisplayControl(host.Object, null!, reviewFactory, statsFactory);

        if (review)
            await control.SwitchToReview(7, new Deck { Id = 1, UserId = 7 });
        else
            await control.SwitchToStats(7);

        host.Object.CurrentDisplay.Should().BeSameAs(original.Object);
        original.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once);
        dialogs.VerifyNoOtherCalls();
        if (review)
        {
            bus.VerifyRemove(value => value.DomainChanged -= It.IsAny<Action>(), Times.Once);
            bus.VerifyRemove(value => value.UserOptionsChanged -= It.IsAny<Action>(), Times.Once);
            bus.VerifyRemove(value => value.DeckOptionsChanged -= It.IsAny<Action>(), Times.Once);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task Browse_FailedAttemptsReleaseSubscriptionsAndRetryInitializesTheView(bool filtersFail, bool retry)
        => Helpers.RunOnSta(async () =>
        {
            var bus = new Mock<IVMEventBus>();
            int subscribers = 0;
            bus.SetupAdd(value => value.DomainChanged += It.IsAny<Action>()).Callback(() => subscribers++);
            bus.SetupRemove(value => value.DomainChanged -= It.IsAny<Action>()).Callback(() => subscribers--);
            var builder = new Mock<IDeckTreeBuilder>(MockBehavior.Strict);
            if (filtersFail)
                builder.SetupSequence(service => service.BuildAsync(7))
                    .ThrowsAsync(new IOException())
                    .ReturnsAsync(Array.Empty<DeckNode>());
            else
                builder.Setup(service => service.BuildAsync(7)).ReturnsAsync(Array.Empty<DeckNode>());
            var tags = new Mock<ITagRepo>(MockBehavior.Strict);
            tags.Setup(repo => repo.GetFromUser(7)).ReturnsAsync(Array.Empty<Tag>());
            var query = new Mock<ICardQueryService>(MockBehavior.Strict);
            if (filtersFail)
                query.Setup(service => service.GetCardsWhere(It.IsAny<Filters>(), It.IsAny<CardsOrder>(),
                    It.IsAny<SortingDirection>())).ReturnsAsync(Array.Empty<CardEntity>());
            else
                query.SetupSequence(service => service.GetCardsWhere(It.IsAny<Filters>(), It.IsAny<CardsOrder>(),
                    It.IsAny<SortingDirection>()))
                    .ThrowsAsync(new IOException())
                    .ReturnsAsync(Array.Empty<CardEntity>());
            var dialogs = ErrorDialogs(retry);
            dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Callback(() => subscribers.Should().Be(0))
                .Returns(retry ? DialogResult.Retry : DialogResult.Cancel);
            var net = Helpers.GetSafetyNet(dialogs.Object);
            var filters = new FiltersVMF(builder.Object, tags.Object, bus.Object, Mock.Of<ILastSessionService>(),
                NullLogger<FiltersVM>.Instance, dialogs.Object);
            var factory = new BrowseVMF(query.Object, filters, ContextMenuFactory(bus.Object, dialogs.Object, net),
                bus.Object, NullLogger<BrowseVM>.Instance, dialogs.Object, net);

            var vm = await factory.TryCreateAsync(7);

            if (retry)
            {
                vm.Should().NotBeNull();
                vm!.CardsView.Should().NotBeNull();
                vm.CardsView.Count.Should().Be(0);
                vm.CtxMenuVM.Should().NotBeNull();
                subscribers.Should().Be(2);
                vm.OnClosed();
            }
            else
                vm.Should().BeNull();
            subscribers.Should().Be(0);
            builder.Verify(service => service.BuildAsync(7), Times.Exactly(retry ? 2 : 1));
            dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once);
            dialogs.VerifyNoOtherCalls();
        });

    private static Mock<IDialogService> ErrorDialogs(bool retry)
    {
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
            .Returns(retry ? DialogResult.Retry : DialogResult.Cancel);
        return dialogs;
    }

    private static CardCtxMenuVMF ContextMenuFactory(IVMEventBus bus, IDialogService dialogs, SafetyNet net)
        => new(null!, null!, bus, null!, null!, NullLogger<CardCtxMenuVM>.Instance, dialogs, net, null!, null!);
}