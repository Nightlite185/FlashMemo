using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Services;

public interface IExportService
{
    string PrepareTxt(IEnumerable<CardEntity> cards, string fieldSeparator, char noteSeparator);
    string PrepareCsv(IEnumerable<CardEntity> cards, IReadOnlyDictionary<long, string> deckPaths,
        IEnumerable<CsvExportColumn> columns);
    Task SaveAsync(string content, string path);
}

public class ExportService(INoteSerializer serializer) : IExportService
{
    public string PrepareTxt(IEnumerable<CardEntity> cards, string fieldSeparator, char noteSeparator)
    {
        var content = new StringBuilder();

        foreach (var (_, note) in serializer.Serialize(cards))
        {
            if (note is not StandardNote standard)
                throw new NotSupportedException("Only standard notes supported at the moment.");

            content.Append(standard.FrontContent)
                   .Append(fieldSeparator)
                   .Append(standard.BackContent)
                   .Append(noteSeparator);
        }

        return content.ToString();
    }

    public string PrepareCsv(IEnumerable<CardEntity> cards, IReadOnlyDictionary<long, string> deckPaths,
        IEnumerable<CsvExportColumn> columns)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.Context.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("N/A");
        csv.Context.RegisterClassMap(new CardCsvMap(columns));

        var records = serializer.Serialize(cards)
            .Select(pair => new CsvExportDTO(
                pair.card, pair.note,
                deckPaths.GetValueOrDefault(pair.card.DeckId, pair.card.Deck.Name)));
            
        csv.WriteRecords(records);
        csv.Flush();
        return writer.ToString();
    }

    // One attempt only. The caller decides whether a failed save should be retried.
    public async Task SaveAsync(string content, string path)
    {
        string destination = Path.GetFullPath(path);
        
        string tempPath = Path.Combine(
            Path.GetDirectoryName(destination)!,
            $"FlashMemo-{Guid.NewGuid():N}.tmp");
        
        try
        {
            await File.WriteAllTextAsync(tempPath, content);
            File.Move(tempPath, destination, overwrite: true);
        }
        
        finally
        {
            // Cleanup must not hide the original write/move failure.
            try { File.Delete(tempPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
