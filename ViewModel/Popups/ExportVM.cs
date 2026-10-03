using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using Microsoft.VisualBasic.FileIO;

namespace FlashMemo.ViewModel.Popups;

public enum ExportFormat { Txt, Csv, FMNote }
public enum NoteSeparator { Tab, Enter, Space }

public partial class ExportVM(
    long userId, ICardQueryService cardQService,
    INoteSerializer ser, IEnumerable<long>? deckIds = null) // if deckIds is null -> we export everything from the user.
    : ObservableObject, IViewModel
{
    [ObservableProperty]
    public partial ExportFormat Format { get; set; } = ExportFormat.Txt;

    [ObservableProperty]
    public partial string ExportPath { get; set; } = SpecialDirectories.MyDocuments;

    [ObservableProperty]
    public partial string FieldSeparator { get; set; } = " - ";

    [ObservableProperty]
    public partial NoteSeparator NoteSep { get; set; } = NoteSeparator.Enter;

    [RelayCommand] // returns whether succeeded or not, and an exception if one was thrown.
    public async Task<(bool, Exception?)> Export ()
    {
        try
        {
            var cards = (deckIds is not null)
                ? await cardQService.GetCardsWhere(
                    new Filters() { UserId = userId, DeckIds = [.. deckIds.Distinct()] },
                    CardsOrder.Created, SortingDirection.Ascending)

                : await cardQService.GetAllFromUser(userId);

            var notes = ser.Serialize(
                cards.Select(c => c.Note));

            await using var writer = File.CreateText(ExportPath);
            char noteSep = ToString(NoteSep);

            foreach (var note in notes)
            {
                if (note is not StandardNote sn)
                    throw new NotSupportedException(
                    "Only standard notes supported at the moment.");

                string entry = sn.FrontContent + FieldSeparator
                    + sn.BackContent + noteSep;

                await writer.WriteAsync(entry);
            }
        }

        catch (Exception e)
        {
            return (false, e);
        }

        return (true, null);
    }

    private static char ToString(NoteSeparator sep)
    {
        return sep switch
        {
            NoteSeparator.Tab => '\t',
            NoteSeparator.Enter => '\n',
            NoteSeparator.Space => ' ',

            _ => throw new InvalidEnumArgumentException("note separator has invalid value")
        };
    }
}