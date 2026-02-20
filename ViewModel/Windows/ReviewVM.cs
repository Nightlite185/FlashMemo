using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Model.Domain;
using System.Diagnostics;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;
using FlashMemo.Repositories;
using FlashMemo.Model;

namespace FlashMemo.ViewModel.Windows;
    
public partial class ReviewVM: NavBaseVM, IPopupHost
{
    public ReviewVM(ICardService cs, ICardQueryService cqs, long userId, IDeckMeta deck, DeckOptions deckOpt, ICardRepo cr)
    {
        this.userId = userId;
        this.Deck = deck;

        deckOptions = deckOpt;

        cardService = cs;
        cardQuery = cqs;
        cardRepo = cr;
        learningPool = new();
        ReviewHistory = [];

        stopWatch = new();
    }
    
    #region public properties
    
    [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(IsCardLoaded), nameof(ReviewedCount))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    [ObservableProperty] public partial CardVM? CurrentCard { get; set; }

    [NotifyPropertyChangedFor(nameof(CanReview), nameof(CanRevealAnswer))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    [ObservableProperty] public partial bool AnswerRevealed { get; set; } = false;

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
    public int ReviewedCount => InitialCount
        - cards.Count
        - learningPool.Count
        - (CurrentCard is null ? 0 : 1);

    [ObservableProperty]
    public partial string ElapsedTime { get; set; } = "00:00";

    [ObservableProperty]
    public partial SchedulePermutations? SchedulePerms { get; set; }

    [ObservableProperty]
    public partial bool IsSessionFinished { get; private set; }

    public PopupVMBase? CurrentPopup { get; set; }
    public CardsCountVM CardsCount { get; private set; } = null!;
    public CardCtxMenuVM CtxMenuVM { get; private set; } = null!;
    public Queue<CardEntity> ReviewHistory { get; private init; }
    public event Func<Task>? OnDecksNavRequest;
    #endregion

    #region methods
    internal async Task InitAsync(CardCtxMenuVM ctxMenu) //* called in factory
    {
        this.CtxMenuVM = ctxMenu;

        (var freshCards, var count) = await cardQuery
            .GetForStudy(Deck.Id);

        this.cards = new (freshCards.Select(c => new CardVM(c)));
        this.CardsCount = (CardsCountVM)count;
        this.InitialCount = cards.Count;

        ShowNextCard();
        stopWatch.Start();
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
    public async Task ReloadCurrentCardAsync()
    {
        if (CurrentCard is null)
            throw new InvalidOperationException(
            "Can't reload current card since there was none loaded atm.");

        CurrentCard = new CardVM(
            await cardRepo.GetById(CurrentCard.Id));
    }
    private void ShowNextCard()
    {
        if (cards.TryPop(out var popped))
            CurrentCard = popped;

        else if (learningPool.TryPopEarly() is CardVM card)
            CurrentCard = card;

        else
        {
            CurrentCard = null;
            IsSessionFinished = true;
        }
    }

    private void UpdateOnReview(CardEntity reviewed, ScheduleInfo schedule)
    {
        learningPool.InjectDueInto(cards);

        if (schedule.State == CardState.Learning)
            learningPool.Add(new(reviewed));

        CardsCount.UpdateCount(cards, learningPool.Count);

        ReviewHistory.Enqueue(reviewed);

        if (ReviewHistory.Count > HistoryCap)
            ReviewHistory.Dequeue();
    }

    internal void UpdateTime()
        => ElapsedTime = $"{(int)stopWatch.Elapsed.TotalMinutes:00}:{stopWatch.Elapsed.Seconds:00}";

    private async Task ReviewAsync(Answers answer)
    {
        stopWatch.Reset();

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

        UpdateOnReview(reviewed, updatedSchedule);

        ShowNextCard();
        stopWatch.Start();
    }
    
    #endregion

    #region private things
    private readonly long userId;
    private readonly DeckOptions deckOptions;
    private readonly ICardService cardService;
    private readonly ICardQueryService cardQuery;
    private readonly ICardRepo cardRepo;
    private readonly LearningPool<CardVM> learningPool;
    private Stack<CardVM> cards = null!;
    private readonly Stopwatch stopWatch = null!;
    private const int HistoryCap = 10;
    
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

        await NavigateTo(new EditCardNavRequest(
            CurrentCard!.Id, userId, this));
    }

    [RelayCommand]
    private async Task ShowEditFromHistory(ICard card)
    {
        await NavigateTo(new EditCardNavRequest(
            card.Id, userId));
    }

    [RelayCommand]
    private void ShowCtxMenu()
    {
        if (CurrentCard is null)
            throw new InvalidOperationException(
            "Tried opening ctx menu with no currently loaded card.");

        CtxMenuVM.OpenMenu([CurrentCard]);
    }

    [RelayCommand]
    private async Task ShowDecks() => OnDecksNavRequest?.Invoke();
    #endregion
}