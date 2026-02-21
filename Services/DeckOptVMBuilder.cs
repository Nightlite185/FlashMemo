using AutoMapper;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class DeckOptVMBuilder(IDbContextFactory<AppDbContext> factory, IMapper m, IDeckOptionsRepo dor): DbDependentClass(factory), IDeckOptVMBuilder
{
    private readonly IMapper mapper = m;
    private readonly IDeckOptionsRepo repo = dor;
    public async Task<ICollection<DeckOptionsVM>> BuildAllCounted(long userId)
    {
        var db = GetDb;
        
        var domainOptions = await repo
            .GetAllFromUser(userId);

        var vms = domainOptions
            .Select(mapper.Map<DeckOptionsVM>);

        var countMap = await db.Decks
            .GroupBy(d => d.OptionsId)
            .ToDictionaryAsync(
                k => k.Key, 
                v => v.Count());

        foreach (var vm in vms)
        {
            vm.AssignedDecksCount = countMap
                .TryGetValue(vm.Id, out var c) 
                ? c 
                : 0;
        }

        return [..vms];
    }
}