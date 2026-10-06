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
        if (!Screen.AllScreens.Any(s => s.Bounds.Contains(position)))
            throw new ArgumentOutOfRangeException(nameof(position), "Klickpunkt liegt außerhalb der sichtbaren Monitore.");
        var desktop = SystemInformation.VirtualScreen;
        if (desktop.Width < 2 || desktop.Height < 2) throw new InvalidOperationException("Ungültige Desktopgeometrie.");
        var inputs = new[]
        {
            Mouse((int)Math.Round(((long)position.X - desktop.Left) * 65535d / (desktop.Width - 1)),
                  (int)Math.Round(((long)position.Y - desktop.Top) * 65535d / (desktop.Height - 1)), 0x8000 | 0x4000 | 0x0001),
            Mouse(0, 0, 0x0002),
            Mouse(0, 0, 0x0004)
        };
        var inserted = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (inserted != inputs.Length)
        {
            var error = Marshal.GetLastWin32Error();
            // In case only movement and down were accepted, release the button.
            // This is error cleanup, never a second click or an input bypass.
            if (inserted == 2) SendInput(1, new[] { Mouse(0, 0, 0x0004) }, Marshal.SizeOf<Input>());
            throw new Win32Exception(error, "Windows hat die Mauseingabe nicht vollständig angenommen.");
        }
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
