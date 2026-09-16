using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Services;
using FluentAssertions;

namespace FlashMemo.Tests.ServiceTests;

public class LearningPoolTests
{
    [Fact]
    public void TryPopEarly_ReturnsCardsInDueOrder()
    {
        var pool = new LearningPool<FakeLearningCard>();
        var later = LearningCard(DateTime.Now.AddHours(2));
        var earlier = LearningCard(DateTime.Now.AddHours(1));

        pool.Add(later);
        pool.Add(earlier);

        pool.TryPopEarly().Should().BeSameAs(earlier);
        pool.TryPopEarly().Should().BeSameAs(later);
        pool.TryPopEarly().Should().BeNull();
    }

    [Fact]
    public void InjectDueInto_PutsDueCardsOnTopInDueOrderAndKeepsFutureCardsPooled()
    {
        var pool = new LearningPool<FakeLearningCard>();
        var existing = LearningCard(DateTime.Now.AddHours(4));
        var firstDue = LearningCard(DateTime.Now.AddMinutes(1));
        var secondDue = LearningCard(DateTime.Now.AddMinutes(2));
        var future = LearningCard(DateTime.Now.AddHours(1));

        pool.Add(secondDue);
        pool.Add(future);
        pool.Add(firstDue);

        firstDue.IsDueNow = true;
        secondDue.IsDueNow = true;

        var studyStack = new Stack<FakeLearningCard>();
        studyStack.Push(existing);

        pool.InjectDueInto(studyStack);

        studyStack.Pop().Should().BeSameAs(firstDue);
        studyStack.Pop().Should().BeSameAs(secondDue);
        studyStack.Pop().Should().BeSameAs(existing);
        pool.Count.Should().Be(1);
        pool.TryPopEarly().Should().BeSameAs(future);
    }

    [Fact]
    public void Add_RejectsCardWithoutDueDate()
    {
        var pool = new LearningPool<FakeLearningCard>();
        var card = LearningCard(null);

        var act = () => pool.Add(card);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Add_RejectsCardThatIsAlreadyDue()
    {
        var pool = new LearningPool<FakeLearningCard>();
        var card = LearningCard(DateTime.Now.AddMinutes(-1));
        card.IsDueNow = true;

        var act = () => pool.Add(card);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(CardState.New)]
    [InlineData(CardState.Review)]
    public void Add_RejectsCardOutsideLearningState(CardState state)
    {
        var pool = new LearningPool<FakeLearningCard>();
        var card = LearningCard(DateTime.Now.AddHours(1));
        card.State = state;

        var act = () => pool.Add(card);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Clear_RemovesAllCards()
    {
        var pool = new LearningPool<FakeLearningCard>();
        pool.Add(LearningCard(DateTime.Now.AddHours(1)));
        pool.Add(LearningCard(DateTime.Now.AddHours(2)));

        pool.Clear();

        pool.Count.Should().Be(0);
        pool.TryPopEarly().Should().BeNull();
    }

    private static FakeLearningCard LearningCard(DateTime? due) => new()
    {
        Due = due,
        State = CardState.Learning
    };

    private sealed class FakeLearningCard : ILearningPoolCard
    {
        public DateTime? Due { get; init; }
        public bool IsDueNow { get; set; }
        public CardState State { get; set; }
    }
}
