using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Model.Domain;
using System.Windows.Threading;
using System.Diagnostics;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Windows;
    
public partial class ReviewVM: NavBaseVM, IPopupHost, IReloadHandler
{
    public ReviewVM(ICardService cs, ICardQueryService cqs, long userId, long deckId, DeckOptions deckOpt)
    {
        this.userId = userId;
        this.deckId = deckId;
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
    [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(IsCardLoaded))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    public partial CardEntity? CurrentCard { get; set; } // TODO: change type later to ICard
    
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
    
    [ObservableProperty]
    public partial string ElapsedTime { get; set; } = "00:00";

    [ObservableProperty] // TODO: should work but must humanize this (e.g. turn TimeSpan readable string in a good format)
    public partial SchedulePermutations? SchedulePerms { get; set; }

    public PopupVMBase? CurrentPopup { get; set; }
    #endregion

    #region methods
    internal async Task InitAsync(CardCtxMenuVM ctxMenu) //* called in factory
    {
        cardCtxMenu = ctxMenu;

        cards = new (await cardQuery.GetForStudy(deckId));

        ShowNextCard();
    }

    private ScheduleInfo GetScheduleInfo(Answers answer)
    {
        if (SchedulePerms is null) 
            throw new NullReferenceException();

        return answer switch
        {
            Answers.Easy => SchedulePerms.Value.Easy,
            Answers.Good => SchedulePerms.Value.Good,
            Answers.Hard => SchedulePerms.Value.Hard,
            Answers.Again => SchedulePerms.Value.Again,

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

        else
        {
            CurrentCard = null;
            isSessionFinished = true;
            // Show congrats screen here or sth, like お疲れ様です with some fireworks or confetti lol
        }
    }
    private void StopTimer()
    {
        stopWatch.Stop();
        timer.Stop();
    }
    private void StartTimer()
    {
        stopWatch.Start();
        timer.Start();
    }
    private void UpdateTime()
        => ElapsedTime = $"{(int)stopWatch.Elapsed.TotalMinutes: 00}:{stopWatch.Elapsed.Seconds: 00}";
    private async Task ReviewHelperAsync(Answers answer)
    {
        StopTimer();

        if (!IsCardLoaded)
            throw new InvalidOperationException(
            "Review called without an active card.");

        AnswerRevealed = false;

        var scheduleInfo = GetScheduleInfo(answer);

        await cardService.ReviewCardAsync(
            CurrentCard!.Id, 
            scheduleInfo, answer, 
            stopWatch.Elapsed);

        if (scheduleInfo.State == CardState.Learning)
            learningPool.Add(CurrentCard);

        learningPool.InjectDueInto(cards);
        SchedulePerms = null;

        ShowNextCard();
        StartTimer();
    }
    
    #region CanExecute
    private bool CanReview
        => IsCardLoaded && AnswerRevealed;
    private bool CanRevealAnswer
        => IsCardLoaded && !AnswerRevealed;

    #endregion
    
    #endregion

    #region private things
    private readonly long userId;
    private readonly long deckId;
    private readonly DeckOptions deckOptions;
    private readonly ICardService cardService;
    private CardCtxMenuVM cardCtxMenu = null!;
    private readonly ICardQueryService cardQuery;
    private readonly LearningPool learningPool;
    private Stack<CardEntity> cards = null!;
    private readonly DispatcherTimer timer = null!;
    private readonly Stopwatch stopWatch = null!;
    private bool isSessionFinished; // TODO: make this actually matter later, so the state derived properties actually depend on it.

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReview), nameof(CanRevealAnswer))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    private partial bool AnswerRevealed { get; set; } = false;

    private bool IsCardLoaded => CurrentCard is not null;
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
        => await ReviewHelperAsync(Answers.Again);

    
    [RelayCommand(CanExecute = nameof(CanReview))]
    private async Task HardAnswer() 
        => await ReviewHelperAsync(Answers.Hard);

    
    [RelayCommand(CanExecute = nameof(CanReview))]
    private async Task GoodAnswer()
        => await ReviewHelperAsync(Answers.Good);


    [RelayCommand(CanExecute = nameof(CanReview))]
    private async Task EasyAnswer() 
        => await ReviewHelperAsync(Answers.Easy);
    #endregion

    [RelayCommand(CanExecute = nameof(IsCardLoaded))]
    private async Task ShowCardEdit()
    {
        if (!IsCardLoaded) throw new InvalidOperationException(
        "tried to open card editor, but no card was loaded.");

        await NavigateTo(new EditCardNavRequest(CurrentCard!.Id, userId));

        //TODO: when coming back from the editor to this VM, gotta reload the card and pull any possible changed from db;
    }
    #endregion
}
