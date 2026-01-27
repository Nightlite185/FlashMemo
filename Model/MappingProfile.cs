using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.WindowVMs;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.Model;
    public sealed class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<Card, CardEntity>();
            CreateMap<CardEntity, Card>();

            CreateMap<Filters, FiltersVM>();
            CreateMap<FiltersVM, Filters>();

            CreateMap<DeckOptionsVM, DeckOptions>();
            CreateMap<DeckOptions, DeckOptionsVM>();

            CreateMap<OrderingOptVM, DeckOptions.OrderingOpt>();
            CreateMap<SchedulingOptVM, DeckOptions.SchedulingOpt>();
            CreateMap<DailyLimitsOptVM, DeckOptions.DailyLimitsOpt>();

            CreateMap<DeckOptions.OrderingOpt, OrderingOptVM>();
            CreateMap<DeckOptions.SchedulingOpt, SchedulingOptVM>();
            CreateMap<DeckOptions.DailyLimitsOpt, DailyLimitsOptVM>();

            CreateMap<UserOptions, UserOptionsVM>();
            CreateMap<UserOptionsVM, UserOptions>();
        }
    }
