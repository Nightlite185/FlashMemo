using System.Reflection;
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

    public static TField GetPrivateField<TField>(this object from, string fieldName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        
        FieldInfo field = from.GetType().GetField(fieldName, flags) 
            ?? throw new InvalidOperationException(
            "Couldnt find field with given name.");
            
        return (TField?)field.GetValue(from) 
            ?? throw new NullReferenceException("Couldnt get the field from object.");
    }
}