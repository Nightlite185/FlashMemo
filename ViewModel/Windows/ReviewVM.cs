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
using FlashMemo.ViewModel.Other;
using System.Collections.ObjectModel;

namespace FlashMemo.ViewModel.Windows;

public partial class ReviewVM: BaseVM, IPopupHost, IFocusState, ICtxMenuHost, ICardsSource<CardVM>
{
    internal ReviewVM(ICardService cs, ICardQueryService cqs, long userId, IDeckMeta deck,
                        DeckOptions deckOpt, ICardRepo cr, IVMEventBus bus, IUserOptionsService uos): base(bus)
    {
        this.userId = userId;
        this.Deck = deck;
        uOptionsService = uos;
        
        deckOptions = deckOpt;
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

    [ObservableProperty] public partial int InitialCount { get; private set; }
    [ObservableProperty] public partial int ReviewedCount  { get; private set; }
    [ObservableProperty] public partial string ElapsedTime { get; set; } = "00:00";
    [ObservableProperty] public partial SchedulePermutations? SchedulePerms { get; set; }
    [ObservableProperty] public partial bool IsSessionFinished { get; private set; }
    [ObservableProperty] public partial PopupVMBase? CurrentPopup { get; set; }

    public CardsCountVM CardsCount { get; private set; } = null!;
    public CardCtxMenuVM CtxMenuVM { get; private set; } = null!;
    public ObservableCollection<CardEntity> ReviewHistory { get; private init; }
    public event Func<Task>? OnDecksNavRequest;
    public IEnumerable<CardVM> Cards => allSessionCards;
    public bool TimerVisible => userOptions.ShowReviewTimer;
    public IDeckMeta Deck { get; init; }
    #endregion

    #region methods
    internal async Task InitAsync(CardCtxMenuVM ctxMenu) //* called in factory
    {
        this.CtxMenuVM = ctxMenu;
        eventBus.DomainChanged += OnDomainChanged;
        eventBus.UserOptionsChanged += OnUserOptChanged;

        userOptions = await uOptionsService
            .GetFromUser(userId);

        (var freshCards, var count) = await cardQuery
            .GetForStudy(Deck.Id);

        this.activeCards = new (freshCards
            .Select(c => new CardVM(c)));

        this.CardsCount = new(count, this);

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
    private void UpdateOnReview(CardEntity reviewed)
    {
        if (CurrentCard is null) 
            throw new NullReferenceException(
            "Tried to update on review, but current card is null.");

        CurrentCard.Refresh(reviewed);

        switch (CurrentCard.State)
        {
            case CardState.Learning:
                learningPool.Add(CurrentCard);
                break;

            case CardState.Review:
                ReviewedCount++;
                break;

            default: throw new InvalidOperationException(
            "Card after reviewing can't still have lesson state.");
        }

        CardsCount.UpdateCount();

        // compare last item's id with reviewed, to prevent unnecessary duplicates.
        if (ReviewHistory.LastOrDefault()?.Id != reviewed.Id)
            ReviewHistory.Add(reviewed);

        if (ReviewHistory.Count > HistoryCap)
            ReviewHistory.RemoveAt(0);
    }
    internal void UpdateTime() => ElapsedTime = 
        $"{(int)stopWatch.Elapsed.TotalMinutes:00}:{stopWatch.Elapsed.Seconds:00}";
    private async Task ReviewAsync(Answers answer)
    {
        var time = stopWatch.Elapsed;
        stopWatch.Reset();

        if (!IsCardLoaded)
            throw new InvalidOperationException(
            "Review called without an active card.");

        AnswerRevealed = false;

        var updatedSchedule = GetScheduleInfo(answer);
        SchedulePerms = null;
        
        var reviewed = await cardService.ReviewCardAsync(
            CurrentCard!.Id,
            updatedSchedule,
            answer, time);

        UpdateOnReview(reviewed);
        ShowNextCard();
    }

    partial void OnCurrentPopupChanged(PopupVMBase? value)
    {
        if (CurrentPopup is null)
        {
            if (!stopWatch.IsRunning && IsCardLoaded)
                stopWatch.Start();
        }

        else stopWatch.Stop();
    }
    
    protected override async Task ReloadDomainAsync()
    {
        // 1. Fetch fresh entities for all session card IDs in one query
        var freshEntities = await cardRepo.GetByIds(
            allSessionCards.Select(c => c.Id));

        var freshById = CardQueryBuilder // TODO: test if this works
            .ForStudy(freshEntities)
            .ToDictionary(e => e.Id);

        // 2. Refresh note data on all CardVMs (including deleted detection)
        foreach (var card in allSessionCards)
        {
            // if found -> rehydrate it with new data
            if (freshById.TryGetValue(card.Id, out var freshEntity))
                card.Refresh(freshEntity);
            
            // Disappeared from DB entirely — treat as deleted
            else card.IsDeleted = true;
        }

        // 3. Recount valid initials
        InitialCount = allSessionCards.Count(c => !c.IsDeleted);

        // 4. Update count vm
        CardsCount.UpdateCount();

        // 5. Notify that current card may have updated note content.
        if (CurrentCard is not null)
            OnPropertyChanged(nameof(CurrentCard));

        // 6. If current card got nuked, move on
        if (CurrentCard?.IsDeleted == true)
            ShowNextCard();
    }

    public async Task OnActionExecuted(CtxMenuAction action)
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
            ReviewedCount++; // TODO FIX: added cards when review UC was open, closed createCardWindow, progress bar broken, shows 100% even tho there are still cards.

            CardsCount.UpdateCount();
            ShowNextCard();
        }
    }

    public override async Task OnFocusGained()
    {
        if (IsCardLoaded && !IsSessionFinished &&
        !stopWatch.IsRunning && CurrentPopup is null)
        {
            stopWatch.Start();
        }

        await base.OnFocusGained();
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        stopWatch.Stop();
    }

    protected override async Task ReloadUserOptAsync()
    {
        userOptions = await uOptionsService
            .GetFromUser(userId);

        OnPropertyChanged(nameof(TimerVisible));
    }
    #endregion

    #region private things
    private readonly long userId;
    private DeckOptions deckOptions;
    private UserOptions userOptions = null!;
    private readonly IUserOptionsService uOptionsService;
    private readonly ICardService cardService;
    private readonly ICardQueryService cardQuery;
    private readonly ICardRepo cardRepo;
    private readonly LearningPool<CardVM> learningPool;
    private List<CardVM> allSessionCards = [];
    private Stack<CardVM> activeCards = null!;
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

        if (userOptions.TimerStopsOnReveal)
            stopWatch.Stop();

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

        // stopWatch.Stop();

        await NavigateTo(new EditCardNavRequest(
            CurrentCard!.Id, userId, this));
    }

    [RelayCommand]
    private async Task ShowEditFromHistory(ICard card)
    {
        // stopWatch.Stop();

        await NavigateTo(new EditCardNavRequest(
            card.Id, userId, this));
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