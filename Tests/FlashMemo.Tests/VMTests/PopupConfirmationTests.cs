using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class PopupConfirmationTests
{
    [Theory]
    [InlineData("create", false)]
    [InlineData("create", true)]
    [InlineData("deck", false)]
    [InlineData("deck", true)]
    [InlineData("reschedule", false)]
    [InlineData("reschedule", true)]
    [InlineData("note", false)]
    [InlineData("note", true)]
    public async Task Confirm_ClosesOnlyWhenCallbackSucceeds(string kind, bool succeeds)
    {
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        var net = Helpers.GetSafetyNet(dialogs.Object);
        int confirmations = 0;
        int closes = 0;
        Task Confirm()
        {
            confirmations++;
            return succeeds ? Task.CompletedTask : Task.FromException(new IOException());
        }
        void Close() => closes++;

        var deck = new DeckNode(new Deck { Id = 1, UserId = 7, Name = "Deck" }, []);
        PopupVMBase vm = kind switch
        {
            "create" => new CreateDeckVM((_, _) => Confirm(), Close,
                NullLogger<CreateDeckVM>.Instance, dialogs.Object, net) { NameField = "New deck" },
            "deck" => new DeckSelectVM(_ => Confirm(), Close, [deck],
                NullLogger<DeckSelectVM>.Instance, dialogs.Object, net) { SelectedDeck = deck },
            "reschedule" => new RescheduleVM(_ => Confirm(), Close,
                NullLogger<RescheduleVM>.Instance, dialogs.Object, net),
            "note" => new NoteTypeSelectVM(_ => Confirm(), Close, net),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        await vm.ConfirmCommand.ExecuteAsync(null);

        confirmations.Should().Be(1);
        closes.Should().Be(succeeds ? 1 : 0);
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
        dialogs.VerifyNoOtherCalls();
    }
}
