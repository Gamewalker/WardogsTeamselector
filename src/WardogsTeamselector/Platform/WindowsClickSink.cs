using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WardogsTeamselector.Core;

namespace WardogsTeamselector.Platform;

public sealed class WindowsClickSink : IClickSink
{
    public void Click(Point position)
    {
        var monitor = Screen.AllScreens.FirstOrDefault(s => s.Bounds.Contains(position));
        if (monitor is null)
            throw new ArgumentOutOfRangeException(nameof(position), "Klickpunkt liegt außerhalb der sichtbaren Monitore.");
        var desktop = SystemInformation.VirtualScreen;
        if (desktop.Width < 2 || desktop.Height < 2) throw new InvalidOperationException("Ungültige Desktopgeometrie.");
        var inputs = BuildInputs(position, desktop, monitor.Bounds);
        var inserted = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (inserted != inputs.Length)
        {
            var error = Marshal.GetLastWin32Error();
            // In case only movement and down were accepted, release the button.
            // This is error cleanup, never a second click or an input bypass.
            if (inserted == inputs.Length - 1) SendInput(1, new[] { Mouse(0, 0, 0x0004) }, Marshal.SizeOf<Input>());
            throw new Win32Exception(error, "Windows hat die Mauseingabe nicht vollständig angenommen.");
        }
    }

    private static Input[] BuildInputs(Point position, Rectangle desktop, Rectangle monitor)
    {
        // A recreated game dialog can lose its hover target while the pointer stays
        // at the same coordinates. Move one pixel and back before every click.
        // Keep both positions on the target monitor and preserve both move events.
        var adjacent = new Point(position.X < monitor.Right - 1 ? position.X + 1 : position.X - 1, position.Y);
        Input Move(Point point) => Mouse(
            (int)Math.Round(((long)point.X - desktop.Left) * 65535d / (desktop.Width - 1)),
            (int)Math.Round(((long)point.Y - desktop.Top) * 65535d / (desktop.Height - 1)),
            0x8000 | 0x4000 | 0x2000 | 0x0001);
        return new[] { Move(adjacent), Move(position), Mouse(0, 0, 0x0002), Mouse(0, 0, 0x0004) };
    }

    private static Input Mouse(int x, int y, uint flags) => new()
    {
        Type = 0,
        Data = new InputUnion { Mouse = new MouseInput { X = x, Y = y, Flags = flags } }
    };

    // INPUT's union includes every native member so alignment and size match
    // Win32 on x86 and x64 (28 and 40 bytes respectively).
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public HardwareInput Hardware;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput
    {
        public int X; public int Y; public uint MouseData; public uint Flags; public uint Time; public UIntPtr ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    {
        public ushort VirtualKey; public ushort Scan; public uint Flags; public uint Time; public UIntPtr ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)] private struct HardwareInput { public uint Message; public ushort Low; public ushort High; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, [In] Input[] inputs, int size);
}
