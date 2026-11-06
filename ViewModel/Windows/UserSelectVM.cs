using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Windows;

public partial class UserSelectVM(IUserRepo userRepo, IUserVMBuilder userVMBuilder, ILoginService loginService, 
                                ILastSessionService lastSession, IUserOptionsService userOptionsService,
                                ILogger<UserSelectVM> logger, IDialogService dialogs, SafetyNet net,
                                long? currentUserId = null): ObservableObject, IViewModel, ICloseRequest
{
    public bool IsNameAvailable(string name)
        => !Users.Any(u => u.Name == name);

    #region public properties
    public ObservableCollection<UserVM> Users { get; init; } = [];

    [ObservableProperty]
    public partial string NewUsernameField { get; set; } = "";
    
    [ObservableProperty]
    public partial UserVM? SelectedUser { get; set; }
    public event Action? OnCloseRequest;
    #endregion

    #region ICommands
    [RelayCommand]
    private async Task RenameUser(UserVM user)
    {
        await user.RenameAsync(
            name => net.TryUnsafe(
                () => userRepo.Rename(user.Id, name),
                actionTried: "rename the user",
                retry: false
            )
        );
    }

    [RelayCommand]
    private async Task RemoveUser(UserVM toRemove)
    {
        if (currentUserId == toRemove.Id)
        {
            dialogs.Show(
                title: "Invalid action",
                message: "Sorry, but you can't delete the user that you're currently logged in with. Please log in to another user, then delete this one.",
                buttons: DialogButtons.OK,
                icon: DialogIcons.Information
            );

            return;
        }

        var preferenceOwnerId = currentUserId ?? toRemove.Id;
        bool shouldConfirm = true;

        try
        {
            shouldConfirm = (await userOptionsService
                .GetFromUser(preferenceOwnerId)).ConfirmUserDeletion;
        }
        catch (Exception e)
        {
            logger.LogError(e,
                "Failed to load deletion preferences for user {UserId}; asking for confirmation anyway",
                preferenceOwnerId);
        }

        if (shouldConfirm)
        {
            var answer = dialogs.Show(
                title: "Delete user?",
                message: $"Are you sure you want to permanently delete '{toRemove.Name}'? "
                    + "This will also delete every deck, card, preset, tag, and review record "
                    + "owned by this user. This action can't be undone."
                    + DialogService.DeleteConfirmationSettingsHint,
                buttons: DialogButtons.YesNo,
                icon: DialogIcons.Warning);

            if (answer is DialogResult.No)
                return;
        }

        if (!await net.TryUnsafe(
            () => userRepo.Remove(toRemove.Id),
            actionTried: "remove the user",
            retry: false))
            return;

        Users.Remove(toRemove);

        if (lastSession.LastUserId == toRemove.Id)
        {
            try { await lastSession.ClearLastUserAsync(); }

            catch (Exception e)
            {
                logger.LogError(e, "failed to clear last logged user of id = {UserId}", lastSession.LastUserId);
            }
        }
    }

    [RelayCommand]
    private async Task CreateUser(string name)
    {
        var user = UserEntity.Create(name);

        var saveRes = await net.TryUnsafe(
            () => userRepo.CreateNew(user),
            actionTried: "create new user.",
            retry: false);

        if (!saveRes.Success) return;

        user = saveRes.Value;
        Users.Add(userVMBuilder.BuildUncounted(user));

        if (NewUsernameField == name)
            NewUsernameField = "";
    }

    [RelayCommand]
    private async Task Login(UserVM toLogin)
    {
        if (currentUserId != toLogin.Id)
        {
            var login = await net.TryUnsafe(
                () => loginService.ChangeUser(toLogin.Id),
                actionTried: "log in to the selected user",
                retry: false);

            if (!login.Success || !login.Value) // TODO: add a overload in net that would return just bool, for action arg typed as Func<Task<bool>>
                return;
        }

        OnCloseRequest?.Invoke();
    }
    #endregion
}