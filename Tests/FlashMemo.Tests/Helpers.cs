using System.Reflection;
using System.Windows.Threading;
using AutoMapper;
using FlashMemo.Model;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests;

public static class Helpers
{
    public static SafetyNet GetSafetyNet(IDialogService? dialogs = null)
        => new(NullLogger<SafetyNet>.Instance, dialogs ?? Mock.Of<IDialogService>());

    // FlowDocument conversion needs an STA thread, but no Application or window.
    public static Task RunOnSta(Action action)
        => RunOnSta(() => { action(); return Task.CompletedTask; });

    public static Task RunOnSta(Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke((Action)(async () =>
            {
                try { await action(); completion.SetResult(); }
                catch (Exception exception) { completion.SetException(exception); }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
            }));
            Dispatcher.Run();
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    public static IMapper GetMapper()
    {
        return new MapperConfiguration(cfg => 
        cfg.AddProfile<MappingProfile>(), new LoggerFactory())
        .CreateMapper();
    }

    public static TField GetPrivateField<TField>(this object from, string fieldName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        
        FieldInfo field = from.GetType().GetField(fieldName, flags) 
            ?? throw new InvalidOperationException(
            "Couldnt find field with given name.");
            
        return (TField?)field.GetValue(from) 
            ?? throw new NullReferenceException("Couldnt get the field from object.");
    }

    public static async Task ReloadDomain(this BaseVM vm, IVMEventBus bus)
    {
        vm.OnFocusLost();
        bus.NotifyDomain();
        await vm.OnFocusGained();
    }
}
