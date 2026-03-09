using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class UserOptionsMenuVM(long userId, IUserOptionsService optRepo, IMapper imapper): ObservableObject, IViewModel
{
    public UserOptionsVM Options { get; private set; } = null!;
    
    internal async Task InitAsync()
    {
        var entity = await repo.GetFromUser(userId);
        defaultOpt = UserOptions.CreateDefault();

        Options = mapper.Map<UserOptionsVM>(entity);
    }


    #region ICommands
    [RelayCommand] private async Task SaveChanges()
    {
        await repo.Update(
            userId,
            mapper.Map<UserOptions>(Options)
        );

        mapper.Map(Options, lastSaved);
    }
    [RelayCommand] private async Task RevertChanges()
        => mapper.Map(lastSaved, Options);
    [RelayCommand] private async Task ToDefault()
        => mapper.Map(defaultOpt, Options);
    #endregion

    #region private things
    private UserOptions lastSaved = null!;
    private UserOptions defaultOpt = null!;

    private readonly long userId = userId;
    private readonly IUserOptionsService repo = optRepo;
    private readonly IMapper mapper = imapper;
    #endregion
}