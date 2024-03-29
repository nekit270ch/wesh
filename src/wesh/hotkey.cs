using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace wesh
{
    class HotKey
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private readonly static IntPtr hookID = IntPtr.Zero;
        private static GCHandle handle;

        public static IntPtr SetKeyHandler(bool stop, KeyHandlerCallback callback)
        {
            return SetHook((nCode, wParam, lParam)=>{
                int vkCode = Marshal.ReadInt32(lParam);
                Keys? kKey = (Keys)vkCode;
                if(kKey != null)
                {
                    string key = kKey.ToString();
                    if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) callback(key);
                }

                return stop?hookID:CallNextHookEx(hookID, nCode, wParam, lParam);
            });
        }

        public static IntPtr SetHotKey(string skey, bool ctrl, bool alt, bool shift, bool stop, HotKeyCallback callback)
        {
            bool ctrlPressed = false, altPressed = false, shiftPressed = false;

            return SetHook((nCode, wParam, lParam)=>{
                int vkCode = Marshal.ReadInt32(lParam);
                Keys? kKey = ((Keys)vkCode);

                if(kKey != null)
                {
                    string key = kKey.ToString();

                    if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                    {
                        if (key.Contains("ControlKey")) ctrlPressed = true;
                        if (key.Contains("Menu")) altPressed = true;
                        if (key.Contains("ShiftKey")) shiftPressed = true;
                        else if ((ctrl ? ctrlPressed : true) && (alt ? altPressed : true) && (shift ? shiftPressed : true) && key == skey) callback();
                    }
                    else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
                    {
                        if (key.Contains("ControlKey")) ctrlPressed = false;
                        if (key.Contains("Menu")) altPressed = false;
                        if (key.Contains("ShiftKey")) shiftPressed = false;
                    }
                }

                return stop?hookID:CallNextHookEx(hookID, nCode, wParam, lParam);
            });
        }

        public static void UnsetHotKey()
        {
            UnhookWindowsHookEx(hookID);
            handle.Free();
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            handle = GCHandle.Alloc(proc, GCHandleType.Normal);

            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate void HotKeyCallback();
        public delegate void KeyHandlerCallback(string key);
    }
}
