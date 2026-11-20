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
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Windows;

public partial class ReviewVM(ICardService cardService, ICardQueryService cardQuery, long userId,
                            IDeckMeta deck, ICardRepo cardRepo, IVMEventBus eventBus, 
                            IUserOptionsService uOptionsService, IDeckOptionsService deckOptService,
                            ILogger<ReviewVM> logger, IDialogService dialogs, SafetyNet net)
                            : BaseVM(eventBus, logger, dialogs, net), IPopupHost, IFocusState, ICtxMenuHost, ICardsSource<CardVM>
{
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
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    public partial bool IsSavingReview { get; private set; }
    [ObservableProperty] public partial PopupVMBase? CurrentPopup { get; set; }

    public CardsCountVM CardsCount { get; private set; } = null!;
    public CardCtxMenuVM CtxMenuVM { get; private set; } = null!;
    public ObservableCollection<CardEntity> ReviewHistory { get; } = [];
    public event Func<Task>? OnDecksNavRequest;
    public IEnumerable<CardVM> Cards => allSessionCards;
    public bool TimerVisible => userOptions.ShowReviewTimer;
    public IDeckMeta Deck { get; init; } = deck;
    #endregion

    #region methods
    internal async Task InitAsync(CardCtxMenuVM ctxMenu) //* called in factory
    {
        this.CtxMenuVM = ctxMenu;
        eventBus.DomainChanged += OnDomainChanged;
        eventBus.DeckOptionsChanged += OnDeckOptChanged;
        eventBus.UserOptionsChanged += OnUserOptChanged;

        userOptions = await uOptionsService
            .GetFromUser(userId);

        deckOptions = await deckOptService
            .GetFromDeck(Deck.Id);

        (var freshCards, var count) = await cardQuery
            .GetForStudy(Deck.Id, userId);

        this.activeCards = new (freshCards
            .Select(c => new CardVM(c))
            .Reverse()); // reversing bc stack orders it differently.

        this.CardsCount = new(count, this);

        allSessionCards = [..activeCards];
        InitialCount = activeCards.Count;

        ShowNextCard();
    }
    private ScheduleInfo GetScheduleInfo(Answers answer)
    {
        if (SchedulePerms is null) //TODO: replace throw with logging, trying to load permutations again in a net, if failed: return, else continue work here.
            throw new NullReferenceException();

        return answer switch
        {
            Answers.Easy => SchedulePerms.Easy,
            Answers.Good => SchedulePerms.Good,
            Answers.Hard => SchedulePerms.Hard,
            Answers.Again => SchedulePerms.Again,

            _ => throw new ArgumentOutOfRangeException(
                nameof(answer), // TODO: log err here too, I think even critical..
                $"Invalid Answers enum value: {answer}")
        };
    }
    private void ShowNextCard()
    {
        stopWatch.Reset();
        UpdateTime();
        learningPool.InjectDueInto(activeCards);
        
        CurrentCard = activeCards.TryPop(out var fromStack)
            ? fromStack
            : learningPool.TryPopEarly();

        if (CurrentCard is null)
            IsSessionFinished = true;

        else if (CurrentCard.IsInvalid)
            ShowNextCard();

        else ResumeTimer();
    }
    private void UpdateOnReview(CardEntity reviewed, CardVM curr)
    {
        curr.Refresh(reviewed);

        switch (curr.State)
        {
            case CardState.Learning:
                learningPool.Add(curr);
                break;

            case CardState.Review:
                ReviewedCount++;
                curr.IsInvalid = true; // flipping IsInvalid bc now its out of play, and the CardCountVM should skip it.
                break;

            default: throw new InvalidOperationException( // TODO: log critical probably
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
    private void ResumeTimer()
    {
        if (isFocused && IsCardLoaded && !IsSessionFinished && !IsSavingReview
            && CurrentPopup is null && !(AnswerRevealed && userOptions.TimerStopsOnReveal))
            stopWatch.Start();
    }

    private async Task ReviewAsync(Answers answer)
    {
        if (!CanReview) return;

        if (!IsCardLoaded)
            throw new InvalidOperationException(
            "Review called without an active card."); // TODO: log error and show next card quietly

        var updatedSchedule = GetScheduleInfo(answer);

        bool wasRunning = stopWatch.IsRunning;
        stopWatch.Stop();
        var answerTime = stopWatch.Elapsed;
        IsSavingReview = true;
        UnsafeActionResult<CardEntity> result;

        try
        {
            result = await net.TryUnsafe(
                () => cardService.ReviewCardAsync(
                    CurrentCard!.Id, updatedSchedule,
                    answer, answerTime),
                actionTried: "save the card review",
                retry: false);
        }

        finally { IsSavingReview = false; }

        if (!result.Success)
        {
            if (wasRunning)
                ResumeTimer();

            return;
        }

        AnswerRevealed = false;
        SchedulePerms = null;

        UpdateOnReview(result.Value, CurrentCard!);
        ShowNextCard();
    }

    partial void OnCurrentPopupChanged(PopupVMBase? value)
    {
        if (CurrentPopup is null)
            ResumeTimer();

        else stopWatch.Stop();
    }
    
    protected override async Task<bool> ReloadDomainAsync()
    {
        // 1. Fetch fresh entities for all session card IDs in one query
        var freshEntities = await cardRepo.GetByIds(
            allSessionCards.Select(c => c.Id));

        var freshById = freshEntities
            .ToDictionary(e => e.Id);

        // 2. Refresh note data on all CardVMs (including deleted detection)
        foreach (var card in allSessionCards)
        {
            // if found -> rehydrate it with new data
            if (freshById.TryGetValue(card.Id, out var freshEntity))
                card.Refresh(freshEntity);

            // Disappeared from DB entirely — treat as deleted
            else
            {
                card.IsInvalid = true;
                InitialCount--;
            }
        }

        // 4. Update count vm
        CardsCount.UpdateCount();

        // 5. Notify that current card may have updated note content.
        if (CurrentCard is not null)
            OnPropertyChanged(nameof(CurrentCard));

        // 6. If current card got nuked, move on
        if (CurrentCard?.IsInvalid == true)
            ShowNextCard();

        return true;
    }

    public Task OnActionExecuted(CtxMenuAction action, IReadOnlyCollection<CardEntity> affectedCards)
    {
        if (action is CtxMenuAction.Relocate
        or CtxMenuAction.Reschedule
        or CtxMenuAction.Bury
        or CtxMenuAction.Suspend
        or CtxMenuAction.Delete
        or CtxMenuAction.Forget)
        {
            var affectedById = affectedCards.ToDictionary(card => card.Id);
            foreach (var card in allSessionCards)
            {
                if (!affectedById.TryGetValue(card.Id, out var saved))
                    continue;

                if (action != CtxMenuAction.Delete)
                    card.Refresh(saved);

                if (!card.IsInvalid)
                {
                    card.IsInvalid = true;
                    ReviewedCount++;
                }
            }

            CardsCount.UpdateCount();
            if (CurrentCard?.IsInvalid == true)
                ShowNextCard();
        }

        return Task.CompletedTask;
    }

    public override async Task OnFocusGained()
    {
        await base.OnFocusGained();
        ResumeTimer();
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        stopWatch.Stop();
    }

    protected override async Task<bool> ReloadUserOptAsync()
    {
        userOptions = await uOptionsService
            .GetFromUser(userId);

        OnPropertyChanged(nameof(TimerVisible));
        return true;
    }

    protected override async Task<bool> ReloadDeckOptAsync()
    {
        deckOptions = await deckOptService
            .GetFromDeck(Deck.Id);
            
        return true;
    }
    #endregion

    #region private things
    private DeckOptions deckOptions = null!;
    private UserOptions userOptions = null!;
    private readonly LearningPool<CardVM> learningPool = new();
    private readonly Stopwatch stopWatch = new();
    private List<CardVM> allSessionCards = [];
    internal Stack<CardVM> activeCards = null!;
    private const int HistoryCap = 10;
    
    private bool IsCardLoaded => CurrentCard is not null;
    private bool CanReview
        => IsCardLoaded && AnswerRevealed && !IsSavingReview;
    private bool CanRevealAnswer
        => IsCardLoaded && !AnswerRevealed && !IsSavingReview;
    #endregion

    #region ICommands
    
    [RelayCommand(CanExecute = nameof(CanRevealAnswer))]
    private void RevealAnswer()
    {
        if (userOptions.TimerStopsOnReveal)
            stopWatch.Stop();

        if (CurrentCard is null)
        {
            logger.LogError("Tried to reveal answer, but current card is not set to anything. Taking the next card from queue.");

            ShowNextCard();
            return;
        }

        SchedulePerms = Scheduler.GetForecast(
            CurrentCard,
            deckOptions.Scheduling,
            userOptions);

        AnswerRevealed = true;
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
        "tried to open card editor, but no card was loaded."); // TODO: log error, show dialog about card not loaded, show next card.

        await net.TryUnsafe(
            () => NavigateTo(new EditCardNavRequest(CurrentCard!.Id, userId)),
            actionTried: "open the card editor",
            retry: false);
    }

    [RelayCommand]
    private async Task ShowEditFromHistory(ICard card)
    {
        await net.TryUnsafe(
            () => NavigateTo(new EditCardNavRequest(card.Id, userId)),
            actionTried: "open the card editor",
            retry: false);
    }

    [RelayCommand]
    private void ShowCtxMenu()
    {
        if (CurrentCard is null)
            throw new InvalidOperationException( // TODO: here too
            "Tried opening ctx menu with no currently loaded card.");

        CtxMenuVM.OpenMenu([CurrentCard.SavedEntity]);
    }

    [RelayCommand]
    private async Task ShowDecks()
    {
        if (OnDecksNavRequest is { } handler)
            await net.TryUnsafe(
                handler,
                actionTried: "open the decks view",
                retry: false,
                operation: nameof(OnDecksNavRequest));
    }
    #endregion
}