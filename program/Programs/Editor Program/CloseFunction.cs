using System.Runtime.InteropServices;
using Windows;

namespace Editor
{
    public readonly struct CloseFunction
    {
        [UnmanagedCallersOnly]
        public static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }
    }
}