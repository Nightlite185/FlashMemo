using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class UserVMBuilder(IDbContextFactory<AppDbContext> factory, IUserRepo ur, ICountingService cc): DbDependentClass(factory), IUserVMBuilder
{
    private readonly IUserRepo userRepo = ur;
    private readonly ICountingService counter = cc;
    
    public async Task<IEnumerable<UserVM>> BuildAllCounted()
    {
        var users = await userRepo.GetAllAsync();

        List<UserVM> vms = [];

        foreach (var user in users)
            vms.Add(await BuildCounted(user));
        
        return vms;
    }
    public async Task<UserVM> BuildCounted(long userId)
    {
        var db = GetDb;

        var user = await db.Users
            .SingleAsync(u => u.Id == userId);

        return await BuildCounted(user);
    }
    public async Task<UserVM> BuildCounted(UserEntity user)
    {
        int totalCards = await counter
            .AllCards(user.Id);

        int totalDecks = await counter
            .AllDecks(user.Id);

        int forReview = await counter
            .AllReviewableCards(user.Id);

        UserVMStats stats = new()
        {
            CardCount = totalCards,
            DeckCount = totalDecks,
            ReadyForReview = forReview,
            Created = user.Created
        };

        return new(user, stats, userRepo);
    }
    public UserVM BuildUncounted(UserEntity user)
    {
        var stats = new UserVMStats()
            { Created = user.Created };

        return new(user, stats, userRepo);
    }
}