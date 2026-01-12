using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel
{
    public interface IViewModel;
    public partial class MainVM: ObservableObject, IViewModel
    {
        private ObservableCollection<DeckItemVM> Decks { get; } = [];
        public DeckItemVM? SelectedDeck { get; set; }
        public object? CurrentView { get; set; }
    }
}