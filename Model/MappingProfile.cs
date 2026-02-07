using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Windows;
using static FlashMemo.ViewModel.Wrappers.DeckOptionsVM;

namespace FlashMemo.Model;
public sealed class MappingProfile: Profile
{
    public MappingProfile()
    {
        #region Misc mapping
        CreateMap<Card, CardEntity>();
        CreateMap<CardEntity, Card>();

        CreateMap<Filters, FiltersVM>();
        CreateMap<FiltersVM, Filters>();

        CreateMap<UserOptions, UserOptionsVM>();
        CreateMap<UserOptionsVM, UserOptions>();
        #endregion

        #region Deck options (VM <-> record)
        CreateMap<DeckOptionsMenuVM, DeckOptions>();
        CreateMap<DeckOptions, DeckOptionsMenuVM>();

        CreateMap<OrderingOptVM, DeckOptions.SortingOpt>();
        CreateMap<SchedulingOptVM, DeckOptions.SchedulingOpt>();
        CreateMap<DailyLimitsOptVM, DeckOptions.DailyLimitsOpt>();

        CreateMap<DeckOptions.SortingOpt, OrderingOptVM>();
        CreateMap<DeckOptions.SchedulingOpt, SchedulingOptVM>();
        CreateMap<DeckOptions.DailyLimitsOpt, DailyLimitsOptVM>();
        #endregion

        #region Deck options (entity <-> record)
        CreateMap<DeckOptionsEntity, DeckOptions>();

        CreateMap<DeckOptions, DeckOptionsEntity>()
            .ForMember(x => x.Decks, o => o.Ignore())
            .ForMember(x => x.User, o => o.Ignore());

        CreateMap<DeckOptions.SortingOpt, DeckOptionsEntity.SortingOpt>();
        CreateMap<DeckOptions.SchedulingOpt, DeckOptionsEntity.SchedulingOpt>();
        CreateMap<DeckOptions.DailyLimitsOpt, DeckOptionsEntity.DailyLimitsOpt>();

        CreateMap<DeckOptionsEntity.SortingOpt, DeckOptions.SortingOpt>();
        CreateMap<DeckOptionsEntity.SchedulingOpt, DeckOptions.SchedulingOpt>();
        CreateMap<DeckOptionsEntity.DailyLimitsOpt, DeckOptions.DailyLimitsOpt>();
        #endregion
    }
}
