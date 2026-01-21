using System.Windows.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel
{
    public interface ICloseRequest
    {
        public event Action? OnCloseRequest;
    }
    public interface IOnLoadedHandler
    {
        public void LoadEventHandler();
    }
    
    public partial class MainVM(NavigationService ns): ObservableObject, IViewModel
    {
        [ObservableProperty]
        public partial IViewModel CurrentVM { get; set; }
        private readonly NavigationService navService = ns;
    }
}