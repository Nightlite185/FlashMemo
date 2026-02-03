using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlashMemo.ViewModel.Bases;

public abstract partial class OptionsVMBase<TOptions>(IMapper mapper): ObservableObject 
where TOptions: IEquatable<TOptions>
{
    protected IMapper mapper = mapper; 

    ///!<summary>IMPORTANT: THIS MUST BE FILLED IN FACTORY</summary>
    
    protected abstract TOptions DefaultOptions { get; }
    protected bool notifyUI;
    protected bool markChanged;
    protected readonly long userId;

    public bool IsChanged => ToSnapshot().Equals(lastSaved);
    public bool IsOGDefault => lastSaved.Equals(DefaultOptions);
    public bool IsDraftDefault => ToSnapshot().Equals(DefaultOptions);

    #region ICommands
    [RelayCommand]
    protected virtual async Task SaveChanges()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    protected virtual void ToDefault()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    protected virtual async Task RevertChanges()
    {
        throw new NotImplementedException();
    }

    protected virtual void LoadFrom(TOptions options)
    {
        throw new NotImplementedException();
    }
    #endregion
    protected virtual TOptions ToSnapshot()
        => mapper.Map<TOptions>(this); // TODO FIX
}