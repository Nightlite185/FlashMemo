using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel;

public interface IAsyncVMFactory<TVM> where TVM: IViewModel
{
    Task<TVM> CreateAsync();
}

public interface IVMFactory<TVM> where TVM: IViewModel
{
    TVM Create();
}

public interface ICloseRequest
{
    public event Action? OnCloseRequest;
}


public interface INavRequestSender
{
    public event Func<NavigationRequest, Task>? NavRequested;
    void RegisterNavBubbling(INavRequestSender vm);
}

public interface IFiltrable
{
    public Task ApplyFiltersAsync(Filters filters);
}

public interface IPopupHost
{
    public PopupVMBase? CurrentPopup { get; set; }
}

public interface IDisplayHost
{
    object CurrentDisplay { get; set; }
}

public interface IClosedHandler
{
    void OnClosed();
}

public interface IViewModel;

public interface ICardVM
{
    long Id { get; }
    CardEntity ToEntity();
}

public interface IFocusState
{
    Task OnFocusGained();
    void OnFocusLost();
}

public interface ICtxMenuHost
{
    Task OnActionExecuted(CtxMenuAction action);
}

public interface ICardsSource<TCard> where TCard: class
{
    IEnumerable<TCard> Cards { get; }
}

public interface IClosingAware
{
    Task<bool> CanCloseAsync();
}

public interface ITagManagerVM
{
    IEnumerable<TagVM> CardTags { get; }
    Task<IEnumerable<TagVM>> GetAllExistingTagsAsync();
    Task<TagVM?> AddTagAsync(string tagName);
    bool RemoveTag(long tagId);
}

public interface ITagManagerHost
{
    ITagManagerVM? TagManager { get; }
}
