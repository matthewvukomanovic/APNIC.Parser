using System;
using System.Runtime.InteropServices;

namespace apnicparser
{
    class ClipboardHelper
    {
        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        internal static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

        public static void SetClipboard(string value)
        {
            if( OpenClipboard(IntPtr.Zero))
            {
                if( EmptyClipboard())
                {
                    var ptr = Marshal.StringToHGlobalUni(value);
                    if(!SetClipboardData(13, ptr))
                    {
                        // Only free the data if setting the clipboard wasn't successful, the memory is managed by the system once it is set
                        Marshal.FreeHGlobal(ptr);
                    }
                    if (!CloseClipboard())
                    {
#if DEBUG
                        Console.Error.WriteLine("Could not close the clipboard");
#endif
                    }
                }
            }
        }
    }
}