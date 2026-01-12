using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel
{
    public partial class CardItemVM: ObservableObject, IViewModel
    {
        [ObservableProperty]
        public partial bool IsSelected { get; set; }
        
        [ObservableProperty]
        public partial CardEntity Card { get; set; }
    }
}