using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class UserSelectVMTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenameUser_CommitsSubmittedNameOnlyAfterSuccessfulSave(bool succeeds)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repo = new Mock<IUserRepo>(MockBehavior.Strict);
        repo.Setup(service => service.Rename(2, "Submitted")).Returns(completion.Task);
        var dialogs = new Mock<IDialogService>();
        var vm = CreateVM(repo: repo.Object, dialogs: dialogs.Object);
        var entity = new UserEntity { Id = 2, Name = "Original" };
        var user = new UserVM(entity, default);
        user.BeginRenameCommand.Execute(null);
        user.TempName = "Submitted";

        var saving = vm.RenameUserCommand.ExecuteAsync(user);
        user.CancelRenameCommand.Execute(null);
        user.TempName = "Later edit";
        user.Name.Should().Be("Original");
        entity.Name.Should().Be("Original");

        if (succeeds)
            completion.SetResult();
        else
            completion.SetException(new IOException());
        await saving;

        user.Name.Should().Be(succeeds ? "Submitted" : "Original");
        entity.Name.Should().Be(user.Name);
        user.IsRenaming.Should().Be(!succeeds);
        user.TempName.Should().Be(succeeds ? null : "Later edit");
        repo.Verify(service => service.Rename(2, "Submitted"), Times.Once);
        repo.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData(DialogResult.Yes)]
    [InlineData(DialogResult.No)]
    public async Task RemoveUser_WhenPreferencesFail_LogsAndUsesDeletionConfirmation(DialogResult answer)
    {
        var repo = new Mock<IUserRepo>(MockBehavior.Strict);
        repo.Setup(service => service.Remove(2)).Returns(Task.CompletedTask);
        var session = new Mock<ILastSessionService>(MockBehavior.Strict);
        session.SetupGet(service => service.LastUserId).Returns((long?)null);
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        var failure = new IOException();
        options.Setup(service => service.GetFromUser(1)).ThrowsAsync(failure);
        var logger = new Mock<ILogger<UserSelectVM>>();
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.Show("Delete user?", It.IsAny<string>(),
                DialogButtons.YesNo, DialogIcons.Warning))
            .Returns(answer);
        var vm = CreateVM(repo: repo.Object, session: session.Object,
            options: options.Object, dialogs: dialogs.Object, logger: logger.Object);
        var user = CreateUser(2);
        vm.Users.Add(user);

        await vm.RemoveUserCommand.ExecuteAsync(user);

        if (answer is DialogResult.Yes)
            vm.Users.Should().BeEmpty();
        else
            vm.Users.Should().Equal(user);
        repo.Verify(service => service.Remove(2), answer is DialogResult.Yes ? Times.Once() : Times.Never());
        repo.VerifyNoOtherCalls();
        session.VerifyGet(service => service.LastUserId,
            answer is DialogResult.Yes ? Times.Once() : Times.Never());
        session.VerifyNoOtherCalls();
        options.Verify(service => service.GetFromUser(1), Times.Once);
        options.VerifyNoOtherCalls();
        logger.Verify(service => service.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), failure, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        dialogs.Verify(service => service.Show("Delete user?", It.IsAny<string>(),
            DialogButtons.YesNo, DialogIcons.Warning), Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RemoveUser_UpdatesListAfterDeletion_AndCleanupFailureDoesNotUndoIt(
        bool deletionSucceeds, bool cleanupSucceeds)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repo = new Mock<IUserRepo>(MockBehavior.Strict);
        repo.Setup(service => service.Remove(2)).Returns(completion.Task);
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        options.Setup(service => service.GetFromUser(1))
            .ReturnsAsync(new UserOptions { ConfirmUserDeletion = false });
        var session = new Mock<ILastSessionService>(MockBehavior.Strict);
        session.SetupGet(service => service.LastUserId).Returns(2L);
        session.Setup(service => service.ClearLastUserAsync())
            .Returns(cleanupSucceeds ? Task.CompletedTask : Task.FromException(new IOException()));
        var dialogs = new Mock<IDialogService>();
        var vm = CreateVM(repo: repo.Object, session: session.Object,
            options: options.Object, dialogs: dialogs.Object);
        var user = CreateUser(2);
        vm.Users.Add(user);

        var removing = vm.RemoveUserCommand.ExecuteAsync(user);
        vm.Users.Should().Equal(user);
        session.Verify(service => service.ClearLastUserAsync(), Times.Never);

        if (deletionSucceeds)
            completion.SetResult();
        else
            completion.SetException(new IOException());
        await removing;

        if (deletionSucceeds)
            vm.Users.Should().BeEmpty();
        else
            vm.Users.Should().Equal(user);
        repo.Verify(service => service.Remove(2), Times.Once);
        repo.VerifyNoOtherCalls();
        session.Verify(service => service.ClearLastUserAsync(),
            deletionSucceeds ? Times.Once() : Times.Never());
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            deletionSucceeds ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CreateUser_UpdatesListAndClearsSubmittedDraftOnlyAfterSuccessfulSave(
        bool succeeds, bool editsWhileSaving)
    {
        var completion = new TaskCompletionSource<UserEntity>(TaskCreationOptions.RunContinuationsAsynchronously);
        var repo = new Mock<IUserRepo>(MockBehavior.Strict);
        repo.Setup(service => service.CreateNew(It.Is<UserEntity>(user => user.Name == "Submitted")))
            .Returns(completion.Task);
        var saved = new UserEntity { Id = 2, Name = "Submitted" };
        var savedVM = new UserVM(saved, default);
        var builder = new Mock<IUserVMBuilder>(MockBehavior.Strict);
        builder.Setup(service => service.BuildUncounted(saved)).Returns(savedVM);
        var dialogs = new Mock<IDialogService>();
        var vm = CreateVM(repo: repo.Object, builder: builder.Object, dialogs: dialogs.Object);
        vm.NewUsernameField = "Submitted";

        var creating = vm.CreateUserCommand.ExecuteAsync(vm.NewUsernameField);
        vm.Users.Should().BeEmpty();
        vm.NewUsernameField.Should().Be("Submitted");
        if (editsWhileSaving)
            vm.NewUsernameField = "Later edit";

        if (succeeds)
            completion.SetResult(saved);
        else
            completion.SetException(new IOException());
        await creating;

        if (succeeds)
            vm.Users.Should().Equal(savedVM);
        else
            vm.Users.Should().BeEmpty();
        vm.NewUsernameField.Should().Be(editsWhileSaving ? "Later edit" : succeeds ? "" : "Submitted");
        builder.Verify(service => service.BuildUncounted(saved), succeeds ? Times.Once() : Times.Never());
        builder.VerifyNoOtherCalls();
        repo.Verify(service => service.CreateNew(It.IsAny<UserEntity>()), Times.Once);
        repo.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Login_ClosesOnlyAfterSuccessfulUserChange(bool succeeds)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var login = new Mock<ILoginService>(MockBehavior.Strict);
        login.Setup(service => service.ChangeUser(2)).Returns(completion.Task);
        var dialogs = new Mock<IDialogService>();
        var vm = CreateVM(login: login.Object, dialogs: dialogs.Object);
        int closeRequests = 0;
        vm.OnCloseRequest += () => closeRequests++;

        var loggingIn = vm.LoginCommand.ExecuteAsync(CreateUser(2));
        closeRequests.Should().Be(0);

        if (succeeds)
            completion.SetResult();
        else
            completion.SetException(new IOException());
        await loggingIn;

        closeRequests.Should().Be(succeeds ? 1 : 0);
        login.Verify(service => service.ChangeUser(2), Times.Once);
        login.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
    }

    [Fact]
    public async Task Login_CurrentUser_ClosesWithoutChangingSession()
    {
        var login = new Mock<ILoginService>(MockBehavior.Strict);
        var vm = CreateVM(login: login.Object);
        int closeRequests = 0;
        vm.OnCloseRequest += () => closeRequests++;

        await vm.LoginCommand.ExecuteAsync(CreateUser(1));

        closeRequests.Should().Be(1);
        login.VerifyNoOtherCalls();
    }

    private static UserVM CreateUser(long id)
        => new(new UserEntity { Id = id, Name = $"User {id}" }, default);

    private static UserSelectVM CreateVM(IUserRepo? repo = null, IUserVMBuilder? builder = null,
        ILoginService? login = null, ILastSessionService? session = null,
        IUserOptionsService? options = null, IDialogService? dialogs = null,
        ILogger<UserSelectVM>? logger = null)
    {
        dialogs ??= Mock.Of<IDialogService>();
        return new UserSelectVM(repo ?? Mock.Of<IUserRepo>(), builder ?? Mock.Of<IUserVMBuilder>(),
            login ?? Mock.Of<ILoginService>(), session ?? Mock.Of<ILastSessionService>(),
            options ?? Mock.Of<IUserOptionsService>(), logger ?? NullLogger<UserSelectVM>.Instance,
            dialogs, Helpers.GetSafetyNet(dialogs), currentUserId: 1);
    }
}