using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;

namespace FlashMemo.Model
{
    public sealed class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<Card, CardEntity>();
            CreateMap<CardEntity, Card>();

            CreateMap<Filters, FiltersVM>();
            CreateMap<FiltersVM, Filters>();
        }
    }
}