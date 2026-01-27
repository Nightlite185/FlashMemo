using System.Runtime.CompilerServices;
using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.BaseVMs;

public abstract partial class OptionsVMBase<TOptions>(IMapper mapper): ObservableObject 
where TOptions: IDefaultable, IEquatable<TOptions>
{
    protected IMapper mapper = mapper;

    ///!<summary>IMPORTANT: THIS MUST BE FILLED IN Initialize()</summary>
    protected TOptions? cachedPersisted;
    protected abstract TOptions DefaultOptions { get; }
    protected bool notifyUI;
    protected bool markChanged;
    protected long? userId;

    public bool IsChanged
    {
        get
        {
            ThrowIfNotInitialized();

            return ToSnapshot()
                .Equals(cachedPersisted);
        }
    }
    public bool IsOGDefault
    {
        get
        {
            ThrowIfNotInitialized();

            return cachedPersisted!
                .Equals(DefaultOptions);
        }
    }
    public bool IsDraftDefault
        => ToSnapshot().Equals(DefaultOptions);

    #region ICommands
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
    #endregion
    protected virtual TOptions ToSnapshot()
        => mapper.Map<TOptions>(this);
    protected void ThrowIfNotInitialized()
    {
        if (cachedPersisted is null || userId is null)
            throw new InvalidOperationException(
            $"{nameof(cachedPersisted)} or {nameof(userId)} is null, did you forget to call Initialize() on this VM?");
    }
    public virtual async Task Initialize(long userId, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
    {
        if (this.userId is not null)
            throw new InvalidOperationException(
            $"{caller} called {nameof(Initialize)} from {file}, but this VM obj was already initialized.");

        this.userId = userId;
    }
}