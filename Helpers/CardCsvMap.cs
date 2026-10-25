using CsvHelper.Configuration;
using FlashMemo.Model.Domain;

namespace FlashMemo.Helpers;

public sealed class CardCsvMap : ClassMap<CsvExportDTO>
{
    public CardCsvMap(IEnumerable<CsvExportColumn> cols)
    {
        foreach (var col in cols)
        {
            Map(expression: col.PropSelector)
                .Name(col.Name);
        }
    }
}
