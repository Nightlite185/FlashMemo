using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Windows;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class BaseVMReloadTests
{
    [Theory]
    [InlineData("domain", false)]
    [InlineData("domain", true)]
    [InlineData("user", false)]
    [InlineData("user", true)]
    [InlineData("deck", false)]
    [InlineData("deck", true)]
    public async Task FailedReload_StaysDirtyUntilALaterFocusSucceeds(string kind, bool throws)
    {
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        if (throws)
            dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Returns(DialogResult.Cancel);
        var vm = new ReloadVM(Helpers.GetSafetyNet(dialogs.Object));
        vm.Load = () => throws ? Task.FromException<bool>(new IOException()) : Task.FromResult(false);
        vm.MarkDirty(kind);

        await vm.OnFocusGained();

        vm.IsDirty(kind).Should().BeTrue();
        vm.Attempts.Should().Be(1);

        vm.Load = () => Task.FromResult(true);
        vm.OnFocusLost();
        await vm.OnFocusGained();
        vm.IsDirty(kind).Should().BeFalse();
        vm.Attempts.Should().Be(2);

        vm.OnFocusLost();
        await vm.OnFocusGained();
        vm.Attempts.Should().Be(2);
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
            throws ? Times.Once() : Times.Never());
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PendingReload_DoesNotClearTheFlagOrStartASecondLoadWhenFocusReturns()
    {
        var completion = new TaskCompletionSource<bool>();
        var vm = new ReloadVM(Helpers.GetSafetyNet()) { Load = () => completion.Task };
        vm.MarkDirty("domain");

        var reload = vm.OnFocusGained();
        vm.IsDirty("domain").Should().BeTrue();

        vm.OnFocusLost();
        await vm.OnFocusGained();
        vm.Attempts.Should().Be(1);

        completion.SetResult(true);
        await reload;
        vm.IsDirty("domain").Should().BeFalse();
    }

    [Fact]
    public async Task ErrorDialogReturningFocus_DoesNotReenterTheFailedReload()
    {
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        var vm = new ReloadVM(Helpers.GetSafetyNet(dialogs.Object))
        {
            Load = () => Task.FromException<bool>(new IOException())
        };
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
            .Callback(() =>
            {
                vm.OnFocusLost();
                vm.OnFocusGained().GetAwaiter().GetResult();
            })
            .Returns(DialogResult.Cancel);
        vm.MarkDirty("domain");

        await vm.OnFocusGained();

        vm.Attempts.Should().Be(1);
        vm.IsDirty("domain").Should().BeTrue();
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once());
    }

    [Fact]
    public async Task ManualDeckRefresh_ReportsFailureWithoutThrowing()
    {
        var tree = new Mock<IDeckTreeBuilder>(MockBehavior.Strict);
        tree.Setup(builder => builder.BuildCountedAsync(7)).ThrowsAsync(new IOException());
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
            .Returns(DialogResult.Cancel);
        var vm = new DecksVM(null!, tree.Object, null!, null!, null!, 7,
            new VMEventBus(), null!, NullLogger<DecksVM>.Instance, dialogs.Object,
            null!, Helpers.GetSafetyNet(dialogs.Object), null!);

        await vm.ReloadDomainCommand.ExecuteAsync(null);

        tree.VerifyAll();
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once());
    }

    [Fact]
    public async Task ManualBrowseApply_ReportsFailureWithoutThrowing()
    {
        var query = new Mock<ICardQueryService>(MockBehavior.Strict);
        query.Setup(service => service.GetCardsWhere(It.IsAny<Filters>(),
            It.IsAny<CardsOrder>(), It.IsAny<SortingDirection>())).ThrowsAsync(new IOException());
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        var vm = new BrowseVM(query.Object, null!, 7, new VMEventBus(),
            NullLogger<BrowseVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));

        await vm.TryApplyFiltersAsync(new Filters { UserId = 7 });

        query.VerifyAll();
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once());
    }

    private sealed class ReloadVM(SafetyNet net)
        : BaseVM(new VMEventBus(), NullLogger.Instance, Mock.Of<IDialogService>(), net)
    {
        public Func<Task<bool>> Load { get; set; } = () => Task.FromResult(true);
        public int Attempts { get; private set; }

        public void MarkDirty(string kind)
        {
            OnFocusLost();
            switch (kind)
            {
                case "domain": OnDomainChanged(); break;
                case "user": OnUserOptChanged(); break;
                case "deck": OnDeckOptChanged(); break;
            }
        }

        public bool IsDirty(string kind) => kind switch
        {
            "domain" => isDomainDirty,
            "user" => isUserOptDirty,
            _ => isDeckOptDirty
        };

        private Task<bool> Reload()
        {
            Attempts++;
            return Load();
        }

        protected override Task<bool> ReloadDomainAsync() => Reload();
        protected override Task<bool> ReloadUserOptAsync() => Reload();
        protected override Task<bool> ReloadDeckOptAsync() => Reload();
    }
}