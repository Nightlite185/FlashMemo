using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel
{
    public partial class OptionsVMBase<TOptions>: ObservableObject where TOptions: IDefaultable, IEquatable<TOptions>
    {
        protected TOptions? cachedPersisted;

        [RelayCommand]
        protected virtual async Task SaveChanges()
        {
            
        }

        [RelayCommand]
        protected virtual void ToDefault()
        {
            
        }

        [RelayCommand]
        protected virtual async Task RevertChanges()
        {
            
        }
    
        protected virtual void LoadFrom(TOptions options)
        {
            
        }
    }
}