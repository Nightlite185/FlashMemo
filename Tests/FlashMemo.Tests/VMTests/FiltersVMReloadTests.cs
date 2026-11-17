using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class FiltersVMReloadTests
{
    [Theory]
    [InlineData("decks")]
    [InlineData("tags")]
    [InlineData(null)]
    public async Task Reload_KeepsTheCurrentCollectionsUntilBothLoadsSucceed(string? failedQuery)
    {
        const long userId = 7;
        var oldDeck = new DeckNode(new Deck { Id = 1, UserId = userId, Name = "Old deck" }, []);
        var oldTag = new Tag { Id = 2, UserId = userId, Name = "Old tag" };
        var newDeck = new DeckNode(new Deck { Id = 1, UserId = userId, Name = "Renamed deck" }, []);
        DeckNode[] replacementDecks =
        [
            newDeck,
            new(new Deck { Id = 3, UserId = userId, Name = "New deck" }, [])
        ];
        Tag[] replacementTags =
        [
            new() { Id = 2, UserId = userId, Name = "Renamed tag" },
            new() { Id = 4, UserId = userId, Name = "New tag" }
        ];

        var tree = new Mock<IDeckTreeBuilder>(MockBehavior.Strict);
        tree.Setup(builder => builder.BuildAsync(userId)).ReturnsAsync(new[] { oldDeck });
        var tags = new Mock<ITagRepo>(MockBehavior.Strict);
        tags.Setup(repo => repo.GetFromUser(userId)).ReturnsAsync(new[] { oldTag });
        var bus = new VMEventBus();
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        if (failedQuery is not null)
        {
            dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Returns(DialogResult.Cancel);
        }

        var vm = new FiltersVM(tree.Object, tags.Object, bus, Mock.Of<ILastSessionService>(),
            userId, NullLogger<FiltersVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        await vm.InitializeAsync();

        try
        {
            oldDeck.IsSelected = true;
            var oldTagVM = vm.Tags.Single();
            oldTagVM.IsSelected = true;
            var originalDeckTree = vm.DeckTree;
            var originalTags = vm.Tags;
            int collectionChanges = 0;
            vm.DeckTree.CollectionChanged += (_, _) => collectionChanges++;
            vm.Tags.CollectionChanged += (_, _) => collectionChanges++;

            var decksLoaded = new TaskCompletionSource<IEnumerable<DeckNode>>();
            var tagsLoaded = new TaskCompletionSource<ICollection<Tag>>();
            var tagsRequested = new TaskCompletionSource();
            tree.Setup(builder => builder.BuildAsync(userId)).Returns(decksLoaded.Task);
            tags.Setup(repo => repo.GetFromUser(userId))
                .Callback(() => tagsRequested.SetResult())
                .Returns(tagsLoaded.Task);

            var reloading = vm.ReloadDomain(bus);
            collectionChanges.Should().Be(0);
            vm.DeckTree.Should().Equal(oldDeck);
            vm.Tags.Should().Equal(oldTagVM);

            if (failedQuery == "decks")
            {
                decksLoaded.SetException(new IOException("Deck lookup failed"));
            }
            else
            {
                decksLoaded.SetResult(replacementDecks);
                await tagsRequested.Task;
                collectionChanges.Should().Be(0);
                vm.DeckTree.Should().Equal(oldDeck);
                vm.Tags.Should().Equal(oldTagVM);

                if (failedQuery == "tags")
                    tagsLoaded.SetException(new IOException("Tag lookup failed"));
                else
                    tagsLoaded.SetResult(replacementTags);
            }

            await reloading;

            if (failedQuery is not null)
            {
                collectionChanges.Should().Be(0);
                vm.DeckTree.Should().Equal(oldDeck);
                vm.Tags.Should().Equal(oldTagVM);
                oldDeck.IsSelected.Should().BeTrue();
                oldTagVM.IsSelected.Should().BeTrue();

                // The failed reload stays dirty and can recover on the next focus change.
                tree.Setup(builder => builder.BuildAsync(userId)).ReturnsAsync(replacementDecks);
                tags.Setup(repo => repo.GetFromUser(userId)).ReturnsAsync(replacementTags);
                vm.OnFocusLost();
                await vm.OnFocusGained();
            }

            vm.DeckTree.Should().BeSameAs(originalDeckTree);
            vm.Tags.Should().BeSameAs(originalTags);
            vm.DeckTree.Should().Equal(replacementDecks);
            vm.Tags.Select(tag => tag.Id).Should().Equal(2, 4);
            vm.DeckTree.Where(deck => deck.IsSelected).Should().Equal(newDeck);
            vm.Tags.Where(tag => tag.IsSelected).Select(tag => tag.Id).Should().Equal(2);
            dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
                failedQuery is null ? Times.Never() : Times.Once());
            dialogs.VerifyNoOtherCalls();
        }
        finally { vm.OnClosed(); }
    }
}