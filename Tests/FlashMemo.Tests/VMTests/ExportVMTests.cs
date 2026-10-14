using System.IO;
using FlashMemo.Services;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Popups;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class ExportVMTests
{
    [Fact]
    public void DefaultFilename_MatchesTextFormat()
    {
        var vm = CreateViewModel();

        Path.GetExtension(vm.ExportPath).Should().Be(".txt");
        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("export", false)]
    [InlineData("export.csv", false)]
    [InlineData("export.txt.csv", false)]
    [InlineData("invalid?.txt", false)]
    [InlineData("export.txt", true)]
    [InlineData("export.TXT", true)]
    public void ExportPath_ReportsErrorsAndControlsConfirmation(string fileName, bool valid)
    {
        var vm = CreateViewModel();
        vm.ExportPath = string.IsNullOrWhiteSpace(fileName)
            ? fileName
            : Path.Combine(Path.GetTempPath(), fileName);

        vm.GetErrors(nameof(ExportVM.ExportPath)).Any().Should().Be(!valid);
        vm.ConfirmCommand.CanExecute(null).Should().Be(valid);
    }

    [Fact]
    public void FolderPath_IsRejected()
    {
        var vm = CreateViewModel();
        vm.ExportPath = Path.GetTempPath();

        vm.GetErrors(nameof(ExportVM.ExportPath)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CorrectingPath_ClearsErrorsAndNotifiesCommand()
    {
        var vm = CreateViewModel();
        int commandChanges = 0;
        vm.ConfirmCommand.CanExecuteChanged += (_, _) => commandChanges++;

        vm.ExportPath = Path.Combine(Path.GetTempPath(), "export.csv");
        vm.ExportPath = Path.Combine(Path.GetTempPath(), "export.txt");

        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
        commandChanges.Should().BeGreaterThanOrEqualTo(2);
    }

    [Theory]
    [InlineData(NoteSeparator.Tab, "\t")]
    [InlineData(NoteSeparator.Enter, "\n")]
    [InlineData(NoteSeparator.Space, " ")]
    [InlineData(NoteSeparator.SemiColon, ";")]
    public void MatchingSeparators_AreReportedOnBothInputs_AndClearWhenNoteSeparatorChanges(
        NoteSeparator separator, string text)
    {
        var vm = CreateViewModel();
        vm.NoteSep = separator;
        vm.FieldSeparator = text;

        vm.GetErrors(nameof(ExportVM.FieldSeparator)).Should().NotBeEmpty();
        vm.GetErrors(nameof(ExportVM.NoteSep)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();

        vm.NoteSep = separator == NoteSeparator.Space ? NoteSeparator.Enter : NoteSeparator.Space;

        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ChangingFieldSeparator_ClearsBothSeparatorErrors()
    {
        var vm = CreateViewModel();
        vm.FieldSeparator = ";";
        vm.NoteSep = NoteSeparator.SemiColon;

        vm.HasErrors.Should().BeTrue();

        vm.FieldSeparator = " - ";

        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EmptyFieldSeparator_IsRejected()
    {
        var vm = CreateViewModel();
        vm.FieldSeparator = "";

        vm.GetErrors(nameof(ExportVM.FieldSeparator)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ChangingFormat_RevalidatesTheExtension()
    {
        var vm = CreateViewModel();
        vm.Format = (ExportFormat)99;
        vm.ExportPath = Path.Combine(Path.GetTempPath(), "export.csv");

        vm.GetErrors(nameof(ExportVM.Format)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();

        vm.Format = ExportFormat.txt;

        vm.GetErrors(nameof(ExportVM.Format)).Should().BeEmpty();
        vm.GetErrors(nameof(ExportVM.ExportPath)).Should().NotBeEmpty();

        vm.ExportPath = Path.Combine(Path.GetTempPath(), "export.txt");

        vm.HasErrors.Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void InvalidNoteSeparator_IsRejected()
    {
        var vm = CreateViewModel();
        vm.NoteSep = (NoteSeparator)99;

        vm.GetErrors(nameof(ExportVM.NoteSep)).Should().NotBeEmpty();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();

        vm.NoteSep = NoteSeparator.Enter;

        vm.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task ExecutingInvalidExport_DoesNotQueryCardsOrClosePopup()
    {
        var query = new Mock<ICardQueryService>(MockBehavior.Strict);
        var serializer = new Mock<INoteSerializer>(MockBehavior.Strict);
        bool closed = false;
        var vm = new ExportVMF(query.Object, serializer.Object).Create(1, [1], () => closed = true);
        vm.FieldSeparator = "\n";

        await vm.ConfirmCommand.ExecuteAsync(null);

        query.VerifyNoOtherCalls();
        serializer.VerifyNoOtherCalls();
        closed.Should().BeFalse();
    }

    private static ExportVM CreateViewModel()
        => new ExportVMF(Mock.Of<ICardQueryService>(), Mock.Of<INoteSerializer>())
            .Create(1, [1], () => { });
}
