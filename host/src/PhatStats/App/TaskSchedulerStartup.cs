using System.Security.Principal;
using PhatStats.Diagnostics;

namespace PhatStats.App;

internal sealed class TaskSchedulerStartup
{
    private const string TaskName = "PhatStats";
    private readonly SevereErrorRing _errors;
    public TaskSchedulerStartup(SevereErrorRing errors) => _errors = errors;

    public bool SetEnabled(bool enabled)
    {
        try
        {
            var type = Type.GetTypeFromProgID("Schedule.Service") ?? throw new InvalidOperationException("Task Scheduler COM service is unavailable.");
            dynamic service = Activator.CreateInstance(type)!;
            service.Connect();
            dynamic root = service.GetFolder("\\");
            if (!enabled)
            {
                try { root.DeleteTask(TaskName, 0); } catch { }
                return true;
            }

            dynamic task = service.NewTask(0);
            task.RegistrationInfo.Description = "Start PhatStats at interactive user logon.";
            task.Settings.Enabled = true;
            task.Settings.StartWhenAvailable = true;
            task.Settings.DisallowStartIfOnBatteries = false;
            task.Settings.StopIfGoingOnBatteries = false;
            task.Settings.MultipleInstances = 2; // IgnoreNew
            task.Principal.UserId = WindowsIdentity.GetCurrent().Name;
            task.Principal.LogonType = 3; // TASK_LOGON_INTERACTIVE_TOKEN
            task.Principal.RunLevel = 1; // TASK_RUNLEVEL_HIGHEST
            dynamic trigger = task.Triggers.Create(9); // TASK_TRIGGER_LOGON
            trigger.UserId = WindowsIdentity.GetCurrent().Name;
            dynamic action = task.Actions.Create(0); // TASK_ACTION_EXEC
            action.Path = Application.ExecutablePath;
            action.WorkingDirectory = AppContext.BaseDirectory;
            root.RegisterTaskDefinition(TaskName, task, 6, null, null, 3, null); // CREATE_OR_UPDATE
            return true;
        }
        catch (Exception ex)
        {
            _errors.Add("Startup", "TASK_SCHEDULER_FAILED", ex.Message);
            return false;
        }
    }
}
