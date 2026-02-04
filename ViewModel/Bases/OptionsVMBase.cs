using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;

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

    // public bool IsChanged => ToSnapshot().Equals(lastSaved);
    // public bool IsOGDefault => lastSaved.Equals(DefaultOptions);
    // public bool IsDraftDefault => ToSnapshot().Equals(DefaultOptions);

    protected virtual TOptions ToSnapshot()
        => mapper.Map<TOptions>(this);
}