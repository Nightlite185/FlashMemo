using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Model.Domain;
using System.Windows.Threading;
using System.Diagnostics;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Windows;
    
public partial class ReviewVM: BaseVM
{
    public ReviewVM(ICardService cs, ICardQueryService cqs, long userId, long deckId)
    {
        this.userId = userId;
        this.deckId = deckId;

        cardService = cs;
        cardQuery = cqs;
        learningPool = new();

        stopWatch = new();
        timer = new() { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => UpdateTime();
    }
    
    #region public binding properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(CardLoaded))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    public partial CardEntity? CurrentCard { get; set; } // TODO: CHANGE TO CARDITEMVM
    
    public string FrontContent => CurrentCard?.FrontContent 
        ?? throw new InvalidOperationException($"tried to access {nameof(FrontContent)} property, but no card was loaded atm.");
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
    #endregion

    #region methods
    internal async Task InitAsync() //* called in factory
    {
        cards = new (await cardQuery.GetForStudy(deckId));
        
        if (cards.Count == 0) throw new ArgumentException(
            "Deck with this id does not contain any cards for study atm.",
            nameof(deckId));

        CurrentCard = cards.Pop();
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
    private async Task ReviewHelperAsync(Answers ans)
    {
        StopTimer();

        if (!CardLoaded)
            throw new InvalidOperationException("Review called without an active card.");

        AnswerRevealed = false;

        var newState = await cardService
            .ReviewCardAsync(CurrentCard!.Id, ans, stopWatch.Elapsed);

        if (newState == CardState.Learning)
            learningPool.Add(CurrentCard);

        learningPool.InjectDueInto(cards);

        ShowNextCard();
        StartTimer();
    }
    
    #region CanExecute
    private bool CanReview
        => CardLoaded && AnswerRevealed;
    private bool CanRevealAnswer
        => CardLoaded && !AnswerRevealed;

    #endregion
    
    #endregion

    #region private things
    private readonly long userId;
    private readonly long deckId;
    private readonly ICardService cardService;
    private readonly ICardQueryService cardQuery;
    private readonly LearningPool learningPool;
    private Stack<CardEntity> cards = null!;
    private readonly DispatcherTimer timer = null!;
    private readonly Stopwatch stopWatch = null!;
    private bool isSessionFinished = false; // TODO: make this actually matter later, so the state derived properties actually depend on it.

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReview), nameof(CanRevealAnswer))]
    [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
    nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
    private partial bool AnswerRevealed { get; set; } = false;

    private bool CardLoaded => CurrentCard is not null;
    #endregion

    #region ICommands
    
    [RelayCommand(CanExecute = nameof(CanRevealAnswer))]
    private void RevealAnswer()
    {
        // show answer layer here
        AnswerRevealed = true;
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

    [RelayCommand(CanExecute = nameof(CardLoaded))]
    private async Task ShowCardEdit()
    {
        if (!CardLoaded) throw new InvalidOperationException(
        "tried to open card editor, but no card was loaded.");

        await NavigateTo(new EditCardNavRequest(CurrentCard!.Id, userId));

        //TODO: when coming back from the editor to this VM, gotta reload the card and pull any possible changed from db;
    }
    
    #endregion
}
