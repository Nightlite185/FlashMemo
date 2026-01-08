using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services
{
    public class ReviewService(IDbContextFactory<AppDbContext> factory, Mapper mapper)
    {
        private readonly IDbContextFactory<AppDbContext> dbFactory = factory;
        private readonly Mapper mapper = mapper;

        public void ReviewCard(Card card)
        {
            
        }
    }
}