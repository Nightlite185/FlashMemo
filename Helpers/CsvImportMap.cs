using System.Globalization;
using CsvHelper.Configuration;
using FlashMemo.Model.Domain;

namespace FlashMemo.Helpers;

public sealed class CsvImportMap : ClassMap<CsvImportDTO>
{
    public CsvImportMap()
    {
        Map(dto => dto.FrontContent).Name("Front content");
        Map(dto => dto.BackContent).Name("Back content");
        Map(dto => dto.DeckPath).Name("Deck").Optional();
        Map(dto => dto.Tags).Name("Tags").Optional()
            .Convert(args => args.Row.TryGetField<string>("Tags", out var tags) && tags is not null
                ? tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                : []);

        Map(dto => dto.Due).Name("Due date").Optional()
            .TypeConverterOption.CultureInfo(CultureInfo.CurrentCulture)
            .TypeConverterOption.Format("dd/MM/yyyy")
            .TypeConverterOption.NullValues("N/A");

        // CsvHelper's TimeSpan converter reads a whole number as a day count.
        Map(dto => dto.Interval).Name("Interval days").Optional()
            .TypeConverterOption.NullValues("N/A");

        Map(dto => dto.State).Name("State").Optional()
            .TypeConverterOption.NullValues("N/A");

        Map(dto => dto.IsSuspended).Name("Suspended?").Optional()
            .TypeConverterOption.NullValues("N/A");

        Map(dto => dto.IsBuried).Name("Buried?").Optional()
            .TypeConverterOption.NullValues("N/A");

        Map(dto => dto.LearningStage).Name("Learning stage").Optional()
            .TypeConverterOption.NullValues("N/A");

        Map(dto => dto.Created).Name("Created date").Optional()
            .TypeConverterOption.CultureInfo(CultureInfo.CurrentCulture)
            .TypeConverterOption.Format("dd/MM/yyyy")
            .TypeConverterOption.NullValues("N/A");

        Map(dto => dto.LastModified).Name("Last modified").Optional()
            .TypeConverterOption.CultureInfo(CultureInfo.CurrentCulture)
            .TypeConverterOption.Format("dd/MM/yyyy")
            .TypeConverterOption.NullValues("N/A");
            
        Map(dto => dto.LastReviewed).Name("Last reviewed").Optional()
            .TypeConverterOption.CultureInfo(CultureInfo.CurrentCulture)
            .TypeConverterOption.Format("dd/MM/yyyy")
            .TypeConverterOption.NullValues("N/A");
    }
}