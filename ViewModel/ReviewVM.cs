using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Model.Domain;
using System.Windows.Threading;
using System.Diagnostics;

namespace FlashMemo.ViewModel
{
    public partial class ReviewVM: ObservableObject, IViewModel
    {
        public ReviewVM(WindowService windowS, CardService cardS, CardQueryService cardQS)
        {
            this.ws = windowS;
            this.cs = cardS;
            this.cqs = cardQS;

            stopWatch = new();
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
        private void NextCard()
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
            stopWatch.Stop();
            timer.Stop();
        }
        private void StartTimer()
        {
            stopWatch.Start();
            timer.Start();
        }
        private void UpdateTime()
        {
            elapsed = elapsed.Add(TimeSpan.FromSeconds(1));
            ElapsedTime = $"{elapsed.Minutes}:{elapsed.Seconds}";
        }
        private async Task ReviewHelper(Answers ans)
        {
            StopTimer();
            elapsed = TimeSpan.Zero;
            
            await cs.ReviewCardAsync(CurrentCard!.Id, ans, elapsed);

            NextCard();
            StartTimer();
        }
        #endregion

        #region private things
        private readonly WindowService ws;
        private readonly CardService cs;
        private readonly CardQueryService cqs;
        private IReadOnlyList<CardEntity> cards = null!;
        private IEnumerator<CardEntity> enumerator = null!;
        private readonly DispatcherTimer timer = null!;
        private readonly Stopwatch stopWatch = null!;
        private TimeSpan elapsed = TimeSpan.Zero;
        #endregion

        #region ICommands
        private void DefineCommands()
        {
            AgainAnswer = new(
                execute: async () => await ReviewHelper(Answers.Again)
                //canExecute:
            );

            HardAnswer = new(
                execute: async () => await ReviewHelper(Answers.Hard)
                //canExecute:
            );

            GoodAnswer = new(
                execute: async () => await ReviewHelper(Answers.Good)
                //canExecute:
            );

            EasyAnswer = new(
                execute: async () => await ReviewHelper(Answers.Easy)
                //canExecute:
            );
        }
        public RelayCommand AgainAnswer { get; private set; } = null!;
        public RelayCommand HardAnswer { get; private set; } = null!;
        public RelayCommand GoodAnswer { get; private set; } = null!;
        public RelayCommand EasyAnswer { get; private set; } = null!;
        #endregion
    }
}