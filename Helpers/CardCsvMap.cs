using CsvHelper.Configuration;
using FlashMemo.Model.Domain;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Helpers;

public sealed class CardCsvMap : ClassMap<CsvExportDTO>
{
    public CardCsvMap(IEnumerable<CsvColumnOptionVM> cols)
    {        
        foreach(var col in cols)
        {
            if (col.IsSelected)
                Map(expression: col.PropSelector)
                .Name(col.Name);
        }
    }
}