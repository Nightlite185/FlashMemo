using System.IO;
using FlashMemo.Helpers;
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

        return await net.TryUnsafe(
            action: () => TryAddCards(cards),
            actionTried: "save the imported cards.",
            retry: false
        );
    }

    public async Task<bool> TryImportCsvAsync(string path, long deckId)
    {
        throw new NotImplementedException();
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

    private async Task TryAddCards(IEnumerable<CardEntity> cards)
    {
        await using var db = GetDb;

        await db.Cards.AddRangeAsync(cards);
        await db.SaveChangesAsync();
    }
    #endregion
}