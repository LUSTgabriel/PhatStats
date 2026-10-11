using System.IO.Ports;
using System.Text;
using PhatStats.Settings;

namespace PhatStats.Serial;

internal sealed class SerialTransport : IDisposable
{
    private SerialPort? _port;
    private byte[] _writeBuffer = new byte[512];

    public bool IsOpen => _port?.IsOpen == true;
    public string? PortName => _port?.PortName;

    public void Open(string portName, AppSettings settings)
    {
        DisposePort();
        var p = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            DtrEnable = settings.DtrEnable,
            RtsEnable = settings.RtsEnable,
            WriteTimeout = settings.WriteTimeoutMs,
            ReadTimeout = 500,
            Encoding = Encoding.ASCII
        };
        try { p.Open(); _port = p; }
        catch { p.Dispose(); throw; }
    }

    public void WriteAscii(string batch)
    {
        var p = _port ?? throw new InvalidOperationException("Serial port is not open.");
        var required = Encoding.ASCII.GetByteCount(batch);
        if (_writeBuffer.Length < required)
            Array.Resize(ref _writeBuffer, Math.Max(required, _writeBuffer.Length * 2));
        var count = Encoding.ASCII.GetBytes(batch, 0, batch.Length, _writeBuffer, 0);
        p.Write(_writeBuffer, 0, count);
    }

    public void AdoptFrom(SerialTransport candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var incoming = Interlocked.Exchange(ref candidate._port, null)
            ?? throw new InvalidOperationException("Candidate serial port is not open.");
        var old = Interlocked.Exchange(ref _port, incoming);
        if (old is not null)
        {
            try { if (old.IsOpen) old.Close(); } catch { }
            old.Dispose();
        }
    }

    public void Close() => DisposePort();

    private void DisposePort()
    {
        var p = Interlocked.Exchange(ref _port, null);
        if (p is null) return;
        try { if (p.IsOpen) p.Close(); } catch { }
        p.Dispose();
    }

    public void Dispose()
    {
        DisposePort();
        _writeBuffer = Array.Empty<byte>();
    }
}
