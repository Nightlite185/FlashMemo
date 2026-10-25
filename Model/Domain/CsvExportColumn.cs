using System.Linq.Expressions;

namespace FlashMemo.Model.Domain;

public sealed record CsvExportColumn(
    string Name, Expression<Func<CsvExportDTO, string?>> PropSelector);
