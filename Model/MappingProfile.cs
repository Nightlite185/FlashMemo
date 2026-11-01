using AutoMapper;
using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Model;
public sealed class MappingProfile: Profile
{
    public MappingProfile()
    {
        #region Misc mapping
        CreateMap<Card, CardEntity>();
        CreateMap<CardEntity, Card>();

        CreateMap<Note, Note>().Include<StandardNote, StandardNote>();
        CreateMap<StandardNote, StandardNote>();
        CreateMap<Tag, Tag>()
            .ForMember(tag => tag.User, opt => opt.Ignore())
            .ForMember(tag => tag.Cards, opt => opt.Ignore());
        CreateMap<CardEntity, CardEntity>()
            // The editor supplies the selected deck after copying the saved card.
            .ForMember(card => card.Deck, opt => opt.Ignore());

        CreateMap<UserOptions, UserOptionsVM>();
        CreateMap<UserOptionsVM, UserOptions>();
        #endregion

        #region Deck options (VM <-> record)
        CreateMap<DeckOptionsVM, DeckOptions>();
        CreateMap<DeckOptions, DeckOptionsVM>();

        CreateMap<DeckOptionsVM.SortingOpt, DeckOptions.SortingOpt>();
        CreateMap<DeckOptionsVM.DailyLimitsOpt, DeckOptions.DailyLimitsOpt>();
        CreateMap<DeckOptionsVM.SchedulingOpt, DeckOptions.SchedulingOpt>()
            .ForMember(vm => vm.LearningStages, opt => 
                opt.ConvertUsing(new ToLearningStagesDomain()));

        CreateMap<DeckOptions.SortingOpt, DeckOptionsVM.SortingOpt>();
        CreateMap<DeckOptions.DailyLimitsOpt, DeckOptionsVM.DailyLimitsOpt>();
        CreateMap<DeckOptions.SchedulingOpt, DeckOptionsVM.SchedulingOpt>()
            .ForMember(d => d.LearningStages, opt => 
                opt.ConvertUsing(new ToLearningStagesVM()));
                
        #endregion

        #region Deck options (entity <-> record)
        CreateMap<DeckOptionsEntity, DeckOptions>();

        CreateMap<DeckOptions, DeckOptionsEntity>()
            .ForMember(x => x.User, o => o.Ignore());

        CreateMap<DeckOptions.SortingOpt, DeckOptionsEntity.SortingOpt>();
        CreateMap<DeckOptions.DailyLimitsOpt, DeckOptionsEntity.DailyLimitsOpt>();
        CreateMap<DeckOptions.SchedulingOpt, DeckOptionsEntity.SchedulingOpt>();

        CreateMap<DeckOptionsEntity.SortingOpt, DeckOptions.SortingOpt>();
        CreateMap<DeckOptionsEntity.DailyLimitsOpt, DeckOptions.DailyLimitsOpt>();
        CreateMap<DeckOptionsEntity.SchedulingOpt, DeckOptions.SchedulingOpt>();

        #endregion
    }
}
