using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;

namespace FlashMemo.Tests.VMTests;

public class RenameVMBaseTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData("Original")]
    public async Task Rename_InvalidOrUnchangedName_CancelsWithoutSaving(string? name)
    {
        var entity = new Deck { Id = 1, Name = "Original" };
        var deck = new DeckNode(entity, []);
        deck.BeginRenameCommand.Execute(null);
        deck.TempName = name;

        bool renamed = await deck.RenameAsync(_ => throw new InvalidOperationException("Unexpected save"));

        renamed.Should().BeFalse();
        deck.Name.Should().Be("Original");
        entity.Name.Should().Be("Original");
        deck.IsRenaming.Should().BeFalse();
        deck.TempName.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rename_PendingSave_PreservesStateAndCommitsOnlyTheSubmittedName(bool succeeds)
    {
        var entity = new Deck { Id = 1, Name = "Original" };
        var deck = new DeckNode(entity, []);
        deck.BeginRenameCommand.Execute(null);
        deck.TempName = "Submitted";
        var changes = new List<string?>();
        string? entityNameWhenNotified = null;
        deck.PropertyChanged += (_, args) =>
        {
            changes.Add(args.PropertyName);
            if (args.PropertyName == nameof(deck.Name))
                entityNameWhenNotified = entity.Name;
        };
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        string? submittedName = null;

        var saving = deck.RenameAsync(name =>
        {
            submittedName = name;
            return completion.Task;
        });
        deck.CancelRenameCommand.Execute(null); // Focus loss cannot cancel a submitted save.
        deck.BeginRenameCommand.Execute(null);
        bool duplicate = await deck.RenameAsync(_ => throw new InvalidOperationException("Duplicate save"));

        duplicate.Should().BeFalse();
        changes.Should().BeEmpty();
        submittedName.Should().Be("Submitted");
        deck.Name.Should().Be("Original");
        entity.Name.Should().Be("Original");
        deck.TempName.Should().Be("Submitted");
        deck.IsRenaming.Should().BeTrue();

        deck.TempName = "Later edit";
        completion.SetResult(succeeds);

        (await saving).Should().Be(succeeds);
        deck.Name.Should().Be(succeeds ? "Submitted" : "Original");
        entity.Name.Should().Be(succeeds ? "Submitted" : "Original");
        deck.TempName.Should().Be(succeeds ? null : "Later edit");
        deck.IsRenaming.Should().Be(!succeeds);
        entityNameWhenNotified.Should().Be(succeeds ? "Submitted" : null);

        if (!succeeds)
        {
            (await deck.RenameAsync(_ => Task.FromResult(true))).Should().BeTrue();
            deck.Name.Should().Be("Later edit");
            entity.Name.Should().Be("Later edit");
        }
    }

    [Fact]
    public async Task Rename_ThrowingSave_PreservesStateAndAllowsRetry()
    {
        var entity = new Deck { Id = 1, Name = "Original" };
        var deck = new DeckNode(entity, []);
        deck.BeginRenameCommand.Execute(null);
        deck.TempName = "Submitted";

        Func<Task> saving = () => deck.RenameAsync(_ => Task.FromException<bool>(new IOException()));

        await saving.Should().ThrowAsync<IOException>();
        deck.Name.Should().Be("Original");
        entity.Name.Should().Be("Original");
        deck.TempName.Should().Be("Submitted");
        deck.IsRenaming.Should().BeTrue();
        (await deck.RenameAsync(_ => Task.FromResult(true))).Should().BeTrue();
        entity.Name.Should().Be("Submitted");
    }
}