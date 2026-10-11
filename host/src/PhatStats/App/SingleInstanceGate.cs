using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace PhatStats.App;

public sealed class SingleInstanceGate : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activateEvent;
    private readonly ManualResetEvent _stopEvent = new(false);
    private readonly Thread? _listener;
    private bool _ownsMutex;

    private SingleInstanceGate(Mutex mutex, EventWaitHandle activateEvent, bool owns)
    {
        _mutex = mutex; _activateEvent = activateEvent; _ownsMutex = owns;
        if (owns)
        {
            _listener = new Thread(Listen) { IsBackground = true, Name = "PhatStats.SingleInstance" };
            _listener.Start();
        }
    }

    public event EventHandler? ActivateRequested;
    public bool IsPrimary => _ownsMutex;

    public static SingleInstanceGate Open()
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sid))).Substring(0, 16);
        var mutex = new Mutex(false, $"Local\\PhatStats-{suffix}");
        bool owns;
        try { owns = mutex.WaitOne(0, false); }
        catch (AbandonedMutexException) { owns = true; }
        var ev = new EventWaitHandle(false, EventResetMode.AutoReset, $"Local\\PhatStats-Activate-{suffix}");
        return new SingleInstanceGate(mutex, ev, owns);
    }

    public void SignalExisting() { if (!_ownsMutex) _activateEvent.Set(); }

    private void Listen()
    {
        var handles = new WaitHandle[] { _activateEvent, _stopEvent };
        while (WaitHandle.WaitAny(handles) == 0)
            ActivateRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _stopEvent.Set();
        try { _listener?.Join(1000); } catch { }
        if (_ownsMutex) { try { _mutex.ReleaseMutex(); } catch { } _ownsMutex = false; }
        _stopEvent.Dispose(); _activateEvent.Dispose(); _mutex.Dispose();
    }
}
