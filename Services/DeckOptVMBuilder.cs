using AutoMapper;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class DeckOptVMBuilder(IDbContextFactory<AppDbContext> factory, IMapper m, IDeckOptionsService dor): DbDependentClass(factory), IDeckOptVMBuilder
{
    private readonly IMapper mapper = m;
    private readonly IDeckOptionsService repo = dor;
    public async Task<ICollection<DeckOptionsVM>> BuildAllCounted(long userId)
    {
        await using var db = GetDb;
        
        var presetVMs = (await repo
            .GetAllFromUser(userId))
            .Select(mapper.Map<DeckOptionsVM>)
            .ToArray();

        var countMap = await db.Decks
            .Where(d => d.UserId == userId)
            .GroupBy(d => d.OptionsId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count());

        foreach (var vm in presetVMs)
        {
            vm.DeckCount = countMap
                .TryGetValue(vm.Id, out var c) 
                    ? c : 0;
        }

        return presetVMs;
    }
}
