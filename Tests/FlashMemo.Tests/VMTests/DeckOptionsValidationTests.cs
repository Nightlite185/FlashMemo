using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;

namespace FlashMemo.Tests.VMTests;

public class DeckOptionsValidationTests
{
    [Fact]
    public void Validate_ChecksUntouchedPropertiesInEveryGroup()
    {
        var vm = Helpers.GetMapper().Map<DeckOptionsVM>(DeckOptions.Default);
        vm.Sorting.LessonsOrder = (LessonOrder)99;
        vm.Scheduling = new DeckOptionsVM.SchedulingOpt();
        vm.Scheduling.HasErrors.Should().BeFalse();
        vm.DailyLimits.Reviews = -1;

        vm.Validate().Should().BeFalse();

        vm.Sorting.HasErrors.Should().BeTrue();
        vm.Scheduling.HasErrors.Should().BeTrue();
        vm.DailyLimits.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsValidSettingsAndRejectsUnfinishedRename()
    {
        var vm = Helpers.GetMapper().Map<DeckOptionsVM>(DeckOptions.Default);

        vm.Validate().Should().BeTrue();
        vm.IsRenaming = true;
        vm.Validate().Should().BeFalse();
        vm.IsRenaming = false;
        vm.Validate().Should().BeTrue();
    }
}