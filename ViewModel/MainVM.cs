using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel
{
    public interface IViewModel;
    public class MainVM: ObservableObject, IViewModel
    {
        private ObservableCollection<DeckVM> Decks { get; } = [];
        public DeckVM? SelectedDeck { get; set; }
        public object? CurrentView { get; set; }
    }
}