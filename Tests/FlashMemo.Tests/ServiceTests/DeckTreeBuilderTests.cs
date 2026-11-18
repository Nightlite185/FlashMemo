using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.ServiceTests;

public class DeckTreeBuilderTests
{
    [Fact]
    public async Task BuildCountedAsync_PropagatesConstructionFailureBeforeReturningAList()
    {
        Deck[] decks =
        [
            new() { Id = 1, UserId = 7, Name = "First deck" },
            new() { Id = 2, UserId = 7, Name = "Deck with missing counts" }
        ];
        var repo = new Mock<IDeckRepo>(MockBehavior.Strict);
        repo.Setup(service => service.ParentIdChildrenLookup(7))
            .ReturnsAsync(decks.ToLookup(deck => deck.ParentDeckId));
        var counter = new Mock<ICountingService>(MockBehavior.Strict);
        counter.Setup(service => service.StudyableCards(7))
            .ReturnsAsync(new Dictionary<long, CardsCount> { [1] = new() });
        var builder = new DeckTreeBuilder(repo.Object, counter.Object);

        Func<Task> build = () => builder.BuildCountedAsync(7);

        await build.Should().ThrowAsync<InvalidOperationException>().WithMessage("Missing card count");
        repo.Verify(service => service.ParentIdChildrenLookup(7), Times.Once);
        counter.Verify(service => service.StudyableCards(7), Times.Once);
    }
}