using AutoMapper;
using AutoMapper.QueryableExtensions;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class DeckOptVMBuilder(IDbContextFactory<AppDbContext> factory, IMapper m): DbDependentClass(factory), IDeckOptVMBuilder
{
    private readonly IMapper mapper = m;
    public async Task<ICollection<DeckOptionsVM>> BuildAllCounted(long userId)
    {
        var db = GetDb;
        
        var vms = await db.DeckOptions
        .Where(opt => opt.UserId == userId)
            .ProjectTo<DeckOptionsVM>(mapper.ConfigurationProvider)
            .ToArrayAsync();

        var countMap = await db.Decks
            .GroupBy(d => d.OptionsId)
            .ToDictionaryAsync(
                k => k.Key, 
                v => v.Count());

        foreach (var vm in vms)
        {
            vm.DecksAssigned = countMap
                .TryGetValue(vm.Id, out var c) 
                ? c 
                : 0;
        }

        return vms;
    }
}