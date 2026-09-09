using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Windows;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class StatsVMTests
{
    private const long UserId = 7;

    [Fact]
    public async Task InitAsync_WhenPeriodStatsAreUnavailable_DisplaysNotAvailable()
    {
        var service = CreateStatsService(
            weekday: null,
            averageAnswerTime: TimeSpan.MinValue,
            peakHour: int.MinValue);

        var vm = await CreateViewModel(service.Object);

        vm.WeekDayWithMostReviewsInLastMonth.Should().Be("N/A");
        vm.AverageAnswerTimeInLastMonth.Should().Be("N/A");
        vm.MostReviewedHourOfDayInLastMonth.Should().Be("N/A");
    }

    [Fact]
    public async Task InitAsync_WithValidStats_FormatsValuesForDisplay()
    {
        var service = CreateStatsService(
            weekday: DayOfWeek.Wednesday,
            averageAnswerTime: TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(5),
            peakHour: 7,
            totalReviews: 1234,
            currentStreak: 3,
            longestStreak: 12);

        var vm = await CreateViewModel(service.Object);

        vm.WeekDayWithMostReviewsInLastMonth.Should().Be("Wednesday");
        vm.AverageAnswerTimeInLastMonth.Should().Be("02:05");
        vm.MostReviewedHourOfDayInLastMonth.Should().Be("07:00");
        vm.TotalReviewsEver.Should().Be(1234);
        vm.CurrentStreak.Should().Be(3);
        vm.LongestStreak.Should().Be(12);
    }

    [Fact]
    public async Task InitAsync_WhenAverageExceedsOneHour_IncludesHours()
    {
        var service = CreateStatsService(
            weekday: DayOfWeek.Monday,
            averageAnswerTime: new TimeSpan(hours: 1, minutes: 2, seconds: 3),
            peakHour: 23);

        var vm = await CreateViewModel(service.Object);

        vm.AverageAnswerTimeInLastMonth.Should().Be("01:02:03");
        vm.MostReviewedHourOfDayInLastMonth.Should().Be("23:00");
    }

    private static async Task<StatsVM> CreateViewModel(IStatsQueryService service)
    {
        var answerRatioVM = new AnswerRatioVM(UserId, service);
        var vm = new StatsVM(new VMEventBus(), UserId, answerRatioVM, service);

        await vm.InitAsync();
        return vm;
    }

    private static Mock<IStatsQueryService> CreateStatsService(
        DayOfWeek? weekday,
        TimeSpan averageAnswerTime,
        int peakHour,
        int totalReviews = 0,
        int currentStreak = 0,
        int longestStreak = 0)
    {
        var service = new Mock<IStatsQueryService>();

        service.Setup(x => x.DayWithMostReviewsInLastMonth(UserId))
            .ReturnsAsync(weekday);
        service.Setup(x => x.AvgAnswerTimeInLastMonth(UserId))
            .ReturnsAsync(averageAnswerTime);
        service.Setup(x => x.MostReviewedHourOfDayInLastMonth(UserId))
            .ReturnsAsync(peakHour);
        service.Setup(x => x.TotalReviewsEver(UserId))
            .ReturnsAsync(totalReviews);
        service.Setup(x => x.CurrentReviewStreak(UserId))
            .ReturnsAsync(currentStreak);
        service.Setup(x => x.LongestReviewStreak(UserId))
            .ReturnsAsync(longestStreak);

        return service;
    }
}
