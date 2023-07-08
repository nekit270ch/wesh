using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
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
        private static IntPtr _hookID = IntPtr.Zero;

        public static IntPtr SetKeyHandler(bool stop, KeyHandlerCallback callback)
        {
            return SetHook((nCode, wParam, lParam)=>{
                int vkCode = Marshal.ReadInt32(lParam);
                string key = ((Keys)vkCode).ToString();

                if(nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) callback(key);

                return stop?_hookID:CallNextHookEx(_hookID, nCode, wParam, lParam);
            });
        }

        public static IntPtr SetHotKey(string skey, bool ctrl, bool alt, bool stop, HotKeyCallback callback)
        {
            bool ctrlPressed = false, altPressed = false;

            return SetHook((nCode, wParam, lParam)=>{
                int vkCode = Marshal.ReadInt32(lParam);
                string key = ((Keys)vkCode).ToString();

                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (key.Contains("ControlKey")) ctrlPressed = true;
                    if(key.Contains("Menu")) altPressed = true;
                    else if ((ctrl?ctrlPressed:true) && (alt?altPressed:true) && key == skey) callback();
                }
                else if(nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
                {
                    if (key.Contains("ControlKey")) ctrlPressed = false;
                    if (key.Contains("Menu")) altPressed = false;
                }

                return stop?_hookID:CallNextHookEx(_hookID, nCode, wParam, lParam);
            });
        }

        public static void UnsetHotKey(IntPtr id)
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate void HotKeyCallback();
        public delegate void KeyHandlerCallback(string key);
    }
}
