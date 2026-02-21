using AutoMapper;
using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
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
        CreateMap<DeckOptionsVM, DeckOptions>()
            .ForMember(vm => vm.Decks, opt => 
                opt.ConvertUsing(new ListToImmutableArr<long>()));

        CreateMap<DeckOptions, DeckOptionsVM>()
            .ForMember(d => d.Decks, opt =>
                opt.ConvertUsing(new ImmutableArrToList<long>()));

        CreateMap<DeckOptionsVM.SortingOpt, DeckOptions.SortingOpt>();
        CreateMap<DeckOptionsVM.DailyLimitsOpt, DeckOptions.DailyLimitsOpt>();
        CreateMap<DeckOptionsVM.SchedulingOpt, DeckOptions.SchedulingOpt>()
            .ForMember(vm => vm.LearningStages, opt => 
                opt.ConvertUsing(new ObsColToImmutableArr()));

        CreateMap<DeckOptions.SortingOpt, DeckOptionsVM.SortingOpt>();
        CreateMap<DeckOptions.DailyLimitsOpt, DeckOptionsVM.DailyLimitsOpt>();
        CreateMap<DeckOptions.SchedulingOpt, DeckOptionsVM.SchedulingOpt>()
            .ForMember(d => d.LearningStages, opt => 
                opt.ConvertUsing(new ImmutableArrToObsCol()));
                
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
