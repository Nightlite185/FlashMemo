using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model
{
    public sealed class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<Card, CardEntity>();
            CreateMap<User, UserEntity>();
        }
    }
}
