using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;

namespace FlashMemo.Tests.ServiceTests;

public class UserOptionsServiceTests : IDisposable
{
    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task DeleteConfirmationPreferences_RoundTripThroughDatabase()
    {
        var seeder = new TestsDbSeeder(factory);
        await seeder.SeedDefault();
        var user = seeder.SeedUser();
        var service = new UserOptionsService(factory);

        var updated = UserOptions.CreateDefault() with
        {
            ConfirmCardDeletion = false,
            ConfirmDeckDeletion = true,
            ConfirmPresetDeletion = false,
            ConfirmUserDeletion = true
        };

        await service.Update(user.Id, updated);

        var persisted = await service.GetFromUser(user.Id);
        persisted.Should().Be(updated);
    }

    [Fact]
    public void DeleteConfirmations_AreEnabledByDefault()
    {
        var defaults = UserOptions.CreateDefault();

        defaults.ConfirmCardDeletion.Should().BeTrue();
        defaults.ConfirmDeckDeletion.Should().BeTrue();
        defaults.ConfirmPresetDeletion.Should().BeTrue();
        defaults.ConfirmUserDeletion.Should().BeTrue();
    }

    public void Dispose() => factory.Dispose();
}
