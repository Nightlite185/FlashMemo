using FlashMemo.Model.Domain;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;

namespace FlashMemo.Tests.VMTests;

public class DeckOptionsValidationTests
{
    [Fact]
    public void Validate_ChecksUntouchedSchedulingProperties()
    {
        var vm = Helpers.GetMapper().Map<DeckOptionsVM>(DeckOptions.Default);
        vm.Scheduling = new DeckOptionsVM.SchedulingOpt();
        vm.Scheduling.HasErrors.Should().BeFalse();

        vm.Validate().Should().BeFalse();

        vm.Scheduling.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsValidSettingsIndependentlyOfRenameState()
    {
        var vm = Helpers.GetMapper().Map<DeckOptionsVM>(DeckOptions.Default);

        vm.Validate().Should().BeTrue();
        vm.IsRenaming = true;
        vm.Validate().Should().BeTrue();
        vm.IsRenaming = false;
        vm.Validate().Should().BeTrue();
    }
}