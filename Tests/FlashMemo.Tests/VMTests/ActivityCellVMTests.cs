using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;

namespace FlashMemo.Tests.VMTests;

public class ActivityCellVMTests
{
    [Theory]
    [InlineData(2026, 1, 5, 0)]
    [InlineData(2026, 1, 6, 1)]
    [InlineData(2026, 1, 7, 2)]
    [InlineData(2026, 1, 8, 3)]
    [InlineData(2026, 1, 9, 4)]
    [InlineData(2026, 1, 10, 5)]
    [InlineData(2026, 1, 11, 6)]
    public void WeekdayIndex_UsesMondayFirstRows(
        int year,
        int month,
        int day,
        int expectedIndex)
    {
        var cell = new ActivityCellVM
        {
            Date = new DateOnly(year, month, day),
            ReviewCount = 0
        };

        cell.WeekdayIndex.Should().Be(expectedIndex);
    }
}
