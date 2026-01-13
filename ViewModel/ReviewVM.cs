using System.Windows;
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
            ReviewButtonsVis = Visibility.Collapsed;

            ws = windowS;
            cs = cardS;
            cqs = cardQS;

            sw = new();
            timer = new() { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, _) => UpdateTime();

            DefineCommands();
        }
        
        #region public binding properties
        [ObservableProperty]
        public partial CardEntity? CurrentCard { get; set; }

        [ObservableProperty]
        public partial string FrontContent { get; set; } = null!;

        [ObservableProperty]
        public partial string BackContent { get; set; } = null!;
        
        [ObservableProperty]
        public partial Visibility ReviewButtonsVis { get; set; }

        [ObservableProperty]
        public partial Visibility RevealAnswerVis { get; set; }

        [ObservableProperty]
        public partial string ElapsedTime { get; set; } = "00:00";
        #endregion

        #region methods
        public async Task LoadDeckAsync(long deckId)
        {
            cards = await cqs.GetForStudy(deckId);

            enumerator = cards.GetEnumerator();
            enumerator.MoveNext();
            ReviewButtonsVis = Visibility.Visible;
        }
        private void ShowNextCard()
        {
            if (enumerator.MoveNext())
            {
                CurrentCard = enumerator.Current;
            }

            else
            {
                ReviewButtonsVis = Visibility.Collapsed;
                CurrentCard = null;

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
        private async Task ReviewHelper(Answers ans)
        {
            StopTimer();

            if (!CardLoaded)
                throw new InvalidOperationException(
                    @"Cannot review a card without loading any first or after having already reviewed everything. 
                    Review buttons should not be visible now.");

            ReviewButtonsVis = Visibility.Collapsed;

            await cs.ReviewCardAsync(CurrentCard!.Id, ans, sw.Elapsed);

            ShowNextCard();

            AnswerRevealed = false;
            RevealAnswerVis = Visibility.Visible;


            StartTimer();
        }
        private bool CanExecuteReviewCommands()
            => CardLoaded && AnswerRevealed;
        #endregion

        #region private things
        private readonly WindowService ws;
        private readonly CardService cs;
        private readonly CardQueryService cqs;
        private IReadOnlyList<CardEntity> cards = null!;
        private IEnumerator<CardEntity> enumerator = null!;
        private readonly DispatcherTimer timer = null!;
        private readonly Stopwatch sw = null!;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RevealAnswer), nameof(AgainAnswer),
        nameof(HardAnswer), nameof(GoodAnswer), nameof(EasyAnswer))]
        private partial bool AnswerRevealed { get; set; } = false;

        private bool CardLoaded => CurrentCard is not null;
        #endregion

        #region ICommands
        private void DefineCommands()
        {
            RevealAnswer = new(
                execute: () =>
                {
                    AnswerRevealed = true;
                    ReviewButtonsVis = Visibility.Visible;
                },

                canExecute: () => CardLoaded && !AnswerRevealed
            );

            #region Review card buttons
            AgainAnswer = new(
                execute: async () => await ReviewHelper(Answers.Again),
                canExecute: CanExecuteReviewCommands
            );

            HardAnswer = new(
                execute: async () => await ReviewHelper(Answers.Hard),
                canExecute: CanExecuteReviewCommands
            );

            GoodAnswer = new(
                execute: async () => await ReviewHelper(Answers.Good),
                canExecute: CanExecuteReviewCommands
            );

            EasyAnswer = new(
                execute: async () => await ReviewHelper(Answers.Easy),
                canExecute: CanExecuteReviewCommands
            );
            #endregion

            OpenEditWindow = new(
                execute: ws.ShowWindow<EditWindow>,
                canExecute: () => CardLoaded
            );

        }
        // TO DO: turn those into normal methods with RelayCommand attribute instead.
        public RelayCommand RevealAnswer { get; private set; } = null!;

        public RelayCommand AgainAnswer { get; private set; } = null!;
        public RelayCommand HardAnswer { get; private set; } = null!;
        public RelayCommand GoodAnswer { get; private set; } = null!;
        public RelayCommand EasyAnswer { get; private set; } = null!;
        public RelayCommand OpenEditWindow { get; set; } = null!;
        #endregion
    }
}