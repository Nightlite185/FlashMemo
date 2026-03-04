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
    
public partial class ReviewVM: NavBaseVM, IPopupHost, IClosedHandler, IFocusState, ICtxMenuHost
{
    internal ReviewVM(ICardService cs, ICardQueryService cqs, long userId, IDeckMeta deck,
                        DeckOptions deckOpt, ICardRepo cr, IDomainEventBus bus)
    {
        this.userId = userId;
        this.Deck = deck;
        
        deckOptions = deckOpt;

        eventBus = bus;
        cardService = cs;
        cardQuery = cqs;
        cardRepo = cr;
        ReviewHistory = [];
        learningPool = new();

        stopWatch = new();
    }
    
    #region public properties
    
    [NotifyPropertyChangedFor(nameof(IsCardLoaded))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    [ObservableProperty] public partial CardVM? CurrentCard { get; set; }

    [NotifyPropertyChangedFor(nameof(CanReview), nameof(CanRevealAnswer))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    [ObservableProperty] public partial bool AnswerRevealed { get; set; } = false;

    public IDeckMeta Deck { get; init; }
    [ObservableProperty] public partial int InitialCount { get; private set; }
    [ObservableProperty] public partial int ReviewedCount  { get; private set; }
    
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
        eventBus.DomainChanged += OnDomainChanged;

        (var freshCards, var count) = await cardQuery
            .GetForStudy(Deck.Id);

        this.activeCards = new (freshCards.Select(c => new CardVM(c)));
        this.CardsCount = (CardsCountVM)count;

        allSessionCards = [..activeCards];
        InitialCount = activeCards.Count;

        ShowNextCard();
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
        learningPool.InjectDueInto(activeCards);
        
        CurrentCard = activeCards.TryPop(out var fromStack)
            ? fromStack
            : learningPool.TryPopEarly();

        if (CurrentCard is null)
            IsSessionFinished = true;

        else if (CurrentCard.IsDeleted)
            ShowNextCard();

        else stopWatch.Start();
    }
    private void UpdateOnReview(CardEntity reviewed, ScheduleInfo schedule)
    {
        if (schedule.State is CardState.Learning)
            learningPool.Add(new(reviewed));

        else if (schedule.State is CardState.Review)
            ReviewedCount++;

        else throw new InvalidOperationException(
            "Card after reviewing can't still have lesson state.");

        CardsCount.UpdateCount(activeCards, learningPool.Count);

        ReviewHistory.Enqueue(reviewed);

        if (ReviewHistory.Count > HistoryCap)
            ReviewHistory.Dequeue();
    }
    internal void UpdateTime() => ElapsedTime = 
        $"{(int)stopWatch.Elapsed.TotalMinutes:00}:{stopWatch.Elapsed.Seconds:00}";
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
    }
    
    private async Task OnDomainChanged()
    {
        if (!isFocused)
            isDirty = true;
    }
    public async Task OnFocusGained()
    {
        if (isDirty)
        {
            await SoftReloadAsync();
            isDirty = false;
        }
    }
    public void OnFocusLost() => isFocused = false;
    public void OnClosed() => eventBus.DomainChanged -= OnDomainChanged;

    private async Task SoftReloadAsync()
    {
        stopWatch.Reset();

        var removedIds = await cardQuery.RemovedFromSubset(
            allSessionCards.Select(c => c.Id));

        var removedCards = allSessionCards
            .Where(c => removedIds.Contains(c.Id))
            .ToArray();

        foreach (var card in removedCards)
            card.IsDeleted = true;
        
        InitialCount -= removedCards.Length;

        CardsCount.UpdateCount(activeCards, learningPool.Count);

        ShowNextCard();
    }

    public void OnActionExecuted(CtxMenuAction action)
    {
        if (CurrentCard is null) throw new InvalidOperationException(
            "Couldn't have called ctx menu action if current card is null");

        if (action is CtxMenuAction.Relocate
        or CtxMenuAction.Reschedule
        or CtxMenuAction.Bury
        or CtxMenuAction.Suspend
        or CtxMenuAction.Delete
        or CtxMenuAction.Forget)
        {
            CurrentCard.IsDeleted = true;
            ShowNextCard();

            CardsCount.UpdateCount(
                activeCards, learningPool.Count);

            ReviewedCount++;
        }
    }

    #endregion

    #region private things
    private readonly long userId;
    private DeckOptions deckOptions;
    private readonly ICardService cardService;
    private readonly ICardQueryService cardQuery;
    private readonly ICardRepo cardRepo;
    private readonly IDomainEventBus eventBus;
    private readonly LearningPool<CardVM> learningPool;
    private List<CardVM> allSessionCards = [];
    private Stack<CardVM> activeCards = null!;
    private readonly Stopwatch stopWatch = null!;
    private const int HistoryCap = 10;
    
    private bool IsCardLoaded => CurrentCard is not null;
    private bool isDirty;
    private bool isFocused = true;
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