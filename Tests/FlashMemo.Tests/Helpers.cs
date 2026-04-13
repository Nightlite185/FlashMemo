using AutoMapper;
using FlashMemo.Model;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Tests;

public static class Helpers
{
    public static IMapper GetMapper()
    {
        return new MapperConfiguration(cfg => 
        cfg.AddProfile<MappingProfile>(), new LoggerFactory())
        .CreateMapper();
    }
}