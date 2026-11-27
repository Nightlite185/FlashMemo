using System.Globalization;
using System.IO;
using CsvHelper;
using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Bases;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

/// <summary>Provides methods for importing cards. Those methods handle their own exceptions and shouldnt be wrapped in another layer of net from the VM.</summary>
public class ImportService(IDbContextFactory<AppDbContext> dbFactory, IDialogService dialogs, SafetyNet net) : DbDependentClass(dbFactory)
{
    #region public methods
    public async Task<bool> TryImportTxtAsync(string path, string fieldSep, NoteSeparator noteSep, IDeckMeta deck)
    {
        var read = await net.TryUnsafe(
            action: () => File.ReadAllTextAsync(path),
            actionTried: "read the file at given path",
            retry: true);

        if (!read.Success) return false;

        var notes = read.Value
            .Split(noteSep.ToChar())
            .Select(c => c.Split(fieldSep))
            .ToArray();

        var cards = new List<CardEntity>(capacity: notes.Length);

        foreach(var note in notes)
        {
            if (note.Length != 2)
            {
                InvalidContentDialog();
                return false;
            }
            
            cards.Add(CardEntity.CreateNew(
                note: StandardNote.Create(note[0], note[1]),
                deck, tags: []
            ));
        }

        return await TryAddCards(cards);
    }

    public async Task<bool> TryImportCsvAsync(string path, IDeckMeta deck)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CsvImportMap>();
        
        var dtos = csv.GetRecords<CsvImportDTO>().ToArray();

        var cards = await net.TryUnsafe(
            action: () => MapCsvToCards(dtos, deck),
            actionTried: "map csv rows to cards",
            retry: true);

        if (!cards.Success) return false;

        // TODO: create & persist tags that didnt exist before, but were on cards.

        if (!await TryAddCards(cards.Value))
            return false;

        return true;
    }

    public async Task<bool> TryImportFlameAsync(string path, long deckId)
    {
        throw new NotImplementedException();
    }
    #endregion
    
    #region private helpers
    private void InvalidContentDialog()
    {
        dialogs.ShowError(
            "content at given path has invalid format. Make sure it matches the separators given.");
    }

    private async Task<bool> TryAddCards(IEnumerable<CardEntity> cards)
    {
        async Task save()
        {
            await using var db = GetDb;

            await db.Cards.AddRangeAsync(cards);
            await db.SaveChangesAsync();
        }

        return await net.TryUnsafe(
            action: save,
            actionTried: "save the imported cards to database.",
            retry: false);
    }

    private async Task<IReadOnlyList<CardEntity>> MapCsvToCards(CsvImportDTO[] csvDtos, IDeckMeta targetDeck)
    {
        var userId = targetDeck.UserId;
        await using var db = GetDb;

        var allTags = await db.Tags
            .Where(t => t.UserId == userId)
            .AsNoTracking()
            .ToHashSetAsync();
        
        HashSet<string> foundTags = [];

        var cards = new List<CardEntity>(csvDtos.Length);

        foreach (var dto in csvDtos)
        {
            foundTags.AddRange(dto.Tags);

            // TODO: use tags that already existed,
            // and for the rest: create new and add to the db, so they get their ids. 
            // Then when adding cards, relations set correctly.
            // But all this needs to be revertable; if one write fails, return.
            // so need to use a db transaction.

            // maybe even return tags that didnt exist so the caller can write them.
            // this method shouldnt mutate db since its just a helper for mapping

            var note = StandardNote.Create(
                dto.FrontContent, dto.BackContent);

            var card = CardEntity.CreateNew(
                note, targetDeck, tags);

            // TODO: map the properties from dto to card, now it only sets basic properties from deck and note.

            cards.Add(card);
        }

        return cards;
    }
    #endregion
}