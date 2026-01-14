using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Model.Domain;
using System.Windows.Threading;
using System.Diagnostics;
using FlashMemo.View;

namespace FlashMemo.ViewModel
{
    public partial class ReviewVM: ObservableObject, IViewModel
    {
        public ReviewVM(WindowService windowS, CardService cardS, CardQueryService cardQS)
        {
            ws = windowS;
            cs = cardS;
            cqs = cardQS;

            sw = new();
            timer = new() { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, _) => UpdateTime();
        }
        
        #region public binding properties
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FrontContent), nameof(BackContent), nameof(CardLoaded))]
        [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
        nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
        public partial CardEntity? CurrentCard { get; set; }
        
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
        public async Task LoadDeckAsync(long deckId)
        {
            cards = await cqs.GetForStudy(deckId);

            cardIdx = 0;
            CurrentCard = cards[cardIdx];
        }
        private void ShowNextCard()
        {
            CurrentCard = cards.ElementAtOrDefault(++cardIdx);

            if (CurrentCard is null)
            {
                // Show congrats screen here or sth, like お疲れ様です with some fireworks or confetti lol
            }
        }
        private void StopTimer()
        {
            sw.Stop();
            timer.Stop();
        }
        private void StartTimer()
        {
            sw.Start();
            timer.Start();
        }
        private void UpdateTime()
            => ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes: 00}:{sw.Elapsed.Seconds: 00}";
        private async Task ReviewHelperAsync(Answers ans)
        {
            StopTimer();

            if (!CardLoaded)
                throw new InvalidOperationException("Review called without an active card.");


            await cs.ReviewCardAsync(CurrentCard!.Id, ans, sw.Elapsed);

            ShowNextCard();

            AnswerRevealed = false;
            
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
        private readonly WindowService ws;
        private readonly CardService cs;
        private readonly CardQueryService cqs;
        private IReadOnlyList<CardEntity> cards = null!;
        private int cardIdx;
        private readonly DispatcherTimer timer = null!;
        private readonly Stopwatch sw = null!;
        private bool isSessionFinished = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanReview), nameof(CanRevealAnswer))]
        [NotifyCanExecuteChangedFor(nameof(RevealAnswerCommand), nameof(AgainAnswerCommand),
        nameof(HardAnswerCommand), nameof(GoodAnswerCommand), nameof(EasyAnswerCommand))]
        private partial bool AnswerRevealed { get; set; } = false;

        private bool CardLoaded => CurrentCard is not null;
        #endregion

        #region ICommands
        
        [RelayCommand(CanExecute = nameof(CanRevealAnswer))]
        public void RevealAnswer()
        {
            // show answer layer here
            AnswerRevealed = true;
        }
        
        #region answer commands
        [RelayCommand(CanExecute = nameof(CanReview))]
        public async Task AgainAnswer()
            => await ReviewHelperAsync(Answers.Again);

        
        [RelayCommand(CanExecute = nameof(CanReview))]
        public async Task HardAnswer() 
            => await ReviewHelperAsync(Answers.Hard);

        
        [RelayCommand(CanExecute = nameof(CanReview))]
        public async Task GoodAnswer() 
            => await ReviewHelperAsync(Answers.Good);


        [RelayCommand(CanExecute = nameof(CanReview))]
        public async Task EasyAnswer() 
            => await ReviewHelperAsync(Answers.Easy);
        #endregion

        [RelayCommand(CanExecute = nameof(CardLoaded))]
        public void OpenEditWindow() => ws.ShowWindow<EditWindow>();
        
        #endregion
    }
}