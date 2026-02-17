using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Model.Domain;
using System.Windows.Threading;
using System.Diagnostics;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;
    
public partial class ReviewVM: NavBaseVM, IPopupHost, IReloadHandler
{
    public ReviewVM(ICardService cs, ICardQueryService cqs, long userId, IDeckMeta deck, DeckOptions deckOpt)
    {
        this.userId = userId;
        this.Deck = deck;

        deckOptions = deckOpt;

        cardService = cs;
        cardQuery = cqs;
        learningPool = new();

        stopWatch = new();
        timer = new() { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => UpdateTime();
    }
    
    #region public properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(IsCardLoaded), nameof(ReviewedCount))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    public partial CardEntity? CurrentCard { get; set; } // TODO: change type later to ICard

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReview), nameof(CanRevealAnswer))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    public partial bool AnswerRevealed { get; set; } = false;

    public IDeckMeta Deck { get; init; }
    public string FrontContent => CurrentCard?.FrontContent
        ?? throw new InvalidOperationException(
        $"tried to access {nameof(FrontContent)} property, but no card was loaded atm.");
    public string BackContent
    {
        get
        {
            if (CurrentCard is null) throw new InvalidOperationException(
                $"tried to access {nameof(BackContent)} property, but no card was loaded atm.");

            return CurrentCard.BackContent ?? "うら空っぽみたい"; // just for testing so ik if its actually empty or sth bugged
        }
    }
    
    public int InitialCount { get; private set; }
    public int ReviewedCount
    {
        get
        {
            if (cards.Count == 0 && learningPool.Count == 0)
                return InitialCount;

            return InitialCount - cards.Count - learningPool.Count - 1;
        }
    }

    [ObservableProperty]
    public partial string ElapsedTime { get; set; } = "00:00";

    [ObservableProperty]
    public partial SchedulePermutations? SchedulePerms { get; set; }

    [ObservableProperty]
    public partial bool IsSessionFinished { get; private set; }

    public PopupVMBase? CurrentPopup { get; set; }
    public CardsCountVM CardsCount { get; private set; } = null!;
    public event Func<Task>? OnDecksNavRequest;
    #endregion

    #region methods
    internal async Task InitAsync(CardCtxMenuVM ctxMenu) //* called in factory
    {
        cardCtxMenu = ctxMenu;

        (var freshCards, var count) = await cardQuery
            .GetForStudy(Deck.Id);

        this.cards = new(freshCards);
        this.CardsCount = (CardsCountVM)count;
        this.InitialCount = cards.Count;

        ShowNextCard();
        StartTimer();
    }
    private ScheduleInfo GetScheduleInfo(Answers answer)
    {
        if (SchedulePerms is null)
            throw new NullReferenceException();

        return answer switch
        {
            Answers.Easy => SchedulePerms.Easy,
            Answers.Good => SchedulePerms.Good,
            Answers.Hard => SchedulePerms.Hard,
            Answers.Again => SchedulePerms.Again,

            _ => throw new ArgumentOutOfRangeException(
                nameof(answer), 
                $"Invalid Answers enum value: {answer}")
        };
    }
    public Task ReloadAsync(ReloadTargets rt)
    {
        throw new NotImplementedException();
    }
    private void ShowNextCard()
    {
        if (cards.TryPop(out var popped))
            CurrentCard = popped;

        else if (learningPool.TryPopEarly() is CardEntity card)
            CurrentCard = card;

        else
        {
            CurrentCard = null;
            IsSessionFinished = true;
        }
    }
    private void StopTimer()
    {
        stopWatch.Reset();
        timer.Stop();

        UpdateTime();
    }
    private void StartTimer()
    {
        stopWatch.Start();
        timer.Start();
    }
    private void UpdateTime()
        => ElapsedTime = $"{(int)stopWatch.Elapsed.TotalMinutes:00}:{stopWatch.Elapsed.Seconds:00}";
    private async Task ReviewAsync(Answers answer)
    {
        StopTimer();

        if (!IsCardLoaded)
            throw new InvalidOperationException(
            "Review called without an active card.");

        AnswerRevealed = false;

        var updatedSchedule = GetScheduleInfo(answer);
        SchedulePerms = null;
        
        var reviewed = await cardService.ReviewCardAsync(
            CurrentCard!.Id,
            updatedSchedule, answer,
            stopWatch.Elapsed);

        learningPool.InjectDueInto(cards);

        if (updatedSchedule.State == CardState.Learning)
            learningPool.Add(reviewed);

        CardsCount.UpdateCount(cards, learningPool.Count);

        ShowNextCard();
        StartTimer();
    }
    
    #endregion

    #region private things
    private readonly long userId;
    private readonly DeckOptions deckOptions;
    private readonly ICardService cardService;
    private CardCtxMenuVM cardCtxMenu = null!;
    private readonly ICardQueryService cardQuery;
    private readonly LearningPool learningPool;
    private Stack<CardEntity> cards = null!;
    private readonly DispatcherTimer timer = null!;
    private readonly Stopwatch stopWatch = null!;

    private bool IsCardLoaded => CurrentCard is not null;
    private bool CanReview
        => IsCardLoaded && AnswerRevealed;
    private bool CanRevealAnswer
        => IsCardLoaded && !AnswerRevealed;
    #endregion

    #region ICommands
    
    [RelayCommand(CanExecute = nameof(CanRevealAnswer))]
    private void RevealAnswer()
    {
        // show answer layer here
        AnswerRevealed = true;

        SchedulePerms = Scheduler.GetForecast(
            CurrentCard ?? throw new NullReferenceException(),
            deckOptions.Scheduling);
    }
    
    #region answer commands
    [RelayCommand(CanExecute = nameof(CanReview))]
    private async Task AgainAnswer()
        => await ReviewAsync(Answers.Again);

    
    [RelayCommand(CanExecute = nameof(CanReview))]
    private async Task HardAnswer() 
        => await ReviewAsync(Answers.Hard);

    
    [RelayCommand(CanExecute = nameof(CanReview))]
    private async Task GoodAnswer()
        => await ReviewAsync(Answers.Good);


    [RelayCommand(CanExecute = nameof(CanReview))]
    private async Task EasyAnswer() 
        => await ReviewAsync(Answers.Easy);
    #endregion

    [RelayCommand(CanExecute = nameof(IsCardLoaded))]
    private async Task ShowCardEdit()
    {
        if (!IsCardLoaded) throw new InvalidOperationException(
        "tried to open card editor, but no card was loaded.");

        await NavigateTo(new EditCardNavRequest(CurrentCard!.Id, userId));

        //TODO: when coming back from the editor to this VM, gotta reload the card and pull any possible changed from db;
    }

    [RelayCommand]
    private async Task ShowDecks() => OnDecksNavRequest?.Invoke();
    #endregion
}