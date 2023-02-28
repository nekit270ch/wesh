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

        public static bool FindInList(List<string> list1, List<string> list2)
        {
            int mat = 0;
            List<string> found = new List<string>();
            if(list1.Count < list2.Count) return false;

            for(int i = 0; i < list1.Count; i++)
            {
                for(int j = 0; j < list2.Count; j++)
                {
                    if (!found.Contains(list2[j]) && (list1[i].Length > 1 ? list1[i].Contains(list2[j]) : list1[i] == list2[j]))
                    {
                        mat++;
                        found.Add(list2[j]);
                    }
                }
            }
            return mat == list2.Count;
        }

        public static IntPtr SetHotKey(string strKeys, bool stop, HotKeyCallback callback)
        {
            List<string> inKeys = strKeys.Split('+').ToList();
            List<string> keys = new List<string>();

            for(int i = 0; i < inKeys.Count; i++)
            {
                inKeys[i] = inKeys[i].Replace("Ctrl", "Control").Replace("Alt", "Menu");
            }

            return SetHook((nCode, wParam, lParam)=>{
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    string key = ((Keys)vkCode).ToString();

                    keys.Add(key);
                    if(keys.Count > inKeys.Count) keys.Clear();
                    if(FindInList(keys, inKeys))
                    {
                        keys.Clear();
                        callback();
                    }
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
    }
}
