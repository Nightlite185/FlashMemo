using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class UserOptionsMenuVM(long userId, IUserOptionsService optService, IMapper imapper, IVMEventBus bus)
                                        : ObservableObject, IViewModel, ICloseRequest, IClosingAware
{
    public UserOptionsVM Options { get; private set; } = null!;
    private bool CanSaveChanges => Options?.HasErrors == false;
    public event Action? OnCloseRequest;
    
    #region methods
    internal async Task InitAsync()
    {
        var entity = await repo.GetFromUser(userId);
        defaultOpt = UserOptions.CreateDefault();

        Options = mapper.Map<UserOptionsVM>(entity);
        lastSaved = mapper.Map<UserOptions>(entity);

        Options.ErrorsChanged += (_, _) => 
            SaveChangesCommand.NotifyCanExecuteChanged();

        SaveChangesCommand.NotifyCanExecuteChanged();
    }

    public bool CanClose()
    {
        var snapshot = mapper.Map<UserOptions>(Options);

        if (snapshot.Equals(lastSaved))
            return true;

        var result = DialogService.Show(
            title: "Unsaved changes",
            message: "You have unsaved changes. Do you want to discard them?",
            buttons: DialogButtons.YesNo,
            icon: DialogIcons.Warning
        );

        return result == DialogResult.Yes;
    }
    #endregion

    #region ICommands
    [RelayCommand(CanExecute = nameof(CanSaveChanges))]
    private async Task SaveChanges()
    {
        await repo.Update(
            userId,
            mapper.Map<UserOptions>(Options)
        );

        lastSaved = mapper.Map<UserOptions>(Options);

        eventBus.NotifyUserOpt();
        OnCloseRequest?.Invoke();
    }
    [RelayCommand] private void RevertChanges()
        => mapper.Map(lastSaved, Options);
    [RelayCommand] private void ToDefault()
        => mapper.Map(defaultOpt, Options);
    #endregion

    #region private things
    private UserOptions lastSaved = null!;
    private UserOptions defaultOpt = null!;

    private readonly long userId = userId;
    private readonly IUserOptionsService repo = optService;
    private readonly IMapper mapper = imapper;
    private readonly IVMEventBus eventBus = bus;
    #endregion
}
