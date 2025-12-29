using System.Collections.ObjectModel;

namespace FlashMemo.ViewModel
{
    public class MainVM
    {
        private ObservableCollection<DeckVM> Decks { get; } = [];
        public DeckVM? SelectedDeck { get; set; }
        public object? CurrentView { get; set; }
    }
}