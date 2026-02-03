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

            CreateMap<OrderingOptVM, DeckOptions.Ordering>();
            CreateMap<SchedulingOptVM, DeckOptions.Scheduling>();
            CreateMap<DailyLimitsOptVM, DeckOptions.DailyLimits>();

            CreateMap<DeckOptions.Ordering, OrderingOptVM>();
            CreateMap<DeckOptions.Scheduling, SchedulingOptVM>();
            CreateMap<DeckOptions.DailyLimits, DailyLimitsOptVM>();
            #endregion

            #region Deck options (entity <-> record)
            CreateMap<DeckOptionsEntity, DeckOptions>();

            CreateMap<DeckOptions, DeckOptionsEntity>()
                .ForMember(x => x.Decks, o => o.Ignore())
                .ForMember(x => x.User, o => o.Ignore());

            CreateMap<DeckOptions.Ordering, DeckOptionsEntity.Ordering>();
            CreateMap<DeckOptions.Scheduling, DeckOptionsEntity.Scheduling>();
            CreateMap<DeckOptions.DailyLimits, DeckOptionsEntity.DailyLimits>();

            CreateMap<DeckOptionsEntity.Ordering, DeckOptions.Ordering>();
            CreateMap<DeckOptionsEntity.Scheduling, DeckOptions.Scheduling>();
            CreateMap<DeckOptionsEntity.DailyLimits, DeckOptions.DailyLimits>();
            #endregion
        }
    }
