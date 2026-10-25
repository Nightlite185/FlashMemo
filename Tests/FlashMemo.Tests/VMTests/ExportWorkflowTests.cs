using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class ExportWorkflowTests
{
    [Fact]
    public async Task TextExport_DoesNotInspectDeckTree()
    {
        var tree = new Mock<ICollection<DeckNode>>(MockBehavior.Strict);
        var (vm, _, exporter, _) = CreateWorkflow(deckTree: tree.Object);
        exporter.Setup(service => service.SaveAsync("prepared content", vm.ExportPath))
            .Returns(Task.CompletedTask);

        await vm.ConfirmCommand.ExecuteAsync(null);

        tree.VerifyNoOtherCalls();
        exporter.Verify(service => service.PrepareTxt(It.IsAny<IEnumerable<CardEntity>>(), " - ", '\n'), Times.Once);
        exporter.Verify(service => service.SaveAsync("prepared content", vm.ExportPath), Times.Once);
        exporter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CsvExport_BuildsDeckPathsWhenConfirmedAndPassesThemToService()
    {
        var deck = new DeckNode(new Deck { Id = 1, Name = "Original" }, []);
        var (vm, _, exporter, _) = CreateWorkflow(deckTree: [deck]);
        vm.Format = ExportFormat.csv;
        deck.TempName = "Renamed before confirming";
        deck.CommitRename();
        exporter.VerifyNoOtherCalls();
        exporter.Setup(service => service.PrepareCsv(It.IsAny<IEnumerable<CardEntity>>(),
                It.Is<IReadOnlyDictionary<long, string>>(paths => paths.Count == 1
                    && paths[1] == "Renamed before confirming"), It.IsAny<IEnumerable<CsvExportColumn>>()))
            .Returns("prepared csv");
        exporter.Setup(service => service.SaveAsync("prepared csv", vm.ExportPath)).Returns(Task.CompletedTask);

        await vm.ConfirmCommand.ExecuteAsync(null);

        exporter.Verify(service => service.PrepareCsv(It.IsAny<IEnumerable<CardEntity>>(),
            It.Is<IReadOnlyDictionary<long, string>>(paths => paths.Count == 1
                && paths[1] == "Renamed before confirming"), It.IsAny<IEnumerable<CsvExportColumn>>()), Times.Once);
        exporter.Verify(service => service.SaveAsync("prepared csv", vm.ExportPath), Times.Once);
        exporter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Retry_ReusesPreparedContentAndOriginalPath_AndClosesOnlyAfterSuccess()
    {
        int closed = 0;
        var (vm, query, exporter, dialogs) = CreateWorkflow(() => closed++);
        string path = vm.ExportPath;
        exporter.SetupSequence(service => service.SaveAsync("prepared content", path))
            .ThrowsAsync(new IOException("File is locked"))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"))
            .Returns(Task.CompletedTask);
        dialogs.Setup(service => service.Show(It.IsAny<string>(), It.IsAny<string>(),
                DialogButtons.RetryCancel, DialogIcons.Error))
            .Callback(() =>
            {
                closed.Should().Be(0);
                vm.ExportPath = Path.ChangeExtension(path, ".changed.txt");
            })
            .Returns(DialogResult.Retry);

        await vm.ConfirmCommand.ExecuteAsync(null);

        closed.Should().Be(1);
        exporter.Verify(service => service.SaveAsync("prepared content", path), Times.Exactly(3));
        exporter.Verify(service => service.PrepareTxt(It.IsAny<IEnumerable<CardEntity>>(), " - ", '\n'), Times.Once);
        query.Verify(service => service.GetCardsWhere(It.IsAny<Filters>(),
            CardsOrder.Created, SortingDirection.Ascending), Times.Once);
        dialogs.Verify(service => service.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.RetryCancel, DialogIcons.Error), Times.Exactly(2));
        exporter.VerifyNoOtherCalls();
        query.VerifyNoOtherCalls();
        dialogs.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelAfterWriteFailure_KeepsPopupOpenAndDoesNotRetry(bool accessDenied)
    {
        int closed = 0;
        var (vm, _, exporter, dialogs) = CreateWorkflow(() => closed++);
        Exception failure = accessDenied ? new UnauthorizedAccessException() : new IOException();
        exporter.Setup(service => service.SaveAsync("prepared content", vm.ExportPath)).ThrowsAsync(failure);
        dialogs.Setup(service => service.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.RetryCancel, DialogIcons.Error)).Returns(DialogResult.Cancel);

        await vm.ConfirmCommand.ExecuteAsync(null);

        closed.Should().Be(0);
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
        exporter.Verify(service => service.SaveAsync("prepared content", vm.ExportPath), Times.Once);
        dialogs.Verify(service => service.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.RetryCancel, DialogIcons.Error), Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PreparationFailure_ShowsErrorWithoutSavingOrClosing()
    {
        int closed = 0;
        var (vm, _, exporter, dialogs) = CreateWorkflow(() => closed++);
        exporter.Setup(service => service.PrepareTxt(It.IsAny<IEnumerable<CardEntity>>(), " - ", '\n'))
            .Throws(new NotSupportedException("Unsupported note"));
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);

        await vm.ConfirmCommand.ExecuteAsync(null);

        closed.Should().Be(0);
        exporter.Verify(service => service.PrepareTxt(It.IsAny<IEnumerable<CardEntity>>(), " - ", '\n'), Times.Once);
        exporter.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task QueryFailure_ShowsErrorWithoutPreparingSavingOrClosing()
    {
        int closed = 0;
        var (vm, query, exporter, dialogs) = CreateWorkflow(() => closed++);
        query.Setup(service => service.GetCardsWhere(It.IsAny<Filters>(),
            CardsOrder.Created, SortingDirection.Ascending)).ThrowsAsync(new InvalidOperationException());
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);

        await vm.ConfirmCommand.ExecuteAsync(null);

        closed.Should().Be(0);
        exporter.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NoCards_ShowsInformationAndClosesWithoutPreparingOrSaving()
    {
        int closed = 0;
        var (vm, query, exporter, dialogs) = CreateWorkflow(() => closed++);
        query.Setup(service => service.GetCardsWhere(It.IsAny<Filters>(),
            CardsOrder.Created, SortingDirection.Ascending)).ReturnsAsync(Array.Empty<CardEntity>());
        dialogs.Setup(service => service.ShowNoCardsFoundToExport());

        await vm.ConfirmCommand.ExecuteAsync(null);

        closed.Should().Be(1);
        exporter.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowNoCardsFoundToExport(), Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NoCsvColumns_ShowsErrorWithoutQueryingPreparingOrSaving()
    {
        var (vm, query, exporter, dialogs) = CreateWorkflow();
        vm.Format = ExportFormat.csv;
        foreach (var column in vm.CsvColumns)
            column.IsSelected = false;
        dialogs.Setup(service => service.ShowError("Select at least one column to export.", "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);

        await vm.ConfirmCommand.ExecuteAsync(null);

        query.VerifyNoOtherCalls();
        exporter.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError("Select at least one column to export.", "Error", DialogButtons.OK), Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    private static (ExportVM VM, Mock<ICardQueryService> Query, Mock<IExportService> Exporter, 
    Mock<IDialogService> Dialogs) CreateWorkflow(Action? close = null, ICollection<DeckNode>? deckTree = null)
    {
        var query = new Mock<ICardQueryService>(MockBehavior.Strict);
        query.Setup(service => service.GetCardsWhere(It.IsAny<Filters>(),
            CardsOrder.Created, SortingDirection.Ascending)).ReturnsAsync(new[] { new CardEntity() });
        var exporter = new Mock<IExportService>(MockBehavior.Strict);
        exporter.Setup(service => service.PrepareTxt(It.IsAny<IEnumerable<CardEntity>>(), " - ", '\n'))
            .Returns("prepared content");
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        var vm = new ExportVM(7, query.Object, exporter.Object, dialogs.Object, close ?? (() => { }),
            [1], deckTree ?? [new DeckNode(new Deck { Id = 1, Name = "Deck" }, [])])
        {
            ExportPath = Path.Combine(Path.GetTempPath(), $"FlashMemo-{Guid.NewGuid():N}.txt")
        };

        return (vm, query, exporter, dialogs);
    }
}
