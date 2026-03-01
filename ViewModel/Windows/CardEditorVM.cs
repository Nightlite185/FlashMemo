using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class CardEditorVM(ICardService cs, ITagRepo tr, ICardRepo cr)
: ICloseRequest, IPopupHost, IViewModel
{
    public EditableCardVM Card { get; protected set; } = null!; //* factory sets this
    public PopupVMBase? CurrentPopup { get; set; }

    #region methods
    internal async Task Initialize(long cardId, CardCtxMenuVM ccmVM) //* Factory calls this
    {
        ctxMenu = ccmVM;
        lastSavedCard = await cardRepo.GetById(cardId);

        var tags = await tagRepo.GetFromCard(cardId);
        lastSavedCard.Tags.AddRange(tags); // snapshotting old tags
        
        Card = new (lastSavedCard);
    }
    
    protected async Task SaveChanges()
    {
        await cardService.SaveEditedCard(
            Card.ToEntity(), 
            CardAction.Modify
        );

        OnCloseRequest?.Invoke();
    }

    #endregion

    #region private things
    private CardEntity lastSavedCard = null!;
    private CardCtxMenuVM ctxMenu = null!;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardService cardService = cs;
    private readonly ICardRepo cardRepo = cr;
    #endregion

    #region ICommands
    [RelayCommand]
    private async Task RevertChanges() // only reverts the vm-made changes to what the card was.
    {
        Card.RevertChanges();

        // refreshing the tags cuz user might have edited them globally
        // after loading this VM, but before this command was called.
        
        // var oldTagIds = lastSavedCard.Tags;
        // var oldTags = await tagRepo.GetByIds(
        //     oldTagIds.Select(t => t.Id));

        // Card.Tags.AddRange(oldTags.ToVMs());
    }

    [RelayCommand]
    protected virtual void CancelChanges() => OnCloseRequest?.Invoke();

    [RelayCommand]
    private void ShowCtxMenu()
    {
        ctxMenu.OpenMenu([Card]);
    }
    #endregion

    public event Action? OnCloseRequest;
}