using AutoMapper;
using FlashMemo.Helpers;
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
        CreateMap<DeckOptionsEntity, DeckOptions>()
            .ForMember(x => x.Decks, opt =>
                opt.ConvertUsing(new DeckListToIds()));

        CreateMap<DeckOptions, DeckOptionsEntity>()
            .ForMember(x => x.Decks, o => o.Ignore())
            .ForMember(x => x.User, o => o.Ignore());

        CreateMap<DeckOptions.SortingOpt, DeckOptionsEntity.SortingOpt>();
        CreateMap<DeckOptions.DailyLimitsOpt, DeckOptionsEntity.DailyLimitsOpt>();
        CreateMap<DeckOptions.SchedulingOpt, DeckOptionsEntity.SchedulingOpt>()
            .ForMember(x => x.LearningStages, opt => 
                opt.ConvertUsing(new ImmutableArrToList<TimeSpan>()));

        CreateMap<DeckOptionsEntity.SortingOpt, DeckOptions.SortingOpt>();
        CreateMap<DeckOptionsEntity.DailyLimitsOpt, DeckOptions.DailyLimitsOpt>();
        CreateMap<DeckOptionsEntity.SchedulingOpt, DeckOptions.SchedulingOpt>()
            .ForMember(x => x.LearningStages, opt => 
                opt.ConvertUsing(new ListToImmutableArr<TimeSpan>()));

        #endregion
    }
}
