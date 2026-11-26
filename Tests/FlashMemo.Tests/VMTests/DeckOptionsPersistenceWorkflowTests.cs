using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Windows;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class DeckOptionsPersistenceWorkflowTests : IDisposable
{
    private const long UserId = 7;
    private const long OtherUserId = 8;
    private const long DeckId = 1;
    private readonly FakeDbFactory factory = new();
    private readonly IMapper mapper = Helpers.GetMapper();
    private readonly Mock<IDialogService> dialogs = new(MockBehavior.Strict);

    [Fact]
    public async Task SaveCustomPreset_PreservesOwnershipAndRemainsHiddenFromOtherUsers()
    {
        var (vm, original) = await CreateWorkflow();
        vm.CurrentOptions.DailyLimits.Reviews = 57;
        var expected = original with
        {
            DailyLimits = original.DailyLimits with { Reviews = 57 }
        };

        await vm.SaveChangesCommand.ExecuteAsync(null);

        var service = new DeckOptionsService(factory, mapper);
        (await service.GetFromDeck(DeckId)).Should().Be(expected);
        (await service.GetAllFromUser(OtherUserId))
            .Should().NotContain(preset => preset.Id == original.Id);
        var reopened = await OpenMenu();
        mapper.Map<DeckOptions>(reopened.CurrentOptions).Should().Be(expected);
        reopened.IsCurrentModified.Should().BeFalse();
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetToDefault_SaveAndReopen_PreservesCustomPresetIdentityAndAssignment()
    {
        var (vm, original) = await CreateWorkflow();
        int originalDeckCount = vm.CurrentOptions.DeckCount;
        var expected = DeckOptions.Default with
        {
            Id = original.Id,
            Name = original.Name,
            UserId = original.UserId
        };

        vm.ToDefaultCommand.Execute(null);

        mapper.Map<DeckOptions>(vm.CurrentOptions).Should().Be(expected);
        vm.PresetEditable.Should().BeTrue();
        vm.IsCurrentModified.Should().BeTrue();
        vm.CurrentOptions.DeckCount.Should().Be(originalDeckCount);
        vm.AllPresets.Should().ContainSingle(preset => preset.Id == DeckOptions.DefaultId);
        var service = new DeckOptionsService(factory, mapper);
        (await service.GetFromDeck(DeckId)).Should().Be(original);

        await vm.SaveChangesCommand.ExecuteAsync(null);

        (await service.GetFromDeck(DeckId)).Should().Be(expected);
        (await service.GetAllFromUser(UserId))
            .Single(preset => preset.Id == DeckOptions.DefaultId).Should().Be(DeckOptions.Default);
        var reopened = await OpenMenu();
        mapper.Map<DeckOptions>(reopened.CurrentOptions).Should().Be(expected);
        reopened.PresetEditable.Should().BeTrue();
        reopened.IsCurrentModified.Should().BeFalse();
        reopened.CurrentOptions.DeckCount.Should().Be(originalDeckCount);
        reopened.AllPresets.Should().ContainSingle(preset => preset.Id == DeckOptions.DefaultId);
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveDefault_AssignsItWithoutChangingItsNullOwnerOrTheCustomPreset()
    {
        var (vm, original) = await CreateWorkflow();
        var defaultPreset = vm.AllPresets.Single(preset => preset.Id == DeckOptions.DefaultId);

        await vm.ChangePresetCommand.ExecuteAsync(defaultPreset);

        mapper.Map<DeckOptions>(vm.CurrentOptions).Should().Be(DeckOptions.Default);

        await vm.SaveChangesCommand.ExecuteAsync(null);

        var service = new DeckOptionsService(factory, mapper);
        (await service.GetFromDeck(DeckId)).Should().Be(DeckOptions.Default);
        (await service.GetAllFromUser(UserId))
            .Single(preset => preset.Id == original.Id).Should().Be(original);
        var reopened = await OpenMenu();
        reopened.CurrentOptions.Id.Should().Be(DeckOptions.DefaultId);
        reopened.CurrentOptions.UserId.Should().BeNull();
        reopened.PresetEditable.Should().BeFalse();
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAndReopen_PreservesSixtyMinuteLearningStages()
    {
        var (vm, original) = await CreateWorkflow();
        vm.CurrentOptions.Scheduling.LearningStages.I = 60;
        vm.CurrentOptions.Scheduling.LearningStages.II = 60;
        vm.CurrentOptions.Scheduling.LearningStages.III = 60;
        var expected = original with
        {
            Scheduling = original.Scheduling with { LearningStages = new LearningStages(60, 60, 60) }
        };

        await vm.SaveChangesCommand.ExecuteAsync(null);

        var service = new DeckOptionsService(factory, mapper);
        (await service.GetFromDeck(DeckId)).Should().Be(expected);
        var reopened = await OpenMenu();
        reopened.CurrentOptions.Scheduling.LearningStages.I.Should().Be(60);
        reopened.CurrentOptions.Scheduling.LearningStages.II.Should().Be(60);
        reopened.CurrentOptions.Scheduling.LearningStages.III.Should().Be(60);
        mapper.Map<DeckOptions>(reopened.CurrentOptions).Should().Be(expected);
        reopened.IsCurrentModified.Should().BeFalse();
        dialogs.VerifyNoOtherCalls();
    }

    private async Task<(DeckOptionsMenuVM VM, DeckOptions Original)> CreateWorkflow()
    {
        await new TestsDbSeeder(factory).SeedDefault();
        var original = DeckOptions.CreateNew("Custom preset", UserId) with
        {
            Id = 11,
            DailyLimits = DeckOptions.DailyLimitsOpt.Default with { Lessons = 15, Reviews = 42 },
            Scheduling = DeckOptions.SchedulingOpt.Default with
            {
                GoodMultiplier = 3,
                GraduateDayCount = 5,
                LearningStages = new LearningStages(5, 10, 20)
            },
            Sorting = DeckOptions.SortingOpt.Default with { LessonsOrder = LessonOrder.LastModified }
        };
        await using (var db = factory.CreateDbContext())
        {
            db.Users.AddRange(
                new UserEntity { Id = UserId, Name = "First user", Options = UserOptions.CreateDefault() },
                new UserEntity { Id = OtherUserId, Name = "Second user", Options = UserOptions.CreateDefault() });
            db.DeckOptions.AddRange(
                mapper.Map<DeckOptionsEntity>(original),
                mapper.Map<DeckOptionsEntity>(DeckOptions.CreateNew("Other user's preset", OtherUserId) with { Id = 12 }));
            db.Decks.AddRange(
                new Deck { Id = DeckId, UserId = UserId, Name = "Custom deck", OptionsId = original.Id },
                new Deck { Id = 2, UserId = UserId, Name = "Default deck", OptionsId = DeckOptions.DefaultId },
                new Deck { Id = 3, UserId = OtherUserId, Name = "Other user's deck", OptionsId = 12 });
            await db.SaveChangesAsync();
        }

        return (await OpenMenu(), original);
    }

    private async Task<DeckOptionsMenuVM> OpenMenu()
    {
        await using var db = factory.CreateDbContext();
        var deck = await db.Decks.AsNoTracking().SingleAsync(deck => deck.Id == DeckId);
        var service = new DeckOptionsService(factory, mapper);
        var vm = new DeckOptionsMenuVM(mapper, new DeckOptVMBuilder(factory, mapper, service),
            service, new UserOptionsService(factory), deck, new VMEventBus(),
            NullLogger<DeckOptionsMenuVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        await vm.InitializeAsync();
        return vm;
    }

    public void Dispose() => factory.Dispose();
}