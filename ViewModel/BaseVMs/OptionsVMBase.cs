using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.BaseVMs;

public abstract partial class OptionsVMBase<TOptions>: ObservableObject 
where TOptions: IDefaultable, IEquatable<TOptions>
{
    protected TOptions? cachedPersisted;
    protected bool notifyUI;
    protected bool markChanged;

    [ObservableProperty]
    protected partial bool IsChanged { get; set; }

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

    protected abstract TOptions ToSnapshot();
}