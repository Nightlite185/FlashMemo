using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel
{
    public partial class BrowseVM: ObservableObject, IViewModel
    {
        [ObservableProperty]
        public partial ObservableCollection<CardItemVM> Cards { get; set; }

        
    }
}