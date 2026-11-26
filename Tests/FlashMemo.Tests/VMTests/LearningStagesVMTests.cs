using FlashMemo.Model.Domain;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;

namespace FlashMemo.Tests.VMTests;

public class LearningStagesVMTests
{
    [Theory]
    [InlineData(59, 60, 1)]
    [InlineData(60, 1, 60)]
    [InlineData(60, 75, 99)]
    public void Mapping_PreservesCompleteMinuteCountsInAllStages(int first, int second, int third)
    {
        var stages = new LearningStages(first, second, third);
        var preset = DeckOptions.Default with
        {
            Scheduling = DeckOptions.SchedulingOpt.Default with { LearningStages = stages }
        };
        var mapper = Helpers.GetMapper();

        var vm = mapper.Map<DeckOptionsVM>(preset);

        vm.Scheduling.LearningStages.I.Should().Be(first);
        vm.Scheduling.LearningStages.II.Should().Be(second);
        vm.Scheduling.LearningStages.III.Should().Be(third);
        mapper.Map<DeckOptions>(vm).Scheduling.LearningStages.Should().Be(stages);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void Validate_AcceptsInclusiveBoundsInAllStages(int minutes)
    {
        var vm = new LearningStagesVM(new LearningStages(minutes, minutes, minutes));

        vm.Validate().Should().BeTrue();
        vm.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void Validate_ReportsUntouchedStagesAndCorrectionsClearTheirErrors()
    {
        var vm = new LearningStagesVM();

        vm.Validate().Should().BeFalse();

        vm.GetErrors(nameof(vm.I)).Should().NotBeEmpty();
        vm.GetErrors(nameof(vm.II)).Should().NotBeEmpty();
        vm.GetErrors(nameof(vm.III)).Should().NotBeEmpty();

        vm.I = 1;
        vm.II = 30;
        vm.III = 60;

        vm.HasErrors.Should().BeFalse();
        vm.Validate().Should().BeTrue();
    }

    [Theory]
    [InlineData(LearningStage.I, 0)]
    [InlineData(LearningStage.I, 61)]
    [InlineData(LearningStage.II, 0)]
    [InlineData(LearningStage.II, 61)]
    [InlineData(LearningStage.III, 0)]
    [InlineData(LearningStage.III, 61)]
    public void EditingStage_ReportsOutOfRangeValuesAndClearsErrorsWhenCorrected(LearningStage stage, int minutes)
    {
        var vm = new LearningStagesVM(new LearningStages(4, 8, 15));
        var errorsChanged = new List<string?>();
        vm.ErrorsChanged += (_, e) => errorsChanged.Add(e.PropertyName);

        SetStage(vm, stage, minutes);

        vm.HasErrors.Should().BeTrue();
        vm.GetErrors(stage.ToString()).Should().NotBeEmpty();
        errorsChanged.Should().Contain(stage.ToString());
        vm.Validate().Should().BeFalse();

        errorsChanged.Clear();
        SetStage(vm, stage, 60);

        vm.HasErrors.Should().BeFalse();
        vm.GetErrors(stage.ToString()).Should().BeEmpty();
        errorsChanged.Should().Contain(stage.ToString());
        vm.Validate().Should().BeTrue();
    }

    private static void SetStage(LearningStagesVM vm, LearningStage stage, int minutes)
    {
        switch (stage)
        {
            case LearningStage.I: vm.I = minutes; break;
            case LearningStage.II: vm.II = minutes; break;
            case LearningStage.III: vm.III = minutes; break;
        }
    }
}