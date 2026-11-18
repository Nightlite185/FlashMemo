using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class BrowseVMReloadTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task FailedApplyOrReset_PreservesResultsSelectionAndAppliedFilters(bool reset, bool mappingFails)
        => Helpers.RunOnSta(async () =>
        {
            using var flow = await Workflow.Create();
            if (reset)
                flow.FilterVM.ResetFiltersCommand.Execute(null);
            else
                flow.FilterVM.IsSuspended = true;
            var requested = flow.FilterVM.TakeSnapshot();
            flow.AssertOriginalState();

            var pending = new TaskCompletionSource<IList<CardEntity>>();
            flow.Query.Setup(query => query.GetCardsWhere(It.IsAny<Filters>(),
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>())).Returns(pending.Task);
            if (mappingFails)
                flow.Query.Setup(query => query.GetAllFromUser(7)).ReturnsAsync(InvalidCards());
            else
                flow.Query.Setup(query => query.GetAllFromUser(7)).ThrowsAsync(new IOException());
            flow.CancelErrors();

            var applying = flow.VM.TryApplyFiltersAsync(requested);
            flow.AssertOriginalState();

            if (mappingFails)
                pending.SetResult(InvalidCards());
            else
                pending.SetException(new IOException());
            await applying;

            flow.AssertOriginalState();
            flow.Query.Verify(query => query.GetAllFromUser(7), Times.Once);
            flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
                Times.Exactly(2));
            flow.Dialogs.VerifyNoOtherCalls();
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SuccessfulApply_CommitsResultsAndCachesOnlyAfterLoading(bool empty)
        => Helpers.RunOnSta(async () =>
        {
            using var flow = await Workflow.Create();
            flow.FilterVM.IsSuspended = true;
            var requested = flow.FilterVM.TakeSnapshot();
            var pending = new TaskCompletionSource<IList<CardEntity>>();
            flow.Query.Setup(query => query.GetCardsWhere(requested,
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>())).Returns(pending.Task);

            var applying = flow.VM.TryApplyFiltersAsync(requested);
            flow.AssertOriginalState();

            pending.SetResult(empty ? Array.Empty<CardEntity>() : new[] { CreateCard(2) });
            await applying;

            flow.VM.CardsView.Cast<CardVM>().Select(card => card.Id)
                .Should().Equal(empty ? Array.Empty<long>() : new long[] { 2 });
            flow.VM.SelectedCount.Should().Be(0);
            flow.VM.VisibleCardsCount.Should().Be(empty ? 0 : 1);
            flow.FilterVM.CachedFilters.Should().BeSameAs(requested);
            flow.Session.Object.LastFilters.Should().BeSameAs(requested);
            flow.Query.Verify(query => query.GetAllFromUser(7), Times.Never);
            flow.Dialogs.VerifyNoOtherCalls();
        });

    [Fact]
    public Task SuccessfulFallback_RemembersUnfilteredResultsAndKeepsTheDraftAvailable()
        => Helpers.RunOnSta(async () =>
        {
            using var flow = await Workflow.Create();
            flow.FilterVM.IsSuspended = true;
            flow.Query.Setup(query => query.GetCardsWhere(It.IsAny<Filters>(),
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>())).ThrowsAsync(new IOException());
            var pending = new TaskCompletionSource<IList<CardEntity>>();
            flow.Query.Setup(query => query.GetAllFromUser(7)).Returns(pending.Task);
            flow.CancelErrors();

            var applying = flow.VM.TryApplyFiltersAsync();
            flow.AssertOriginalState();

            pending.SetResult(new[] { CreateCard(2), CreateCard(3) });
            await applying;

            flow.VM.CardsView.Cast<CardVM>().Select(card => card.Id).Should().Equal(2, 3);
            flow.VM.SelectedCount.Should().Be(0);
            flow.VM.VisibleCardsCount.Should().Be(2);
            flow.FilterVM.CachedFilters.Should().BeEquivalentTo(Filters.GetEmpty(7));
            flow.Session.Object.LastFilters.Should().BeSameAs(flow.FilterVM.CachedFilters);
            flow.FilterVM.IsSuspended.Should().BeTrue();
            flow.FilterVM.IsDue.Should().BeTrue();
            flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
                Times.Once);
            flow.Dialogs.VerifyNoOtherCalls();
        });

    [Fact]
    public Task Retry_UsesTheSubmittedSnapshotAndCommitsItOnlyOnSuccess()
        => Helpers.RunOnSta(async () =>
        {
            using var flow = await Workflow.Create();
            flow.FilterVM.IsSuspended = true;
            var requested = flow.FilterVM.TakeSnapshot();
            flow.Query.SetupSequence(query => query.GetCardsWhere(requested,
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>()))
                .ThrowsAsync(new IOException())
                .ReturnsAsync(new[] { CreateCard(2) });
            flow.Dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Callback(() =>
                {
                    flow.AssertOriginalState();
                    flow.FilterVM.IsSuspended = false;
                })
                .Returns(DialogResult.Retry);

            await flow.VM.TryApplyFiltersAsync(requested);

            flow.FilterVM.CachedFilters.Should().BeSameAs(requested);
            flow.Session.Object.LastFilters.Should().BeSameAs(requested);
            flow.FilterVM.IsSuspended.Should().BeFalse();
            flow.VM.CardsView.Cast<CardVM>().Select(card => card.Id).Should().Equal(2);
            flow.Query.Verify(query => query.GetCardsWhere(requested,
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>()), Times.Exactly(2));
            flow.Query.Verify(query => query.GetAllFromUser(7), Times.Never);
            flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
                Times.Once);
        });

    [Fact]
    public Task FailedFocusReload_StaysDirtyAndUsesAppliedFiltersRatherThanTheDraftOnRecovery()
        => Helpers.RunOnSta(async () =>
        {
            using var flow = await Workflow.Create();
            flow.FilterVM.IsSuspended = true;
            flow.Query.Setup(query => query.GetCardsWhere(flow.OriginalFilters,
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>())).ThrowsAsync(new IOException());
            flow.Query.Setup(query => query.GetAllFromUser(7)).ThrowsAsync(new IOException());
            flow.CancelErrors();

            await flow.VM.ReloadDomain(flow.Bus);
            flow.AssertOriginalState();

            flow.Query.Setup(query => query.GetCardsWhere(flow.OriginalFilters,
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>())).ReturnsAsync(new[] { CreateCard(2) });
            flow.VM.OnFocusLost();
            await flow.VM.OnFocusGained();

            flow.VM.CardsView.Cast<CardVM>().Select(card => card.Id).Should().Equal(2);
            flow.FilterVM.CachedFilters.Should().BeSameAs(flow.OriginalFilters);
            flow.Session.Object.LastFilters.Should().BeSameAs(flow.OriginalFilters);
            flow.FilterVM.IsSuspended.Should().BeTrue();
            flow.Query.Verify(query => query.GetCardsWhere(flow.OriginalFilters,
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>()), Times.Exactly(2));
            flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
                Times.Exactly(2));
            flow.Dialogs.VerifyNoOtherCalls();
        });

    private static CardEntity CreateCard(long id) => new()
    {
        Id = id,
        UserId = 7,
        DeckId = 1,
        Deck = new Deck { Id = 1, UserId = 7, Name = "Deck" },
        Note = StandardNote.Create($"Front {id}", $"Back {id}")
    };

    private static CardEntity[] InvalidCards()
        => [CreateCard(2), new() { Id = 3, Note = new UnsupportedNote() }];

    private sealed class UnsupportedNote : Note { }

    private sealed class Workflow : IDisposable
    {
        public Mock<ICardQueryService> Query { get; } = new(MockBehavior.Strict);
        public Mock<IDialogService> Dialogs { get; } = new(MockBehavior.Strict);
        public Mock<ILastSessionService> Session { get; } = new();
        public VMEventBus Bus { get; } = new();
        public Filters OriginalFilters { get; } = Filters.GetEmpty(7) with { IsDue = true };
        public FiltersVM FilterVM { get; private set; } = null!;
        public BrowseVM VM { get; private set; } = null!;
        private CardVM originalCard = null!;

        public static async Task<Workflow> Create()
        {
            var flow = new Workflow();
            flow.Session.SetupProperty(session => session.LastFilters, flow.OriginalFilters);
            var tree = new Mock<IDeckTreeBuilder>(MockBehavior.Strict);
            tree.Setup(builder => builder.BuildAsync(7)).ReturnsAsync(Array.Empty<DeckNode>());
            var tags = new Mock<ITagRepo>(MockBehavior.Strict);
            tags.Setup(repo => repo.GetFromUser(7)).ReturnsAsync(Array.Empty<Tag>());
            var net = Helpers.GetSafetyNet(flow.Dialogs.Object);
            flow.FilterVM = new FiltersVM(tree.Object, tags.Object, flow.Bus, flow.Session.Object,
                7, NullLogger<FiltersVM>.Instance, flow.Dialogs.Object, net);
            await flow.FilterVM.InitializeAsync();
            flow.Query.Setup(query => query.GetCardsWhere(It.IsAny<Filters>(),
                It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>())).ReturnsAsync(new[] { CreateCard(1) });
            flow.VM = new BrowseVM(flow.Query.Object, flow.FilterVM, 7, flow.Bus,
                NullLogger<BrowseVM>.Instance, flow.Dialogs.Object, net);
            await flow.VM.InitializeAsync(null!);
            flow.originalCard = flow.VM.CardsView.Cast<CardVM>().Single();
            flow.originalCard.IsSelected = true;
            flow.VM.SelectedCount = 1;
            flow.VM.GetSelectedCards();
            flow.Query.Invocations.Clear();
            return flow;
        }

        public void CancelErrors()
            => Dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Returns(DialogResult.Cancel);

        public void AssertOriginalState()
        {
            VM.CardsView.Cast<CardVM>().Should().Equal(originalCard);
            originalCard.IsSelected.Should().BeTrue();
            VM.SelectedCount.Should().Be(1);
            VM.VisibleCardsCount.Should().Be(1);
            FilterVM.CachedFilters.Should().BeSameAs(OriginalFilters);
            Session.Object.LastFilters.Should().BeSameAs(OriginalFilters);
        }

        public void Dispose() => VM.OnClosed();
    }
}