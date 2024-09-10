using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.JScript;
using System.Security.Principal;
using Microsoft.Win32;
using System.IO.Compression;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Threading.Tasks;
using DllCallerLib;
using System.Reflection;
using Microsoft.JScript.Vsa;
using System.Data;

namespace wesh
{
    public class WESH
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, long lParam, StringBuilder wParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int SendMessage(IntPtr hWnd, uint wMsg, long lParam, long wParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, long lParam, long wParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int PostMessage(IntPtr hWnd, int wMsg, long lParam, StringBuilder wParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int PostMessage(IntPtr hWnd, int wMsg, long lParam, long wParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndhildAfter, string lpClass, string lpWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndA, int X, int Y, int W, int H, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref W32Rect rect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EnumWindows(W32EnumWindowsProc proc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void keybd_event(byte bVk, byte bScan, ulong dwFlags, int extraInfo);

        [DllImport("winmm.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern uint mciSendString(string lpstrCommand, StringBuilder lpstrReturnString, int uReturnLength, IntPtr hWndCallback);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetDesktopWindow();

        private struct W32Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        delegate bool W32EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_MBUTTONDOWN = 0x0207;
        private const uint WM_MBUTTONUP = 0x0208;
        private const uint WM_LBUTTONDBLCLK = 0x0203;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const uint WM_MOUSEMOVE = 0x0200;
        private const uint BM_CLICK = 0xf5;

        public delegate string Command(string[] args);

        public const double Version = 2.7;
        public const string VersionStr = "WESH v2.7";
        public const string WeshNull = "__WESH_NULL";
        public static readonly string WeshPath = Process.GetCurrentProcess().MainModule.FileName;
        public static readonly string WeshDir = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).Directory.FullName;
        private static readonly Random rand = new Random((int)DateTime.Now.Ticks);
        private static bool IsDefaultAssembliesLoaded = false;
        private static VsaEngine JSEngine = VsaEngine.CreateEngine();
        private static DataTable dt = new DataTable();

        public static Dictionary<string, Command> Commands = new Dictionary<string, Command>()
        {
            {".", (args)=>{
                return dt.Compute(String.Join(" ", args), "").ToString();
            } },

            {"cond", (args)=>{
                return BoolToString(Cond(String.Join(" ", args)));
            } },

            {"bitop", (args)=>{
                if(args[0] == "inv")
                {
                    return (~ToInt(args[1])).ToString();
                }
                else
                {
                    int total = ToInt(args[1]);

                    for(int i = 2; i < args.Length; i++)
                    {
                        if(args[0] == "or") total |= ToInt(args[i]);
                        else if(args[0] == "xor") total ^= ToInt(args[i]);
                        else if(args[0] == "and") total &= ToInt(args[i]);
                        else if(args[0] == "lsh") total <<= ToInt(args[i]);
                        else if(args[0] == "rsh") total >>= ToInt(args[i]);
                    }

                    return total.ToString();
                }
            } },

            {"echo", (args)=>{
                return args[0];
            } },

            {"return", (args)=>{
                return args[0];
            } },

            {"set", (args)=>{
                SetVariable(args[0], args[1]);
                return "";
            } },

            {"sete", (args)=>{
                if(Constants.Contains(args[0])) throw new Exception(args[0] + " является константой.");
                if (args[0].Contains('.'))
                {
                    string[] sp = args[0].Split('.');
                    if(!Variables.ContainsKey(sp[0]) || !UserObjects.ContainsKey(Variables[sp[0]])) return "ERROR: Object not found";
                    if (UserObjects[Variables[sp[0]]].ContainsKey(sp[1]))
                    {
                        UserObjects[Variables[sp[0]]][sp[1]] = Exec(args[1]);
                    }
                    else
                    {
                        UserObjects[Variables[sp[0]]].Add(sp[1], Exec(args[1]));
                    }
                }
                else
                {
                    if (Variables.ContainsKey(args[0]))
                    {
                        Variables[args[0]] = Exec(args[1]);
                    }
                    else
                    {
                        Variables.Add(args[0], Exec(args[1]));
                    }
                }
                return "";
            } },

            {"const", (args)=>{
                Constants.AddRange(args);
                return "";
            } },

            {"var.list", (args)=>{
                return String.Join(Environment.NewLine, Variables.Keys);
            } },

            {"var.get", (args)=>{
                return Variables[args[0]];
            } },

            {"var.set", (args)=>{
                Variables[args[0]] = args[1];
                return "";
            } },

            {"var.delete", (args)=>{
                foreach(string var in args) Variables.Remove(var);
                return "";
            } },

            {"exit", (args)=>{
                Environment.Exit((args.Length > 0?ToInt(args[0]):0));
                return "";
            } },

            {"exec", (args)=>{
                return Exec(args[0]);
            } },

            {"load", (args)=>{
                foreach(string arg in args)
                {
                    string code = "";
                    if((arg.EndsWith(".wesh") || arg.EndsWith(".weshm")) && GetPath(arg) != null) code = File.ReadAllText(GetPath(arg));
                    else if(arg.StartsWith("http")) code = GetRequest(arg);
                    else
                    {
                        string name = Variables["modulesDir"] + "\\" + args[0] + ".weshm";
                        if(!File.Exists(name)) Commands["module.install"](args);
                        code = File.ReadAllText(name);
                    }
                    ExecScript(code);
                }
                return "";
            } },

            {"loadext", (args)=>{
                string code = File.ReadAllText(GetPath(args[0]));
                
                string[] refAsm = args.Length > 1 ? args[1].Split(',') : new string[0];

                var prov = new CSharpCodeProvider();

                var cParams = new CompilerParameters();
                cParams.GenerateExecutable = false;
                cParams.GenerateInMemory = true;
                cParams.ReferencedAssemblies.AddRange(new string[]{ "mscorlib.dll", "system.dll", WeshPath });
                if(refAsm.Length > 0) cParams.ReferencedAssemblies.AddRange(refAsm);

                var res = prov.CompileAssemblyFromSource(cParams, code);

                if (res.Errors.HasErrors)
                {
                    string r = "";
                    foreach(CompilerError err in res.Errors)
                    {
                        r += "Compiler error: "+err.ErrorText+" at line "+err.Line+Environment.NewLine;
                    }
                    throw new Exception(r);
                }

                var asm = res.CompiledAssembly;
                var method = asm.GetType("WeshExtension").GetMethod("Main");

                var ret = method.Invoke(null, new object[0]);

                return "";
            } },

            {"async", (args)=>{
                Task.Run(()=>{
                    Exec(args[0]);
                });
                return "";
            } },

            {"try", (args)=>{
                try
                {
                    Exec(args[0], false, false);
                }catch(Exception ex)
                {
                    SetVariable("err", ex.Message);
                    Exec(args[1]);
                }
                return "";
            } },

            {"if", (args)=>{
                bool hasElseBlock = args[args.Length - 2] == "else";
                int offset = hasElseBlock ? 3 : 1;

                if (Cond(ParseArg(String.Join(" ", args.Take(args.Length - offset)))))
                {
                    return Exec(args[args.Length - offset]);
                }
                else
                {
                    return Exec(hasElseBlock ? args[args.Length - 1] : "");
                }
            } },

            {"ife", (args)=>{
                bool hasElseBlock = args[args.Length - 2] == "else";
                int offset = hasElseBlock ? 3 : 1;

                string trueCode = args[args.Length - offset];
                string falseCode = hasElseBlock ? args[args.Length - 1]: "";

                bool result = false;

                for(int i = 0; i < args.Length - offset; i++)
                {
                    string arg = args[i];

                    if(i > 0)
                    {
                        if(arg == "or")
                        {
                            bool rs = false;

                            if(args[i+1] == "not")
                            {
                                rs = Exec(args[i+2]) != "true";
                                i++;
                            }
                            else
                            {
                                rs = Exec(args[i+1]) == "true";
                            }

                            result = result || rs;
                            i++;
                        }else if(arg == "and")
                        {
                            bool rs = false;

                            if(args[i+1] == "not")
                            {
                                rs = Exec(args[i+2]) != "true";
                                i++;
                            }
                            else
                            {
                                rs = Exec(args[i+1]) == "true";
                            }

                            result = result && rs;
                            i++;
                        }
                        else
                        {
                            result = Exec(arg) == "true";
                        }
                    }
                    else
                    {
                        result = Exec(arg) == "true";
                    }
                }

                return Exec(result?trueCode:falseCode);
            } },

            {"while", (args)=>{
                string cond = String.Join(" ", args.Take(args.Length - 1));
                string code = args[args.Length - 1];

                while (Cond(ParseArg(cond)))
                {
                    Exec(code);
                }

                return "";
            } },

            {"whilee", (args)=>{
                string code = args[args.Length - 1];

                while (true)
                {
                    bool result = false;

                    for(int i = 0; i < args.Length - 1; i++)
                    {
                        string arg = args[i];

                        if(i > 0)
                        {
                            if(arg == "or")
                            {
                                bool rs = false;

                                if(args[i+1] == "not")
                                {
                                    rs = Exec(args[i+2]) != "true";
                                    i++;
                                }
                                else
                                {
                                    rs = Exec(args[i+1]) == "true";
                                }

                                result = result || rs;
                                i++;
                            }else if(arg == "and")
                            {
                                bool rs = false;

                                if(args[i+1] == "not")
                                {
                                    rs = Exec(args[i+2]) != "true";
                                    i++;
                                }
                                else
                                {
                                    rs = Exec(args[i+1]) == "true";
                                }

                                result = result && rs;
                                i++;
                            }
                        }
                        else
                        {
                            if(arg == "not")
                            {
                                result = Exec(args[i+1]) != "true";
                            }
                            else
                            {
                                result = Exec(arg) == "true";
                            }
                        }
                    }

                    if(!result) break;

                    Exec(code);
                }
                return "";
            } },

            {"for", (args)=>{
                for(int i = ToInt(args[1]); i < ToInt(args[3]); i+=ToInt(args[2]))
                {
                    SetVariable(args[0], i.ToString());
                    Exec(args[4]);
                }
                return "";
            } },

            {"foreach", (args)=>{
                string[] s = Exec(args[0]).Replace("\r", "").Split('\n');
                string ret = "";
                for(int i = 0; i < s.Length; i++)
                {
                    SetVariable("s", s[i]);
                    SetVariable("i", i.ToString());
                    string o = Exec(args[1]);
                    if(o.Length > 0) ret += o + Environment.NewLine;
                }
                return ret;
            } },

            {"foreachIf", (args)=>{
                string[] s = Exec(args[0]).Replace("\r", "").Split('\n');
                string ret = "";
                for(int i = 0; i < s.Length; i++)
                {
                    SetVariable("s", s[i]);
                    SetVariable("i", i.ToString());

                    if(Cond(Exec(args[1])))
                    {
                        string o = Exec(args[2]);
                        if(o.Length > 0) ret += o + Environment.NewLine;
                    }
                }
                return ret;
            } },

            {"func", (args)=>{
                Functions.Add(args[0], args[args.Length - 1]);
                FunctionArguments.Add(args[0], args.Skip(1).Take(args.Length - 2).ToArray());
                return "";
            } },

            {"afunc", (args)=>{
                string name = "__WESH_FUNCTION_"+GetRandomName();
                Functions.Add(name, args[args.Length - 1]);
                FunctionArguments.Add(name, args.Take(args.Length - 1).ToArray());
                return name;
            } },

            {"f", (args)=>{
                return ExecFunction(args[0], args.Skip(1).ToArray());
            } },

            {"clear", (args)=>{
                Console.Clear();
                return "";
            } },

            {"write", (args)=>{
                Console.Write(args[0]);
                return "";
            } },

            {"writeln", (args)=>{
                Console.WriteLine(args[0]);
                return "";
            } },

            {"readln", (args)=>{
                Console.Write((args.Length>0?args[0]:""));
                return Console.ReadLine();
            } },

            {"title", (args)=>{
                if(args.Length > 0) Console.Title = args[0];
                return Console.Title;
            } },

            {"wait", (args)=>{
                Thread.Sleep(ToInt(args[0]));
                return "";
            } },

            {"cd", (args)=>{
                string path = GetPath(args[0]);
                if(string.IsNullOrEmpty(path)) throw new FileNotFoundException();
                Variables["currDir"] = path;
                return "";
            } },

            {"msgbox", (args)=>{
                Dictionary<string, MessageBoxButtons> buttons = new Dictionary<string, MessageBoxButtons>()
                {
                    {"ok", MessageBoxButtons.OK},
                    {"okCancel", MessageBoxButtons.OKCancel},
                    {"yesNo", MessageBoxButtons.YesNo},
                    {"yesNoCancel", MessageBoxButtons.YesNoCancel},
                    {"abortRetryIngnore", MessageBoxButtons.AbortRetryIgnore},
                    {"retryCancel", MessageBoxButtons.RetryCancel}
                };

                Dictionary<string, MessageBoxIcon> icons = new Dictionary<string, MessageBoxIcon>()
                {
                    {"none", MessageBoxIcon.None},
                    {"info", MessageBoxIcon.Information},
                    {"error", MessageBoxIcon.Error},
                    {"warning", MessageBoxIcon.Warning},
                    {"question", MessageBoxIcon.Question}
                };

                Dictionary<DialogResult, string> result = new Dictionary<DialogResult, string>()
                {
                    {DialogResult.None, "none"},
                    {DialogResult.OK, "ok"},
                    {DialogResult.Cancel, "cancel"},
                    {DialogResult.Yes, "yes"},
                    {DialogResult.No, "no"},
                    {DialogResult.Abort, "abort"},
                    {DialogResult.Retry, "retry"},
                    {DialogResult.Ignore, "ignore"}
                };

                if(args.Length == 1) return result[MessageBox.Show(args[0])];
                if(args.Length == 2) return result[MessageBox.Show(args[0], args[1])];
                if(args.Length == 3)
                {
                    return result[MessageBox.Show(args[0], args[1], buttons[args[2]])];
                }
                if(args.Length == 4)
                {
                    return result[MessageBox.Show(args[0], args[1], buttons[args[2]], icons[args[3]])];
                }
                return "";
            } },

            {"inputbox", (args)=>{
                string val = args[2];
                InputBox(args[0], args[1], ref val);
                return val;
            } },

            {"minWeshVersion", (args)=>{
                if(double.Parse(args[0]) > Version){
                    Console.WriteLine("ERROR: This script requires wesh " + args[0] + " or later");
                    Environment.Exit(1);
                }

                return "";
            } },

            {"minRuntimeVersion", (args)=>{
                if(double.Parse(args[0]) > double.Parse(Environment.Version.Major + "." + Environment.Version.Minor)){
                    Console.WriteLine("ERROR: This script requires .NET " + args[0] + " or later");
                    Environment.Exit(1);
                }

                return "";
            } },

            {"requestAdminPriv", (args)=>{
                if(!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
                {
                    var p = new Process();
                    p.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                    p.StartInfo.Verb = "RunAs";

                    var aargs = Environment.GetCommandLineArgs();
                    if(aargs.Length > 1)
                    {
                        var bargs = aargs.Skip(1).ToArray();
                        p.StartInfo.Arguments = "\"" + String.Join("\" \"", bargs) + "\"";
                    }

                    p.Start();
                    Environment.Exit(0);
                }
                return "";
            } },

            {"rand", (args)=>{
                return rand.Next(ToInt(args[0]), ToInt(args[1])).ToString();
            } },

            {"js", (args)=>{
                return EvalJS(Regex.Replace(args[0], @"[\\]{1,}(["+GetLangEl("codeBlockStart")+GetLangEl("codeBlockEnd")+GetLangEl("cmdDelim")+"]{1})", "$1"));
            } },

            {"cs", (args)=>{
                string code = args[0];
                if(args[1] == "true") code = Regex.Replace(args[0], @"[\\]{1,}(["+GetLangEl("codeBlockStart")+GetLangEl("codeBlockEnd")+GetLangEl("cmdDelim")+"]{1})", "$1");
                string[] enPoint = args[2].Split('.');
                string[] refAsm = args.Length > 3 ? args[3].Split(',') : new string[0];
                string[] mArgs = args.Skip(4).ToArray();

                var prov = new CSharpCodeProvider();

                var cParams = new CompilerParameters();
                cParams.GenerateExecutable = false;
                cParams.GenerateInMemory = true;
                cParams.ReferencedAssemblies.AddRange(new string[]{ "mscorlib.dll", "system.dll" });
                if(refAsm.Length > 0) cParams.ReferencedAssemblies.AddRange(refAsm);

                var res = prov.CompileAssemblyFromSource(cParams, code);

                if (res.Errors.HasErrors)
                {
                    string r = "";
                    foreach(CompilerError err in res.Errors)
                    {
                        r += "Compiler error: "+err.ErrorText+"at line "+err.Line+Environment.NewLine;
                    }
                    throw new Exception(r);
                }

                var asm = res.CompiledAssembly;
                var method = asm.GetType(enPoint[0]).GetMethod(enPoint[1]);

                var ret = method.Invoke(null, mArgs);

                if(ret == null) return "";
                return ret.ToString();
            } },

            {"isNull", (args)=>{
                return BoolToString(args[0] == WeshNull);
            } },

            {"cmd.list", (args)=>{
                return CreateArray(Commands.Keys.ToArray());
            } },

            {"cmd.fromFunc", (args)=>{
                if(!Functions.ContainsKey(args[1])) return "";
                Command func = (a)=>{
                    var al = a.ToList();
                    al.Insert(0, args[1]);
                    return Commands["f"](al.ToArray());
                };

                if (Commands.ContainsKey(args[0])) Commands[args[0]] = func;
                else Commands.Add(args[0], func);
                return "";
            } },

            {"cmd.alias", (args)=>{
                Commands.Add(args[0], Commands[args[1]]);
                return "";
            } },

            {"str.contains", (args)=>{
                return args[0].Contains(args[1])?"true":"false";
            } },

            {"str.replace", (args)=>{
                if(args.Length > 3 && args[3] == "true")
                {
                    return Regex.Replace(args[0], args[1], args[2]);
                }
                else
                {
                    return args[0].Replace(args[1], args[2]);
                }
            } },

            {"str.charAt", (args)=>{
                return args[0][ToInt(args[1])].ToString();
            } },

            {"str.isMatch", (args)=>{
                return Regex.IsMatch(args[0], args[1])?"true":"false";
            } },

            {"str.equals", (args)=>{
                return BoolToString(args[0].Equals(args[1]));
            } },

            {"str.indexOf", (args)=>{
                return args[0].IndexOf(args[1]).ToString();
            } },

            {"str.getSpecChar", (args)=>{
                if(args[0] == "a") return "\a";
                if(args[0] == "b") return "\b";
                if(args[0] == "n") return "\n";
                if(args[0] == "r") return "\r";
                if(args[0] == "t") return "\t";
                if(args[0].StartsWith("x") || args[0].StartsWith("u")) return System.Convert.ToChar(ToInt(args[0].Replace("x", "").Replace("u", ""))).ToString();
                return args[0];
            } },

            {"str.concat", (args)=>{
                return String.Concat(args);
            } },

            {"str.split", (args)=>{
                return CreateArray(args[0].Split(args[1][0]));
            } },

            {"str.toBase64", (args)=>{
                return ToBase64(args[0]);
            } },

            {"str.fromBase64", (args)=>{
                return FromBase64(args[1]);
            } },

            {"str.length", (args)=>{
                return args[0].Length.ToString();
            } },

            {"fs.readFile", (args)=>{
                string p = GetPath(args[0]);
                return File.ReadAllText(p);
            } },

            {"fs.writeFile", (args)=>{
                File.WriteAllText(GetPath(args[0], true), args[1]);
                return "";
            } },

            {"fs.addToFile", (args)=>{
                File.AppendAllText(GetPath(args[0], true), args[1]);
                return "";
            } },

            {"fs.copyFile", (args)=>{
                File.Copy(GetPath(args[0]), GetPath(args[1], true));
                return "";
            } },

            {"fs.moveFile", (args)=>{
                File.Move(GetPath(args[0]), GetPath(args[1], true));
                return "";
            } },

            {"fs.deleteFile", (args)=>{
                File.Delete(GetPath(args[0]));
                return "";
            } },

            {"fs.fileExists", (args)=>{
                return BoolToString(File.Exists(GetPath(args[0])));
            } },

            {"fs.fileInfo", (args)=>{
                var f = new FileInfo(GetPath(args[0]));
                return CreateObject(new Dictionary<string, string>()
                {
                    {"name", f.Name },
                    {"fullName", f.FullName },
                    {"ext", f.Extension },
                    {"size", f.Length.ToString() },
                    {"lastWriteTime", f.LastWriteTime.ToString() },
                    {"lastAccessTime", f.LastAccessTime.ToString() },
                    {"creationTime", f.CreationTime.ToString() },
                    {"dir", f.DirectoryName },
                    {"isReadOnly", BoolToString(f.IsReadOnly) }
                });
            } },

            {"fs.createDir", (args)=>{
                Directory.CreateDirectory(GetPath(args[0], true));
                return "";
            } },

            {"fs.moveDir", (args)=>{
                Directory.Move(GetPath(args[0]), GetPath(args[1], true));
                return "";
            } },

            {"fs.deleteDir", (args)=>{
                Directory.Delete(GetPath(args[0]), true);
                return "";
            } },

            {"fs.dirInfo", (args)=>{
                var d = new DirectoryInfo(GetPath(args[0]));
                return CreateObject(new Dictionary<string, string>()
                {
                    {"name", d.Name },
                    {"fullName", d.FullName },
                    {"lastWriteTime", d.LastWriteTime.ToString() },
                    {"lastAccessTime", d.LastAccessTime.ToString() },
                    {"creationTime", d.CreationTime.ToString() },
                    {"parentDir", d.Parent.FullName },
                    {"rootDir", d.Root.FullName }
                });
            } },

            {"fs.listDir", (args)=>{
                string p = GetPath(args.Length > 0?args[0]:".");
                string mode = args.Length > 1?args[1]:"";
                List<string> list = new List<string>();
                if(mode != "files") list.AddRange(Directory.GetDirectories(p));
                if(mode != "dirs") list.AddRange(Directory.GetFiles(p));
                if(!(args.Length > 2 && args[2] == "true"))
                {
                    for(int i = 0; i < list.Count; i++)
                    {
                        list[i] = Path.GetFileName(list[i]);
                    }
                }
                return CreateArray(list.ToArray());
            } },

            {"reg.createKey", (args)=>{
                var key = GetRegistryKey(args[0], true);
                key.CreateSubKey(args[1]);
                key.Close();
                return "";
            } },

            {"reg.write", (args)=>{
                var key = GetRegistryKey(args[0], true);
                if(args.Length > 3)
                {
                    string valType = args[3][0].ToString().ToUpper() + args[3].Substring(1, args[3].Length - 1);
                    key.SetValue(args[1], args[2], (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), valType));
                }
                else
                {
                    key.SetValue(args[1], args[2]);
                }
                key.Close();
                return "";
            } },

            {"reg.read", (args)=>{
                var key = GetRegistryKey(args[0], false);
                var val = key.GetValue(args[1]);
                key.Close();

                string s = (val is bool)?BoolToString((bool)val):val.ToString();
                return s;
            } },

            {"reg.delete", (args)=>{
                var key = GetRegistryKey(args[0], true);
                key.DeleteValue(args[1]);
                key.Close();
                return "";
            } },

            {"reg.deleteKey", (args)=>{
                var key = GetRegistryKey(args[0], true);
                key.DeleteSubKey(args[1]);
                key.Close();
                return "";
            } },

            {"reg.list", (args)=>{
                List<string> l = new List<string>();

                var key = GetRegistryKey(args[0], false);
                l.AddRange(key.GetSubKeyNames());
                l.AddRange(key.GetValueNames());
                key.Close();

                return String.Join(Environment.NewLine, l);
            } },

            {"obj.create", (args)=>{
                string name = CreateObject(new Dictionary<string, string>());
                Variables.Add(args[0], name);
                return "";
            } },

            {"obj.save", (args)=>{
                string text = args[0] + Environment.NewLine;
                foreach(KeyValuePair<string, string> kvp in UserObjects[args[0]])
                {
                    text += kvp.Key + "=" + kvp.Value + Environment.NewLine;
                }
                File.WriteAllText(GetPath(args[1], true), text);
                return "";
            } },

            {"obj.load", (args)=>{
                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach(string line in File.ReadAllLines(GetPath(args[0])))
                {
                    if(line.Trim().Length == 0 || !line.Contains('=')) continue;
                    var sp = line.Split(new char[]{'='}, 2);
                    dict.Add(sp[0], sp[1]);
                }
                return CreateObject(dict);
            } },

            {"obj.createN", (args)=>{
                UserObjects.Add(args[0], new Dictionary<string, string>());
                return "";
            } },

            {"obj.getKeys", (args)=>{
                return String.Join(Environment.NewLine, UserObjects[args[0]].Keys);
            } },

            {"obj.getData", (args)=>{
                string ret = "";
                foreach(KeyValuePair<string, string> pair in UserObjects[args[0]])
                {
                    ret += pair.Key + ": " + pair.Value + Environment.NewLine;
                }
                return ret;
            } },

            {"obj.get", (args)=>{
                if(UserObjects.ContainsKey(args[0]) && UserObjects[args[0]].ContainsKey(args[1])) return UserObjects[args[0]][args[1]];
                return "";
            } },

            {"obj.set", (args)=>{
                if(UserObjects.ContainsKey(args[0]))
                {
                    if(UserObjects[args[0]].ContainsKey(args[1])) UserObjects[args[0]][args[1]] = args[2];
                    else UserObjects[args[0]].Add(args[1], args[2]);
                }
                return "";
            } },

            {"obj.foreach", (args)=>{
                if(!UserObjects.ContainsKey(args[0])) return "";
                string ret = "";
                foreach(KeyValuePair<string, string> kvp in UserObjects[args[0]])
                {
                    SetVariable("k", kvp.Key);
                    SetVariable("v", kvp.Value);

                    string o = Exec(args[1]);
                    if(o.Length > 0) ret += o + Environment.NewLine;
                }
                return ret;
            } },

            {"class", (args)=>{
                Dictionary<string, string> cls = new Dictionary<string, string>();

                foreach(string ce in args[1].Replace("\\;", "$DTC").Split(';'))
                {
                    string ces = ce.Trim();

                    if (ces.Split(' ')[0].Contains("("))
                    {
                        string[] cla = ces.Split(')');
                        cla[0] = cla[0].Trim();
                        string val = String.Join(")", cla.Skip(1)).Trim().Replace("$DTC", "\\;");
                        val = "&afunc " + val;
                        cla[0] = cla[0].Replace("(", "");
                        cls.Add(cla[0].Trim(), ParseArg(val));
                    }
                    else
                    {
                        string[] cla = ce.Trim().Split('=');
                        if(cla.Length < 2) continue;
                        cla[0] = cla[0].Trim();
                        string val = ParseQuotes(String.Join("=", cla.Skip(1)).Trim().Replace("$DTC", "\\;"));
                        cls.Add(cla[0], ParseArg(val));
                    }
                }

                Classes.Add(args[0], cls);

                return "";
            } },

            {"new", (args)=>{
                string name = "__WESH_OBJECT_"+GetRandomName();

                Dictionary<string, string> obj = new Dictionary<string, string>();

                UserObjects.Add(name, obj);

                foreach(KeyValuePair<string, string> kvp in Classes[args[0]])
                {
                    string val = kvp.Value;
                    if (Functions.ContainsKey(kvp.Value))
                    {
                        string funcCode = Functions[kvp.Value];
                        string newName = "__WESH_FUNCTION_"+GetRandomName();
                        funcCode = funcCode.Replace(Lang["thisKeyword"] + ".", name + ".");
                        Functions.Add(newName, funcCode);
                        val = newName;
                    }
                    obj.Add(kvp.Key, val);
                }

                if (obj.ContainsKey(Lang["constructorKeyword"]))
                {
                    ExecFunction(obj[Lang["constructorKeyword"]], args.Skip(1).ToArray());
                }

                obj.Add("_class", args[0]);

                return name;
            } },

            {"arr.create", (args)=>{
                return CreateArray(args);
            } },

            {"arr.get", (args)=>{
                return UserObjects[args[0]][args[1]];
            } },

            {"arr.set", (args)=>{
                UserObjects[args[0]][args[1]] = args[2];
                return "";
            } },

            {"arr.push", (args)=>{
                var arr = UserObjects[args[0]];
                var len = ToInt(arr["length"]);

                len++;
                arr.Add((len-1).ToString(), args[1]);
                arr["length"] = len.ToString();

                return len.ToString();
            } },

            {"arr.pop", (args)=>{
                var arr = UserObjects[args[0]];
                var len = ToInt(arr["length"]);

                len--;
                var e = arr[len.ToString()];
                arr.Remove(len.ToString());
                arr["length"] = len.ToString();

                return e;
            } },

            {"arr.shift", (args)=>{
                var arr = UserObjects[args[0]];
                var fel = arr["0"];
                var len = ToInt(arr["length"]);

                var vals = arr.ToDictionary(e=>e.Key, e=>e.Value);
                arr.Clear();

                for(int i = 1; i < len; i++)
                {
                    arr.Add((i-1).ToString(), vals[i.ToString()]);
                }
                arr.Add("length", (len-1).ToString());

                return fel;
            } },

            {"arr.insert", (args)=>{
                string[] array = WeshArrayToArray(args[0]);
                var list = array.ToList();
                list.Insert(ToInt(args[1]), args[2]);
                ArrayToWeshArray(args[0], list.ToArray());

                return args[2];
            } },

            {"arr.foreach", (args)=>{
                string r = "";
                int i = 0;
                foreach(string s in WeshArrayToArray(args[0]))
                {
                    SetVariable("k", i.ToString());
                    SetVariable("v", s);
                    r += Exec(args[1]) + Environment.NewLine;
                    i++;
                }
                return r;
            } },

            {"arr.join", (args)=>{
                return String.Join(args[1], WeshArrayToArray(args[0]));
            } },

            {"module.install", (args)=>{
                string addr = Variables["modulesUrl"].Replace("MODULE_NAME", args[0]);
                new WebClient().DownloadFile(addr, Variables["modulesDir"] + "\\" + args[0] + ".weshm");
                return "";
            } },

            {"module.uninstall", (args)=>{
                File.Delete(Variables["modulesDir"] + "\\" + args[0] + ".weshm");
                return "";
            } },

            {"hotkey.set", (args)=>{
                HotKey.SetHotKey(args[0], StringToBool(args[1]), StringToBool(args[2]), StringToBool(args[3]), false, () =>
                {
                    Exec(args[4]);
                });
                return "";
            } },

            {"hotkey.setKeyHandler", (args)=>{
                HotKey.SetKeyHandler(false, (k)=>{
                    SetVariable("key", k);

                    Exec(args[0]);
                });
                return "";
            } },

            {"hotkey.listen", (args)=>{
                Application.Run();
                return "";
            } },

            {"http.request", (args)=>{
                string m = args[0].ToLower();
                string r = "";

                switch (m)
                {
                    case "get": r = GetRequest(args[1]); break;
                    case "post": r = PostRequest(args[1], args[2]); break;
                }

                return r;
            } },

            {"http.downloadFile", (args)=>{
                new WebClient().DownloadFile(args[0], args[1]);
                return "";
            } },

            {"http.fileServer", (args)=>{
                ServeFolder(GetPath(args[0]), ToInt(args[1]), false);
                return "";
            } },

            {"http.server", (args)=>{
                HttpListener server = new HttpListener();
                server.Prefixes.Add("http://0.0.0.0:"+args[0]+"/");
                server.Start();

                while (true)
                {
                    var ctx = server.GetContext();
                    var req = ctx.Request;
                    var res = ctx.Response;
                    var output = res.OutputStream;

                    SetVariable("url", req.RawUrl);
                    SetVariable("ip", req.RemoteEndPoint.ToString());
                    SetVariable("method", req.HttpMethod);

                    Dictionary<string, string> headers = new Dictionary<string, string>();

                    foreach(string key in req.Headers)
                    {
                        headers.Add(key, req.Headers[key]);
                    }

                    SetVariable("headers", CreateObject(headers));

                    Exec(args[1]);

                    res.StatusCode = ToInt(GetVariable("statusCode"));
                    res.ContentType = GetVariable("contentType");

                    byte[] buffer = Encoding.UTF8.GetBytes(GetVariable("content"));

                    res.ContentLength64 = buffer.Length;
                    output.Write(buffer, 0, buffer.Length);
                    output.Flush();
                    output.Close();
                }
            } },

            {"http.parseUrl", (args)=>{
                Dictionary<string, string> uo = new Dictionary<string, string>();

                uo.Add("fullUrl", args[0]);
                uo.Add("path", args[0].Split('?')[0]);

                Dictionary<string, string> q = new Dictionary<string, string>();

                if (args[0].Contains("?"))
                {
                    foreach(string qs in args[0].Split('?')[1].Split('&'))
                    {
                        string[] arr = qs.Split('=');
                        if(arr.Length == 2) q.Add(arr[0], arr[1]);
                    }

                    uo.Add("query", CreateObject(q));
                }

                return CreateObject(uo);
            } },

            {"proc.list", (args)=>{
                var lst = new List<string>();

                if(args.Length > 0)
                {
                    foreach(var p in Process.GetProcessesByName(args[0]))
                    {
                        lst.Add(p.Id.ToString());
                    }
                }
                else
                {
                    foreach(var p in Process.GetProcesses())
                    {
                        lst.Add(p.Id.ToString());
                    }
                }

                return CreateArray(lst.ToArray());
            } },

            {"proc.run", (args)=>{
                var p = new Process();
                if(args.Length > 2 && args[2] == "true")
                {
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.UseShellExecute = false;
                }
                p.StartInfo.FileName = args[0];
                if(args.Length > 1) p.StartInfo.Arguments = args[1];
                if(args.Length > 2)
                {
                    if(args[3] == "normal") p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    if(args[3] == "hidden")
                    {
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    }
                    if(args[3] == "minimized") p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    if(args[3] == "maximized") p.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                }
                if(args.Length > 4) p.StartInfo.Verb = args[4];
                p.Start();
                if(args.Length > 2 && args[2] == "true") p.WaitForExit();
                return (args.Length > 2 && args[2] == "true")?p.StandardOutput.ReadToEnd():p.Id.ToString();
            } },

            {"proc.kill", (args)=>{
                Process.GetProcessById(ToInt(args[0])).Kill();
                return "";
            } },

            {"proc.getInfo", (args)=>{
                string handleName = "__WESH_WIN32_HANDLE_"+GetRandomName();

                var p = Process.GetProcessById(ToInt(args[0]));

                Pointers.Add(handleName, p.MainWindowHandle);

                return CreateObject(new Dictionary<string, string>
                {
                    {"name", p.ProcessName},
                    {"id", p.Id.ToString()},
                    {"fileName", p.MainModule.FileName},
                    {"args", p.StartInfo.Arguments},
                    {"mainWindowHandle", handleName}
                });
            } },

            {"proc.activate", (args)=>{
                var ps = Process.GetProcessById(ToInt(args[0]));
                ShowWindow(ps.MainWindowHandle, 1);
                SetForegroundWindow(ps.MainWindowHandle);
                return "";
            } },

            {"proc.isRunning", (args)=>{
                return BoolToString(Process.GetProcessById(ToInt(args[0])).HasExited);
            } },

            {"kb.sendKey", (args)=>{
                byte vk = System.Convert.ToByte(Enum.Parse(typeof(Keys), args[0]));
                keybd_event(vk, 0, 0, 0);
                return "";
            } },

            {"kb.sendKeys", (args)=>{
                SendKeys.SendWait(args[0]);
                return "";
            } },

            {"mouse.click", (args)=>{
                string button = (args.Length>0?args[0]:"left");
                int delay = (args.Length>1?ToInt(args[1]):10);

                if(button == "left")
                {
                    mouse_event(0x2, 0, 0, 0, 0);
                    if(delay != 0)
                    {
                        Thread.Sleep(delay);
                        mouse_event(0x4, 0, 0, 0, 0);
                    }
                }else if(button == "right")
                {
                    mouse_event(0x8, 0, 0, 0, 0);
                    if(delay != 0)
                    {
                        Thread.Sleep(delay);
                        mouse_event(0x10, 0, 0, 0, 0);
                    }
                }else if(button == "middle")
                {
                    mouse_event(0x20, 0, 0, 0, 0);
                    if(delay != 0)
                    {
                        Thread.Sleep(delay);
                        mouse_event(0x40, 0, 0, 0, 0);
                    }
                }

                return "";
            } },

            {"mouse.setPos", (args)=>{
                Cursor.Position = new Point(ToInt(args[0]), ToInt(args[1]));
                return "";
            } },

            {"mouse.getPos", (args)=>{
                string name = GetRandomName();

                UserObjects.Add(name, new Dictionary<string, string>()
                {
                    {"x", Cursor.Position.X.ToString() },
                    {"y", Cursor.Position.Y.ToString() }
                });

                return name;
            } },

            {"lang.get", (args)=>{
                return Lang[args[0]];
            } },

            {"lang.set", (args)=>{
                Lang[args[0]] = args[1];
                return "";
            } },

            {"lang.list", (args)=>{
                string r = "";
                foreach(KeyValuePair<string, string> kvp in Lang)
                {
                    r += kvp.Key+": "+kvp.Value+Environment.NewLine;
                }
                return r;
            } },

            {"gui.createForm", (args)=>{
                string name = "__WESH_GUI_FORM_" + GetRandomName();
                var form = new Form();

                form.Text = args[0];
                form.Width = ToInt(args[1]);
                form.Height = ToInt(args[2]);
                if(args.Length > 3 && args[3] == "true")
                {
                    form.FormBorderStyle = FormBorderStyle.FixedSingle;
                    form.MaximizeBox = false;
                }

                Controls.Add(name, form);

                return name;
            } },

            {"gui.showForm", (args)=>{
                Application.Run((Form)Controls[args[0]]);
                return "";
            } },

            {"gui.addElem", (args)=>{
                Controls[args[0]].Controls.Add(Controls[args[1]]);
                return "";
            } },

            {"gui.createElem", (args)=>{
                string name = "__WESH_GUI_ELEM_" + GetRandomName();
                Control el;

                if(args[0] == "panel") el = new Panel();
                else if(args[0] == "label") el = new Label();
                else if(args[0] == "button") el = new Button();
                else if(args[0] == "textbox")
                {
                    el = new TextBox();
                    ((TextBox)el).ScrollBars = ScrollBars.Both;
                }
                else if(args[0] == "checkbox") el = new CheckBox();
                else if(args[0] == "combobox") el = new ComboBox();
                else return "";

                el.Location = new Point(ToInt(args[1]),ToInt(args[2]));
                el.Size = new Size(ToInt(args[3]), ToInt(args[4]));

                Controls.Add(name, el);
                return name;
            } },

            {"gui.getElemProp", (args)=>{
                Control el = Controls[args[0]];

                if(args[1] == "text") return el.Text;
                else if(args[1] == "x") return el.Location.X.ToString();
                else if(args[1] == "y") return el.Location.Y.ToString();
                else if(args[1] == "width") return el.Size.Width.ToString();
                else if(args[1] == "height") return el.Size.Height.ToString();
                else if(args[1] == "checked") return BoolToString(((CheckBox)el).Checked);
                else if(args[1] == "multiline") return BoolToString(((TextBox)el).Multiline);
                else if(args[1] == "color") return el.ForeColor.R+","+el.ForeColor.G+","+el.ForeColor.B;
                else if(args[1] == "bgcolor") return el.BackColor.R+","+el.BackColor.G+","+el.BackColor.B;
                else if(args[1] == "selindex") return ((ComboBox)el).SelectedIndex.ToString();

                return "";
            } },

            {"gui.setElemProp", (args)=>{
                Control el = Controls[args[0]];

                if(args[1] == "text") el.Text = args[2];
                else if(args[1] == "x") el.Location = new Point(ToInt(args[2]), el.Location.Y);
                else if(args[1] == "y") el.Location = new Point(el.Location.X, ToInt(args[2]));
                else if(args[1] == "width") el.Size = new Size(ToInt(args[2]), el.Size.Height);
                else if(args[1] == "height") el.Size = new Size(el.Size.Width, ToInt(args[2]));
                else if(args[1] == "checked") ((CheckBox)el).Checked = StringToBool(args[2]);
                else if(args[1] == "multiline") ((TextBox)el).Multiline = StringToBool(args[2]);
                else if(args[1] == "selindex") ((ComboBox)el).SelectedIndex = ToInt(args[2]);
                else if(args[1] == "color")
                {
                    string[] color = args[2].Split(',');
                    el.ForeColor = Color.FromArgb(ToInt(color[0]), ToInt(color[1]), ToInt(color[2]));
                }
                else if(args[1] == "bgcolor")
                {
                    string[] color = args[2].Split(',');
                    el.BackColor = Color.FromArgb(ToInt(color[0]), ToInt(color[1]), ToInt(color[2]));
                }

                return "";
            } },

            {"gui.setFont", (args)=>{
                Control el = Controls[args[0]];
                el.Font = new Font(args[1], ToInt(args[2]));
                return "";
            } },

            {"gui.addEvent", (args)=>{
                Control el = Controls[args[0]];

                if(args[1] == "click") el.Click += (o,e)=>Exec(args[2]);
                if(args[1] == "keydown") el.KeyDown += (o,e)=>{
                    SetVariable("alt", BoolToString(e.Alt));
                    SetVariable("ctrl", BoolToString(e.Control));
                    SetVariable("shift", BoolToString(e.Shift));
                    SetVariable("key", e.KeyCode.ToString());
                    Exec(args[2]);
                };

                return "";
            } },

            {"gui.getElemItem", (args)=>{
                ComboBox el = (ComboBox)Controls[args[0]];
                return el.Items[ToInt(args[1])].ToString();
            } },

            {"gui.getElemItems", (args)=>{
                ComboBox el = (ComboBox)Controls[args[0]];
                string name = "__WESH_OBJECT_" + GetRandomName();
                ArrayToWeshArray(name, el.Items.Cast<string>().ToArray());
                return name;
            } },

            {"gui.addElemItem", (args)=>{
                ComboBox el = (ComboBox)Controls[args[0]];
                return el.Items.Add(args[1]).ToString();
            } },

            {"gui.deleteElemItem", (args)=>{
                ComboBox el = (ComboBox)Controls[args[0]];
                el.Items.RemoveAt(ToInt(args[1]));
                return "";
            } },

            {"gui.openFileDialog", (args)=>{
                var ofd = new OpenFileDialog();
                ofd.Filter = args[0];
                if(args.Length > 1) ofd.Title = args[1];
                if(args.Length > 2) ofd.InitialDirectory = GetPath(args[2]);
                if(ofd.ShowDialog() == DialogResult.OK) return ofd.FileName;
                else return "";
            } },

            {"gui.saveFileDialog", (args)=>{
                var sfd = new SaveFileDialog();
                sfd.Filter = args[0];
                if(args.Length > 1) sfd.Title = args[1];
                if(args.Length > 2) sfd.InitialDirectory = GetPath(args[2]);
                if(sfd.ShowDialog() == DialogResult.OK) return sfd.FileName;
                else return "";
            } },

            {"gui.getElemHandle", (args)=>{
                string name = "__WESH_WIN32_HANDLE_"+GetRandomName();
                Pointers.Add(name, Controls[args[0]].Handle);
                return name;
            } },

            {"env.get", (args)=>{
                return Environment.GetEnvironmentVariable(args[0], (args.Length>1&&args[1]=="true")?EnvironmentVariableTarget.Machine:EnvironmentVariableTarget.User);
            } },

            {"env.set", (args)=>{
                Environment.SetEnvironmentVariable(args[0], args[1], (args.Length>2&&args[2]=="true")?EnvironmentVariableTarget.Machine:EnvironmentVariableTarget.User);
                return "";
            } },

            {"win32.findWindow", (args)=>{
                string name = "__WESH_WIN32_HANDLE_"+GetRandomName();
                IntPtr win = FindWindow(WeshNullToNull(args[0]), WeshNullToNull(args[1]));
                if(win == IntPtr.Zero) return WeshNull;
                Pointers.Add(name, win);
                return name;
            } },

            {"win32.findChildWindow", (args)=>{
                string name = "__WESH_WIN32_HANDLE_"+GetRandomName();
                IntPtr win = FindWindowEx(Pointers[args[0]], (IntPtr)0, WeshNullToNull(args[1]), WeshNullToNull(args[2]));
                if(win == IntPtr.Zero) return WeshNull;
                Pointers.Add(name, win);
                return name;
            } },

            {"win32.getRootWindow", (args)=>{
                string name = "__WESH_WIN32_HANDLE_"+GetRandomName();
                Pointers.Add(name, GetDesktopWindow());
                return name;
            } },

            {"win32.windowSelector", (args)=>{
                IntPtr root = GetDesktopWindow();
                IntPtr win = args.Length > 1?Pointers[args[1]]:root;

                string data = args[0].Replace("\\>", "$GT");
                string[] selectors = data.Split('>');

                foreach(string selector in selectors)
                {
                    string sel = selector.Replace("$GT", ">");
                    win = FindWindowEx(win, IntPtr.Zero, sel[0] == '.'?sel.Substring(1):null, sel[0] != '.'?sel:null);
                    if(win == IntPtr.Zero) break;
                }

                if(win == root) return WeshNull;

                string name = "__WESH_WIN32_HANDLE_"+GetRandomName();
                Pointers.Add(name, win);
                return name;
            } },

            {"win32.enumWindows", (args)=>{
                string r = "";
                EnumWindows((handle, lParam)=>{
                    string name = "__WESH_WIN32_HANDLE_"+GetRandomName();
                    Pointers.Add(name, handle);
                    SetVariable("wnd", name);
                    r += Exec(args[0])+Environment.NewLine;
                    return true;
                }, IntPtr.Zero);
                return r;
            } },

            {"win32.getWindowText", (args)=>{
                IntPtr h = Pointers[args[0]];
                int len = SendMessage(h, 0xE, 0, 0)+1;
                StringBuilder buff = new StringBuilder(len);
                SendMessage(h, 0xD, len, buff);
                return buff.ToString();
            } },

            {"win32.setWindowText", (args)=>{
                StringBuilder buff = new StringBuilder(args[1]);
                SendMessage(Pointers[args[0]], 0xC, 0, buff);
                return "";
            } },

            {"win32.getWindowPos", (args)=>{
                W32Rect r = new W32Rect();
                GetWindowRect(Pointers[args[0]], ref r);
                return CreateObject(new Dictionary<string, string>(){
                    {"x", r.Left.ToString() },
                    {"y", r.Top.ToString() },
                    {"width", (r.Right-r.Left).ToString() },
                    {"height", (r.Bottom-r.Top).ToString() }
                });
            } },

            {"win32.setWindowPos", (args)=>{
                SetWindowPos(Pointers[args[0]], IntPtr.Zero, ToInt(args[1]), ToInt(args[2]), args.Length>3?ToInt(args[3]):0, args.Length>4?ToInt(args[3]):0, args.Length>4?0:0x01);
                return "";
            } },

            {"win32.sendMessage", (args)=>{
                int wpi = 0;
                if(int.TryParse(args[3], out wpi)) return SendMessage(Pointers[args[0]], ToInt(args[1]), ToInt(args[2]), wpi).ToString();
                return SendMessage(Pointers[args[0]], ToInt(args[1]), ToInt(args[2]), new StringBuilder(args[3])).ToString();
            } },

            {"win32.postMessage", (args)=>{
                int wpi = 0;
                if(int.TryParse(args[3], out wpi)) return PostMessage(Pointers[args[0]], ToInt(args[1]), ToInt(args[2]), wpi).ToString();
                return PostMessage(Pointers[args[0]], ToInt(args[1]), ToInt(args[2]), new StringBuilder(args[3])).ToString();
            } },

            {"win32.getHandleValue", (args)=>{
                return Pointers[args[0]].ToInt64().ToString();
            } },

            {"win32.getHandle", (args)=>{
                string name = "__WESH_WIN32_HANDLE_"+GetRandomName();
                Pointers.Add(name, (IntPtr)ToInt(args[0]));
                return name;
            } },

            {"win32.getForegroundWindow", (args)=>{
                string name = "__WESH_WIN32_HANDLE_"+GetRandomName();
                Pointers.Add(name, GetForegroundWindow());
                return name;
            } },

            {"win32.callDll", (args)=>{
                var funcArgs = new List<Argument>();

                if(args.Length > 3){
                    foreach(string ar_ in args.Skip(3)){
                        string ar = ar_;

                        if(ar == WeshNull) ar = "System.IntPtr:0";

                        if(Pointers.ContainsKey(ar)) ar = "System.IntPtr:"+ar;

                        string[] spl = ar.Split(':');

                        if(spl.Length < 2){
                            throw new Exception("Invalid format");
                        }

                        spl[0] = ShortTypeNameToFull(spl[0]);

                        bool isWeshPointer = (spl[0] == "System.IntPtr" && Pointers.ContainsKey(spl[1]));

                        try{
                            funcArgs.Add(
                                new Argument(
                                    spl[0],
                                    spl[0]=="System.IntPtr"?(isWeshPointer?Pointers[spl[1]]:new IntPtr(int.Parse(spl[1]))):System.Convert.ChangeType(String.Join(":", spl.Skip(1)), Type.GetType(spl[0]))
                                )
                            );
                        }catch(FormatException){
                            throw new Exception("Invalid format");
                        }
                    }
                }

                try{
                    string type = ShortTypeNameToFull(args[2]);
                    var r = DllCaller.CallFunction(args[0], args[1], type, funcArgs);
                    if(r == null) return WeshNull;
                    if(type == "System.IntPtr")
                    {
                        string name = "__WESH_POINTER_"+GetRandomName();
                        Pointers.Add(name, (IntPtr)r);
                        return name;
                    }
                    return r.ToString();
                }catch(ArgumentNullException){
                    throw new Exception("Invalid DLL or function name");
                }
            } },

            {"win32.dllImport", (args)=>{
                string eFuncName = args[0];
                string dllName = args[1];
                string funcName = args[2];
                string type = ShortTypeNameToFull(args[3]);
                string[] argTypes = args.Skip(4).Select(t=>ShortTypeNameToFull(t)).ToArray();

                Commands.Add(eFuncName==WeshNull?dllName+"/"+funcName:eFuncName, (ca)=>{
                    List<Argument> argList = new List<Argument>();

                    for(int i = 0; i < argTypes.Length; i++)
                    {
                        string argType = argTypes[i];
                        if(argType == "System.IntPtr" && ca[i] == WeshNull) ca[i] = "0";
                        bool isWeshPointer = (argType=="System.IntPtr"&&Pointers.ContainsKey(ca[i]));

                        try{
                            argList.Add(
                                new Argument(
                                    argType,
                                    argType=="System.IntPtr"?(isWeshPointer?Pointers[ca[i]]:new IntPtr(int.Parse(ca[i]))):System.Convert.ChangeType(ca[i], Type.GetType(argType))
                                )
                            );
                        }catch(FormatException){
                            throw new Exception("Invalid format");
                        }
                    }

                    object r = null;
                    try{
                        object _r = DllCaller.CallFunction(dllName, funcName, type=="void"?"System.Int32":type, argList);
                        if(type != "void") r = _r;
                        if(type == "System.IntPtr")
                        {
                            string name = "__WESH_POINTER_"+GetRandomName();
                            Pointers.Add(name, (IntPtr)_r);
                            return name;
                        }
                    }catch(ArgumentNullException){
                        r = "Invalid DLL or function name";
                    }catch(Exception e){
                        r = e.Message;
                    }

                    if(r == null) return WeshNull;
                    return r.ToString();
                });

                return "";
            } },

            {"clipboard.get", (args)=>{
                return Clipboard.GetText();
            } },

            {"clipboard.set", (args)=>{
                Clipboard.SetText(args[0]);
                return "";
            } },

            {"eobj.createCOMObject", (args)=>{
                string name = "__WESH_EXT_OBJECT_"+GetRandomName();

                Type t = Type.GetTypeFromProgID(args[0]);
                object o = Activator.CreateInstance(t);

                ExtObjectTypes.Add(name, t);
                ExtObjects.Add(name, o);
                return name;
            } },

            {"eobj.loadNETAssembly", (args)=>{
                LoadedAssemblies.Add(Assembly.LoadWithPartialName(args[0]));
                return "";
            } },

            {"eobj.createNETObject", (args)=>{
                string name = "__WESH_EXT_OBJECT_"+GetRandomName();

                var pa = args.Skip(1);
                List<object> ar = new List<object>();
                foreach(string p in pa)
                {
                    int v = 0;
                    if(int.TryParse(p, out v)) ar.Add(v);
                    else if(p == "true" || p == "false") ar.Add(StringToBool(p));
                    else ar.Add(p);
                }

                if(!IsDefaultAssembliesLoaded){
                    LoadedAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());
                    IsDefaultAssembliesLoaded = true;
                }

                Type t = null;

                foreach(Assembly asm in LoadedAssemblies){
                    t = asm.GetType(args[0]);
                    if(t != null) break;
                }

                object o = Activator.CreateInstance(t, ar.ToArray());

                ExtObjectTypes.Add(name, t);
                ExtObjects.Add(name, o);
                return name;
            } },

            {"eobj.getNETClass", (args)=>{
                string name = "__WESH_EXT_OBJECT_"+GetRandomName();

                if(!IsDefaultAssembliesLoaded){
                    LoadedAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());
                    IsDefaultAssembliesLoaded = true;
                }

                Type t = null;

                foreach(Assembly asm in LoadedAssemblies){
                    t = asm.GetType(args[0]);
                    if(t != null) break;
                }

                ExtObjectTypes.Add(name, t);
                ExtObjects.Add(name, null);
                return name;
            } },

            {"eobj.getProp", (args)=>{
                var m = GetProp(ExtObjectTypes[args[0]], ExtObjects[args[0]], args[1]);

                var mType = m.GetType();
                if(mType.FullName == "System.String") return m.ToString();
                else if(mType.FullName.StartsWith("System.Int")) return m.ToString();
                else if(mType.FullName == "System.Boolean") return BoolToString((bool)m);
                else
                {
                    string name = "__WESH_EXT_OBJECT_"+GetRandomName();

                    ExtObjectTypes.Add(name, mType);
                    ExtObjects.Add(name, m);
                    return name;
                }
            } },

            {"eobj.setProp", (args)=>{
                int val = 0;
                if(int.TryParse(args[2], out val))
                {
                    SetProp(ExtObjectTypes[args[0]], ExtObjects[args[0]], args[1], val);
                }else if(args[2] == "true" || args[2] == "false")
                {
                    SetProp(ExtObjectTypes[args[0]], ExtObjects[args[0]], args[1], StringToBool(args[2]));
                }
                else
                {
                    SetProp(ExtObjectTypes[args[0]], ExtObjects[args[0]], args[1], args[2]);
                }

                return "";
            } },

            {"eobj.invokeMethod", (args)=>{
                var pa = args.Skip(2);
                List<object> ar = new List<object>();

                foreach(string p in pa)
                {
                    int v = 0;
                    double d = 0.0;
                    float f = 0.0f;
                    if(int.TryParse(p, out v)) ar.Add(v);
                    else if(double.TryParse(p, out d)) ar.Add(d);
                    else if(p.EndsWith("f") && float.TryParse(p.Replace("f", ""), out f)) ar.Add(f);
                    else if(p == "true" || p == "false") ar.Add(StringToBool(p));
                    else if(ExtObjects.ContainsKey(p)) ar.Add(ExtObjects[p]);
                    else ar.Add(p);
                }

                var m = InvokeMethod(ExtObjectTypes[args[0]], ExtObjects[args[0]], args[1], ar.ToArray());

                if(m == null) return WeshNull;

                var mType = m.GetType();
                if(mType.FullName == "System.String") return m.ToString();
                else if(mType.FullName.StartsWith("System.Int") || mType.FullName == "System.Single" || mType.FullName == "System.Double") return m.ToString();
                else if(mType.FullName == "System.Boolean") return BoolToString((bool)m);
                else
                {
                    string name = "__WESH_EXT_OBJECT_"+GetRandomName();

                    ExtObjectTypes.Add(name, mType);
                    ExtObjects.Add(name, m);
                    return name;
                }
            } },

            {"zip.create", (args)=>{
                ZipFile.CreateFromDirectory(GetPath(args[0]), GetPath(args[1]));
                return "";
            } },

            {"zip.extract", (args)=>{
                ZipFile.ExtractToDirectory(GetPath(args[0]), GetPath(args[1]));
                return "";
            } },

            {"barr.readFromFile", (args)=>{
                byte[] fd = File.ReadAllBytes(GetPath(args[0]));
                List<string> wa = new List<string>();

                foreach(byte b in fd)
                {
                    wa.Add(b.ToString());
                }

                return CreateArray(wa.ToArray());
            } },

            {"barr.writeToFile", (args)=>{
                List<byte> bs = new List<byte>();
                string[] ss = WeshArrayToArray(args[0]);
                foreach(string s in ss)
                {
                    bs.Add(byte.Parse(s));
                }
                File.WriteAllBytes(GetPath(args[1], true), bs.ToArray());
                return "";
            } },

            {"barr.fromBase64String", (args)=>{
                List<string> ss = new List<string>();
                byte[] bs = System.Convert.FromBase64String(args[0]);
                foreach(byte b in bs)
                {
                    ss.Add(b.ToString());
                }
                return CreateArray(ss.ToArray());
            } },

            {"barr.toBase64String", (args)=>{
                List<byte> bs = new List<byte>();
                string[] ss = WeshArrayToArray(args[0]);
                foreach(string s in ss)
                {
                    bs.Add(byte.Parse(s));
                }
                return System.Convert.ToBase64String(bs.ToArray());
            } },

            {"ptr.alloc", (args)=>{
                string name = "__WESH_POINTER_"+GetRandomName();
                Pointers.Add(name, Marshal.AllocHGlobal(ToInt(args[0]) == 0?Marshal.SizeOf(Type.GetType(ShortTypeNameToFull(args[0]))):ToInt(args[0])));
                return name;
            } },

            {"ptr.free", (args)=>{
                Marshal.FreeHGlobal(Pointers[args[0]]);
                return "";
            } },

            {"ptr.read", (args)=>{
                string type = ShortTypeNameToFull(args[1]);
                IntPtr ptr = Pointers[args[0]];
                int off = (args.Length>2?ToInt(args[2]):0);

                if(type == "System.Byte")
                {
                    return Marshal.ReadByte(ptr, off).ToString();
                }
                else if(type == "System.Int16")
                {
                    return Marshal.ReadInt16(ptr, off).ToString();
                }
                else if(type == "System.Int32")
                {
                    return Marshal.ReadInt32(ptr, off).ToString();
                }
                else if(type == "System.Int64")
                {
                    return Marshal.ReadInt64(ptr, off).ToString();
                }
                else if(type == "System.Char")
                {
                    return ((char)Marshal.ReadByte(ptr, off)).ToString();
                }
                else if(type == "System.String")
                {
                    StringBuilder builder = new StringBuilder();

                    for(int i = off; ; i++)
                    {
                        byte bt = Marshal.ReadByte(ptr, i);

                        if(bt == 0)  break;
                        else builder.Append((char)bt);
                    }

                    return builder.ToString();
                }

                return "";
            } },

            {"ptr.write", (args)=>{
                string type = ShortTypeNameToFull(args[1]);
                IntPtr ptr = Pointers[args[0]];
                int off = ToInt(args[2]);

                if(type == "System.Byte")
                {
                    Marshal.WriteByte(ptr, off, byte.Parse(args[3]));
                }
                else if(type == "System.Int16")
                {
                    Marshal.WriteInt16(ptr, off, short.Parse(args[3]));
                }
                else if(type == "System.Int32")
                {
                    Marshal.WriteInt32(ptr, off, int.Parse(args[3]));
                }
                else if(type == "System.Int64")
                {
                    Marshal.WriteInt64(ptr, off, long.Parse(args[3]));
                }
                else if(type == "System.Char")
                {
                    Marshal.WriteByte(ptr, (byte)args[3][0]);
                }
                else if(type == "System.String")
                {
                    for(int i = 0; i < args[3].Length; i++)
                    {
                        Marshal.WriteByte(ptr, off + i, (byte)args[3][i]);
                    }
                    Marshal.WriteByte(ptr, off + args[3].Length, 0);
                }

                return "";
            } },

            {"ptr.getValue", (args)=>{
                return Pointers[args[0]].ToString();
            } },

            {"ptr.sizeof", (args)=>{
                return Marshal.SizeOf(Type.GetType(ShortTypeNameToFull(args[0]))).ToString();
            } },

            {"ptr.getAnsiString", (args)=>{
                return Marshal.PtrToStringAnsi(Pointers[args[0]]);
            } },

            {"ptr.putAnsiString", (args)=>{
                string name = "__WESH_POINTER_"+GetRandomName();
                Pointers.Add(name, Marshal.StringToHGlobalAnsi(args[0]));
                return name;
            } },

            {"ptr.getUnicodeString", (args)=>{
                return Marshal.PtrToStringUni(Pointers[args[0]]);
            } },

            {"ptr.putUnicodeString", (args)=>{
                string name = "__WESH_POINTER_"+GetRandomName();
                Pointers.Add(name, Marshal.StringToHGlobalUni(args[0]));
                return name;
            } },

            {"struct.define", (args)=>{
                if(args.Length == 0 || args.Length % 2 != 0)
                {
                    throw new ArgumentException();
                }

                string name = "__WESH_STRUCT_"+GetRandomName();

                Dictionary<string, string> dict = new Dictionary<string, string>();
                int totalSize = 0;

                for(int i = 0; i < args.Length; i += 2)
                {
                    string type = ShortTypeNameToFull(args[i]);
                    int size = Marshal.SizeOf(Type.GetType(type));
                    dict.Add(args[i+1], type);
                    totalSize += size;
                }

                StructDefinitions.Add(name, dict);
                Pointers.Add(name, Marshal.AllocHGlobal(totalSize));

                return name;
            } },

            {"struct.get", (args)=>{
                IntPtr ptr = Pointers[args[0]];
                string type = StructDefinitions[args[0]][args[1]];
                int off = 0;

                foreach(KeyValuePair<string, string> kvp in StructDefinitions[args[0]])
                {
                    if(kvp.Key == args[1]) break;
                    off += Marshal.SizeOf(Type.GetType(kvp.Value));
                }

                if(type == "System.Byte")
                {
                    return Marshal.ReadByte(ptr, off).ToString();
                }
                else if(type == "System.Int16")
                {
                    return Marshal.ReadInt16(ptr, off).ToString();
                }
                else if(type == "System.Int32")
                {
                    return Marshal.ReadInt32(ptr, off).ToString();
                }
                else if(type == "System.Int64")
                {
                    return Marshal.ReadInt64(ptr, off).ToString();
                }
                else if(type == "System.Char")
                {
                    return ((char)Marshal.ReadByte(ptr, off)).ToString();
                }
                else if(type == "System.String")
                {
                    StringBuilder builder = new StringBuilder();

                    for(int i = off; ; i++)
                    {
                        byte bt = Marshal.ReadByte(ptr, i);

                        if(bt == 0)  break;
                        else builder.Append((char)bt);
                    }

                    return builder.ToString();
                }

                return "";
            } },

            {"struct.set", (args)=>{
                IntPtr ptr = Pointers[args[0]];
                string type = StructDefinitions[args[0]][args[1]];
                int off = 0;

                foreach(KeyValuePair<string, string> kvp in StructDefinitions[args[0]])
                {
                    if(kvp.Key == args[1]) break;
                    off += Marshal.SizeOf(Type.GetType(kvp.Value));
                }

                if(type == "System.Byte")
                {
                    Marshal.WriteByte(ptr, off, byte.Parse(args[3]));
                }
                else if(type == "System.Int16")
                {
                    Marshal.WriteInt16(ptr, off, short.Parse(args[2]));
                }
                else if(type == "System.Int32")
                {
                    Marshal.WriteInt32(ptr, off, int.Parse(args[2]));
                }
                else if(type == "System.Int64")
                {
                    Marshal.WriteInt64(ptr, off, long.Parse(args[2]));
                }
                else if(type == "System.Char")
                {
                    Marshal.WriteByte(ptr, (byte)args[2][0]);
                }
                else if(type == "System.String")
                {
                    for(int i = 0; i < args[3].Length; i++)
                    {
                        Marshal.WriteByte(ptr, off + i, (byte)args[2][i]);
                    }
                    Marshal.WriteByte(ptr, off + args[2].Length, 0);
                }

                return "";
            } },

            {"audio.open", (args)=>{
                string name = "__WESH_PLAYER_"+GetRandomName();

                if (!File.Exists(GetPath(args[0])))
                {
                    throw new FileNotFoundException();
                }

                mciSendString("open \""+GetPath(args[0])+"\" alias "+name, null, 0, IntPtr.Zero);

                return name;
            } },

            {"audio.close", (args)=>{
                mciSendString("close "+args[0], null, 0, IntPtr.Zero);
                return "";
            } },

            {"audio.play", (args)=>{
                mciSendString("play "+args[0]+(args.Length>1?" "+args[1]:""), null, 0, IntPtr.Zero);
                return "";
            } },

            {"audio.stop", (args)=>{
                mciSendString("stop "+args[0], null, 0, IntPtr.Zero);
                return "";
            } },

            {"audio.pause", (args)=>{
                mciSendString("pause "+args[0], null, 0, IntPtr.Zero);
                return "";
            } },

            {"audio.resume", (args)=>{
                mciSendString("resume "+args[0], null, 0, IntPtr.Zero);
                return "";
            } },

            {"audio.sendMCI", (args)=>{
                mciSendString(args[0], null, 0, IntPtr.Zero);
                return "";
            } },

            {"input.sendClick", (args)=>{
                Click(Pointers[args[0]], args[1], ToInt(args[2]), ToInt(args[3]), args.Length>4?ToInt(args[4]):0);
                return "";
            } },

            {"input.sendMouseMove", (args)=>{
                MouseMove(Pointers[args[0]], ToInt(args[1]), ToInt(args[2]));
                return "";
            } },

            {"input.sendKeyPress", (args)=>{
                PressKey(Pointers[args[0]], (int)Enum.Parse(typeof(Keys), args[1]), args.Length>2?ToInt(args[2]):0);
                return "";
            } },

            {"input.sendDoubleClick", (args)=>{
                DoubleClick(Pointers[args[0]], ToInt(args[1]), ToInt(args[2]));
                return "";
            } },
        };

        public static Dictionary<string, string> Lang = new Dictionary<string, string>()
        {
            { "comment", "#" },
            { "cmdDelim", ";" },
            { "argsDelim", "," },
            { "objectDelim", "." },
            { "q1", "'" },
            { "q2", "\"" },
            { "codeBlockStart", "{" },
            { "codeBlockEnd", "}" },
            { "operatorBlockStart", "(" },
            { "operatorBlockEnd", ")" },
            { "altOperatorBlockStart", "[" },
            { "altOperatorBlockEnd", "]" },
            { "varOperator", "@" },
            { "execOperator", "&" },
            { "mathOperator", "%" },
            { "doNotParseOperator", "~" },
            { "thisKeyword", "this" },
            { "returnKeyword", "return" },
            { "constructorKeyword", "constructor" }
        };

        public static Dictionary<string, string> Variables = new Dictionary<string, string>()
        {
            { "null", WeshNull },
            { "error", "" },
            { "newLine", Environment.NewLine },
            { "errorAction", "none" },
            { "stopOnError", "false" },
            { "weshVer", Version.ToString() },
            { "weshDir", WeshDir },
            { "weshPath", WeshPath },
            { "currDir", Environment.CurrentDirectory },
            { "modulesDir", WeshDir + "\\wesh-modules" },
            { "modulesUrl", "https://nekit270.ch/wesh-modules/MODULE_NAME.weshm" },
            { "runtimeVersion", Environment.Version.Major+"."+Environment.Version.Minor+"."+Environment.Version.Build },
            { "runtimeVersionNumber", Environment.Version.Major+"."+Environment.Version.Minor }
        };

        public static List<string> Constants = new List<string>()
        {
            "weshPath", "weshDir", "weshVer", "newLine", "null"
        };

        public static Dictionary<string, Dictionary<string, string>> UserObjects = new Dictionary<string, Dictionary<string, string>>();
        public static Dictionary<string, Control> Controls = new Dictionary<string, Control>();
        public static Dictionary<string, string> Functions = new Dictionary<string, string>();
        public static Dictionary<string, string[]> FunctionArguments = new Dictionary<string, string[]>();
        public static Dictionary<string, IntPtr> Pointers = new Dictionary<string, IntPtr>();
        public static Dictionary<string, Dictionary<string, string>> StructDefinitions = new Dictionary<string, Dictionary<string, string>>();
        public static Dictionary<string, Type> ExtObjectTypes = new Dictionary<string, Type>();
        public static Dictionary<string, object> ExtObjects = new Dictionary<string, object>();
        public static Dictionary<string, Dictionary<string, string>> Classes = new Dictionary<string, Dictionary<string, string>>();
        public static List<Assembly> LoadedAssemblies = new List<Assembly>();
        private static string ShortTypeNameToFull(string sn)
        {
            string name = sn;
            if (name == "int") name = "System.Int32";
            else if (name == "uint") name = "System.UInt32";
            else if (name == "long") name = "System.Int64";
            else if (name == "ulong") name = "System.UInt64";
            else if (name == "double") name = "System.Double";
            else if (name == "byte") name = "System.Byte";
            else if (name == "intptr") name = "System.IntPtr";
            else if (name == "char") name = "System.Char";
            else if (name == "string") name = "System.String";
            return name;
        }

        private static void Click(IntPtr hWnd, string btn, int x, int y, int time = 0)
        {
            long lParam = GetMouseLParam(x, y);
            SendMessage(hWnd, btn == "left" ? WM_LBUTTONDOWN : (btn == "right" ? WM_RBUTTONDOWN : WM_MBUTTONDOWN), 0, lParam);
            if (time > 0) Thread.Sleep(time);
            SendMessage(hWnd, btn == "left" ? WM_LBUTTONUP : (btn == "right" ? WM_RBUTTONUP : WM_MBUTTONUP), 0, lParam);
        }

        private static void DoubleClick(IntPtr hWnd, int x, int y)
        {
            long lParam = GetMouseLParam(x, y);
            SendMessage(hWnd, WM_LBUTTONDOWN, 0, lParam);
            SendMessage(hWnd, WM_LBUTTONDBLCLK, 0, lParam);
            SendMessage(hWnd, WM_LBUTTONUP, 0, lParam);
        }

        private static void MouseMove(IntPtr hWnd, int x, int y)
        {
            long lParam = GetMouseLParam(0, 0);
            SendMessage(hWnd, WM_MOUSEMOVE, 0, lParam);
        }

        private static void PressKey(IntPtr hWnd, int keyCode, int time = 0)
        {
            SendMessage(hWnd, WM_KEYDOWN, (long)keyCode, GetKeyDownLParam(keyCode));
            if (time > 0) Thread.Sleep(time);
            SendMessage(hWnd, WM_KEYUP, (long)keyCode, GetKeyUpLParam(keyCode));
        }

        private static long GetMouseLParam(int x, int y)
        {
            return x | (y << 16);
        }

        private static long GetKeyDownLParam(int key)
        {
            return (MapVirtualKey((uint)key, 0) << 16) | 0;
        }

        private static long GetKeyUpLParam(int key)
        {
            return (0x00000001 | (MapVirtualKey((uint)key, 0) << 16) | (0 << 29) | (1 << 30) | (1 << 31));
        }

        public static DialogResult InputBox(string promptText, string title, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 10, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private static object GetProp(Type t, object o, string name)
        {
            return t.InvokeMember(name, System.Reflection.BindingFlags.GetProperty, null, o, new object[] { });
        }

        private static void SetProp(Type t, object o, string name, object value)
        {
            t.InvokeMember(name, System.Reflection.BindingFlags.SetProperty, null, o, new object[] { value });
        }

        private static object InvokeMethod(Type t, object o, string name, object[] args)
        {
            return t.InvokeMember(name, System.Reflection.BindingFlags.InvokeMethod, null, o, args);
        }

        private static string WeshNullToNull(string s)
        {
            if (s == WeshNull) return null;
            return s;
        }

        private static string NullToWeshNull(string s)
        {
            if (s == null) return WeshNull;
            return s;
        }

        private static string GetLangEl(string la)
        {

            string l = Lang[la];
            if (@"[]\^$.|?*+()".Contains(l))
            {
                return "\\" + l;
            }
            return l;
        }

        public static string GetVariable(string name)
        {
            if (name.Contains(Lang["objectDelim"]))
            {
                string[] sp = name.Split(Lang["objectDelim"][0]);

                string nm = "";
                string val = sp[0];

                for(int i = 1; i < sp.Length; i++){
                    nm = sp[i];

                    if (UserObjects.ContainsKey(val) && UserObjects[val].ContainsKey(nm))
                    {
                        val = UserObjects[val][nm];
                    }
                    else if (i == 1 && Variables.ContainsKey(val) && UserObjects.ContainsKey(Variables[val]) && UserObjects[Variables[val]].ContainsKey(nm))
                    {
                        val = UserObjects[Variables[val]][nm];
                    }
                    else
                    {
                        return WeshNull;
                    }
                }

                return val;
            }
            else if (Variables.ContainsKey(name))
            {
                return Variables[name];
            }
            else
            {
                return WeshNull;
            }
        }

        public static void SetVariable(string name, string value)
        {
            if (Constants.Contains(name)) throw new Exception(name + " is a constant.");
            if (name.Contains(Lang["objectDelim"]))
            {
                string[] sp = name.Split(Lang["objectDelim"][0]);

                string nm = "";
                string val = sp[0];
                Dictionary<string, string> obj = null;

                for(int i = 1; i < sp.Length; i++){
                    nm = sp[i];

                    if (UserObjects.ContainsKey(val) && UserObjects[val].ContainsKey(nm))
                    {
                        obj = UserObjects[val];
                        val = UserObjects[val][nm];
                    }
                    else if (UserObjects.ContainsKey(val))
                    {
                        obj = UserObjects[val];
                        break;
                    }
                    else if (i == 1 && UserObjects.ContainsKey(Variables[val]) && UserObjects[Variables[val]].ContainsKey(nm))
                    {
                        obj = UserObjects[Variables[val]];
                        val = UserObjects[Variables[val]][nm];
                    }
                    else if (i == 1 && UserObjects.ContainsKey(Variables[val]))
                    {
                        obj = UserObjects[Variables[val]];
                        break;
                    }
                }

                if(obj.ContainsKey(nm)){
                    obj[nm] = value;
                }else{
                    obj.Add(nm, value);
                }
            }
            else
            {
                if (Variables.ContainsKey(name))
                {
                    Variables[name] = value;
                }
                else
                {
                    Variables.Add(name, value);
                }
            }
        }

        public static string GetPath(string p)
        {
            return GetPath(p, false);
        }

        public static string GetPath(string p, bool f)
        {
            if (p.Length == 0) p = ".";
            Environment.CurrentDirectory = Variables["currDir"];
            string path = Path.IsPathRooted(p) ? p : Path.GetFullPath(p);
            if (f || File.Exists(path) || Directory.Exists(path)) return path;
            return null;
        }

        public static string GetRandomName()
        {
            return Guid.NewGuid().ToString();
        }

        private static RegistryKey GetRegistryKey(string keyName, bool write)
        {
            string[] k = keyName.Split('\\');
            RegistryKey mKey;
            switch (k[0])
            {
                case "HKLM":
                case "HKEY_LOCAL_MACHINE":
                    mKey = Registry.LocalMachine;
                    break;

                case "HKCR":
                case "HKEY_CLASSES_ROOT":
                    mKey = Registry.ClassesRoot;
                    break;

                case "HKCU":
                case "HKEY_CURRENT_USER":
                    mKey = Registry.CurrentUser;
                    break;

                case "HKU":
                case "HKEY_USERS":
                    mKey = Registry.Users;
                    break;

                case "HKCC":
                case "HKEY_CURRENT_CONFIG":
                    mKey = Registry.CurrentConfig;
                    break;

                default:
                    mKey = null;
                    break;
            }
            k = k.Skip(1).ToArray();

            var key = mKey.OpenSubKey(String.Join("\\", k), write);

            return key;
        }

        public static string CreateObject(Dictionary<string, string> dict)
        {
            string name = "__WESH_OBJECT_"+GetRandomName();
            UserObjects.Add(name, dict);
            return name;
        }

        public static string CreateArray(string[] values)
        {
            var dict = new Dictionary<string, string>();
            var i = 0;

            foreach (string el in values)
            {
                dict.Add(i.ToString(), el);
                i++;
            }

            dict.Add("length", i.ToString());

            return CreateObject(dict);
        }

        public static string[] WeshArrayToArray(string arr)
        {
            List<string> a = new List<string>();
            var obj = UserObjects[arr];

            for(int i = 0; i < ToInt(obj["length"]); i++)
            {
                a.Add(obj[i.ToString()]);
            }

            return a.ToArray();
        }

        public static void ArrayToWeshArray(string name, string[] values)
        {
            var dict = new Dictionary<string, string>();
            var i = 0;

            foreach (string el in values)
            {
                dict.Add(i.ToString(), el);
                i++;
            }

            dict.Add("length", i.ToString());

            UserObjects[name] = dict;
        }

        private static string GetRequest(string url)
        {
            if (url.StartsWith("https://"))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            }
            else
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
            }

            var req = WebRequest.Create(url);

            var res = (HttpWebResponse)req.GetResponse();
            var str = new StreamReader(res.GetResponseStream()).ReadToEnd();
            res.Close();

            return str;
        }

        private static string PostRequest(string url, string data)
        {
            if (url.StartsWith("https://"))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            }
            else
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
            }

            var req = WebRequest.Create(url);

            var pd = Encoding.ASCII.GetBytes(data);

            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = pd.Length;

            req.GetRequestStream().Write(pd, 0, pd.Length);

            var res = (HttpWebResponse)req.GetResponse();
            var str = new StreamReader(res.GetResponseStream()).ReadToEnd();
            res.Close();

            return str;
        }

        public static void HelpMessage()
        {
            Console.WriteLine(@"
wesh [-v] [-h] [-c <команда>] [-f <файл>]

        [-v]                       Выводит версию WESH.
        [-h]                       Выводит справочное сообщение.
        [-c <команда>]             Выполняет указанную команду.
        [-f <файл> <аргументы>]    Выполняет скрипт.
");
        }

        private static bool Match(string s, string r)
        {
            Regex regex = new Regex(r);
            MatchCollection matches = regex.Matches(s);
            return (matches.Count > 0);
        }

        private static void ServeFolder(string folder, int port, bool q)
        {
            HttpListener server = new HttpListener();
            server.Prefixes.Add("http://0.0.0.0:"+port+"/");
            server.Start();

            while (true)
            {
                var ctx = server.GetContext();
                var req = ctx.Request;
                var res = ctx.Response;
                var output = res.OutputStream;

                byte[] buffer;
                string url = GetPath(folder) + req.RawUrl;
                if (url.Contains("?"))
                {
                    url = url.Split('?')[0];
                }

                if (File.Exists(url))
                {
                    buffer = File.ReadAllBytes(url);
                    res.ContentEncoding = Encoding.UTF8;
                }
                else if (File.Exists(url + "/index.html"))
                {
                    buffer = File.ReadAllBytes(url + "/index.html");
                    res.ContentEncoding = Encoding.UTF8;
                }
                else
                {
                    res.StatusCode = 404;
                    res.ContentType = "text/html";
                    buffer = Encoding.UTF8.GetBytes("<!doctype html><center><h1>404 Not Found</h1><p>File "+req.RawUrl+" not found.</p></center>");
                }
                res.ContentLength64 = buffer.Length;
                output.Write(buffer, 0, buffer.Length);
                output.Flush();
                output.Close();

                if (!q) Console.WriteLine("["+req.RemoteEndPoint+"] "+req.RawUrl+"\t"+res.StatusCode);
            }
        }

        private static bool StringToBool(string s)
        {
            if(s.ToLower() == "true") return true;
            return false;
        }

        private static string BoolToString(bool b)
        {
            return b ? "true" : "false";
        }

        private static int ToInt(string s)
        {
            try
            {
                return (s.StartsWith("0x")?System.Convert.ToInt32(s, 16):Int32.Parse(s));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static string EvalJS(string expr)
        {
            try
            {
                return Eval.JScriptEvaluate(expr, JSEngine).ToString();
            }
            catch (Exception)
            {
                return expr;
            }
            
        }

        private static bool Cond(string expr)
        {
            return (bool)dt.Compute(expr, "");
        }

        private static string ToBase64(string s)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(s);
                return System.Convert.ToBase64String(bytes);
            }
            catch (FormatException)
            {
                return s;
            }
        }

        private static string FromBase64(string s)
        {
            try
            {
                var bytes = System.Convert.FromBase64String(s);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return s;
            }
        }

        public static string ParseQuotes(string cmd)
        {
            cmd = Regex.Replace(cmd, GetLangEl("codeBlockStart") + @"([^" + GetLangEl("codeBlockEnd") + @"]+)" + GetLangEl("codeBlockEnd"), new MatchEvaluator((m) =>
            {
                return m.Groups[1].Value.Replace(GetLangEl("q1"), "$Q1").Replace(GetLangEl("q2"), "$Q2").Replace(" ", "$SP").Replace(GetLangEl("argsDelim"), "$CM").Replace(GetLangEl("varOperator"), "$AT").Replace(GetLangEl("execOperator"), "$AM").Replace(GetLangEl("mathOperator"), "$PR");
            }));
            cmd = Regex.Replace(cmd, GetLangEl("q1") + @"([^" + GetLangEl("q1") + @"]*)" + GetLangEl("q1"), new MatchEvaluator((m) =>
            {
                return m.Groups[1].Value.Replace(GetLangEl("q2"), "$Q2").Replace(" ", "$SP").Replace(GetLangEl("argsDelim"), "$CM");
            }));
            cmd = Regex.Replace(cmd, GetLangEl("q2") + @"([^" + GetLangEl("q2") + "]*)" + GetLangEl("q2"), new MatchEvaluator((m) =>
            {
                return m.Groups[1].Value.Replace(GetLangEl("q1"), "$Q1").Replace(" ", "$SP").Replace(GetLangEl("argsDelim"), "$CM");
            }));

            return cmd;
        }

        public static string ParseArg(string arg)
        {
            if (arg.Trim().Length < 2) return arg;

            arg = arg.Replace("$SP", " ");
            arg = arg.Replace("$SM", Lang["cmdDelim"]);
            arg = arg.Replace("$CM", Lang["argsDelim"]);
            arg = arg.Replace("$Q1", Lang["q1"]);
            arg = arg.Replace("$Q2", Lang["q2"]);
            arg = arg.Replace("$CBS", Lang["codeBlockStart"]);
            arg = arg.Replace("$CBE", Lang["codeBlockEnd"]);

            if (arg.StartsWith(GetLangEl("doNotParseOperator")))
            {
                return arg.Substring(1);
            }

            if (arg[0] == Lang["execOperator"][0] && arg[1] != Lang["operatorBlockStart"][0] && arg[1] != Lang["altOperatorBlockStart"][0])
            {
                return Exec(arg.Substring(1));
            }

            if (arg[0] == Lang["mathOperator"][0] && arg[1] != Lang["operatorBlockStart"][0] && arg[1] != Lang["altOperatorBlockStart"][0])
            {
                return dt.Compute(arg.Substring(1), "").ToString();
            }

            MatchCollection vbMatches = new Regex(GetLangEl("varOperator") + GetLangEl("operatorBlockStart") + "[^" + GetLangEl("operatorBlockEnd") + "]+" + GetLangEl("operatorBlockEnd")).Matches(arg);
            if (vbMatches.Count > 0)
            {
                foreach (Match vbm in vbMatches)
                {
                    string val = vbm.Value;
                    val = val.Replace(Lang["varOperator"] + Lang["operatorBlockStart"], "").Replace(Lang["operatorBlockEnd"], "");
                    arg = arg.Replace(vbm.Value, GetVariable(val));
                }
            }

            MatchCollection vMatches = new Regex(GetLangEl("varOperator") + @"[a-zA-Z0-9_" + GetLangEl("objectDelim") + "]+").Matches(arg);
            if (vMatches.Count > 0)
            {
                foreach (Match m in vMatches)
                {
                    string val = m.Value;
                    val = val.Replace(Lang["varOperator"], "");
                    arg = arg.Replace(m.Value, GetVariable(val));
                }
            }

            MatchCollection eMatches = new Regex(GetLangEl("execOperator") + GetLangEl("operatorBlockStart") + "[^" + GetLangEl("operatorBlockEnd") + "]+" + GetLangEl("operatorBlockEnd")).Matches(arg);
            if (eMatches.Count > 0)
            {
                foreach (Match em in eMatches)
                {
                    string val = em.Value;
                    val = val.Replace(Lang["execOperator"] + Lang["operatorBlockStart"], "").Replace(Lang["operatorBlockEnd"], "");
                    arg = arg.Replace(em.Value, Exec(val));
                }
            }

            MatchCollection mMatches = new Regex(GetLangEl("mathOperator") + GetLangEl("operatorBlockStart") + "[^" + GetLangEl("operatorBlockEnd") + "]+" + GetLangEl("operatorBlockEnd")).Matches(arg);
            if (mMatches.Count > 0)
            {
                foreach (Match mm in mMatches)
                {
                    string val = mm.Value;
                    val = val.Replace(Lang["mathOperator"] + Lang["operatorBlockStart"], "").Replace(Lang["operatorBlockEnd"], "");
                    arg = arg.Replace(mm.Value, dt.Compute(val, "").ToString());
                }
            }

            MatchCollection avbMatches = new Regex(GetLangEl("varOperator") + GetLangEl("altOperatorBlockStart") + "[^" + GetLangEl("altOperatorBlockEnd") + "]+" + GetLangEl("altOperatorBlockEnd")).Matches(arg);
            if (avbMatches.Count > 0)
            {
                foreach (Match vbm in avbMatches)
                {
                    string val = vbm.Value;
                    val = val.Replace(Lang["varOperator"] + Lang["altOperatorBlockStart"], "").Replace(Lang["altOperatorBlockEnd"], "");
                    arg = arg.Replace(vbm.Value, GetVariable(val));
                }
            }

            MatchCollection aeMatches = new Regex(GetLangEl("execOperator") + GetLangEl("altOperatorBlockStart") + "[^" + GetLangEl("altOperatorBlockEnd") + "]+" + GetLangEl("altOperatorBlockEnd")).Matches(arg);
            if (aeMatches.Count > 0)
            {
                foreach (Match em in aeMatches)
                {
                    string val = em.Value;
                    val = val.Replace(Lang["execOperator"] + Lang["altOperatorBlockStart"], "").Replace(Lang["altOperatorBlockEnd"], "");
                    arg = arg.Replace(em.Value, Exec(val));
                }
            }

            MatchCollection amMatches = new Regex(GetLangEl("mathOperator") + GetLangEl("altOperatorBlockStart") + "[^" + GetLangEl("altOperatorBlockEnd") + "]+" + GetLangEl("altOperatorBlockEnd")).Matches(arg);
            if (amMatches.Count > 0)
            {
                foreach (Match mm in amMatches)
                {
                    string val = mm.Value;
                    val = val.Replace(Lang["mathOperator"] + Lang["altOperatorBlockStart"], "").Replace(Lang["altOperatorBlockEnd"], "");
                    arg = arg.Replace(mm.Value, dt.Compute(val, "").ToString());
                }
            }

            arg = arg.Replace("$AM", Lang["execOperator"]).Replace("$AT", Lang["varOperator"]).Replace("$PR", Lang["mathOperator"]);

            return arg;
        }

        public static string ExecFunction(string name, string[] args)
        {
            SetVariable("funcArgs", CreateArray(args));

            int i = 0;
            foreach(string argName in FunctionArguments[name])
            {
                SetVariable(argName, args[i]);
                i++;
            }

            return Exec(Functions[name], true, true);
        }

        public static string Exec(string cmd)
        {
            return Exec(cmd, false, true);
        }

        public static string Exec(string cmd, bool isFunc, bool catchErrors)
        {
            string ret = "";
            try
            {
                if (cmd == null || cmd.Trim().Length == 0) return "";

                List<string> args = new List<string>();

                cmd = cmd.Replace("\\"+Lang["cmdDelim"], "$SM");

                string[] cs = cmd.Split(Lang["cmdDelim"][0]);
                if(cs.Length > 1)
                {
                    string r = "";
                    foreach(string cm in cs)
                    {
                        string c = cm;
                        if (c.Trim().Length > 0 && c.Trim() != Lang["cmdDelim"])
                        {
                            bool isr = !isFunc || c.Trim().StartsWith("return ");
                            string exr = Exec(c.Replace("\\" + Lang["cmdDelim"], Lang["cmdDelim"]));
                            if (isr && exr.Trim().Length > 0) r += exr;
                        }
                    }
                    return r;
                }

                cmd = cmd.Trim().Replace("\\" + Lang["codeBlockStart"], "$CBS").Replace("\\"+Lang["codeBlockEnd"], "$CBE");

                cmd = ParseQuotes(cmd);

                cmd = cmd.Replace(GetLangEl("argsDelim"), "");
                args = cmd.Split(' ').ToList();

                cmd = args[0];
                if (args.Count == 1)
                {
                    args.Clear();
                }
                else
                {
                    args.RemoveAt(0);
                }

                for (int j = 0; j < args.Count; j++)
                {
                    args[j] = ParseArg(args[j]);
                }

                if (!Commands.ContainsKey(ParseArg(cmd)))
                {
                    if (Functions.ContainsKey(ParseArg(cmd)))
                    {
                        return ExecFunction(ParseArg(cmd), args.ToArray());
                    }

                    if (args.Count > 0)
                    {
                        if(args[0] == "=")
                        {
                            SetVariable(ParseArg(cmd), String.Join(" ", args.Skip(1)));
                            return "";
                        }
                        else if(args[0] == "=&")
                        {
                            SetVariable(ParseArg(cmd), Exec(String.Join(" ", args.Skip(1))));
                            return "";
                        }
                        else if(args[0] == "=@")
                        {
                            SetVariable(ParseArg(cmd), GetVariable(String.Join(" ", args.Skip(1))));
                            return "";
                        }
                        else if(args[0] == "=%")
                        {
                            SetVariable(ParseArg(cmd), dt.Compute(String.Join(" ", args.Skip(1)), "").ToString());
                            return "";
                        }
                        else if(args[0] == "+=")
                        {
                            string varName = ParseArg(cmd);
                            SetVariable(varName, (double.Parse(GetVariable(varName)) + double.Parse(dt.Compute(String.Join(" ", args.Skip(1)), "").ToString())).ToString());
                            return "";
                        }
                        else if (args[0] == "-=")
                        {
                            string varName = ParseArg(cmd);
                            SetVariable(varName, (double.Parse(GetVariable(varName)) - double.Parse(dt.Compute(String.Join(" ", args.Skip(1)), "").ToString())).ToString());
                            return "";
                        }
                        else if (args[0] == "*=")
                        {
                            string varName = ParseArg(cmd);
                            SetVariable(varName, (double.Parse(GetVariable(varName)) * double.Parse(dt.Compute(String.Join(" ", args.Skip(1)), "").ToString())).ToString());
                            return "";
                        }
                        else if (args[0] == "/=")
                        {
                            string varName = ParseArg(cmd);
                            SetVariable(varName, (double.Parse(GetVariable(varName)) / double.Parse(dt.Compute(String.Join(" ", args.Skip(1)), "").ToString())).ToString());
                            return "";
                        }
                    }

                    string msg = "ERROR: \""+cmd+"\" command not found";

                    if (!catchErrors) throw new Exception(msg);

                    SetVariable("error", msg);

                    string errAct = GetVariable("errorAction");
                    if (errAct == "print") Console.WriteLine(msg);
                    else if (errAct == "msgbox") MessageBox.Show(msg, "wesh", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (errAct.StartsWith("func:")) ExecFunction(errAct.Replace("func:", ""), new string[] { });

                    if (GetVariable("stopOnError") == "true")
                    {
                        Environment.Exit(1);
                    }

                    return msg;
                }

                ret = Commands[cmd](args.ToArray());
            }
            catch (Exception ex)
            {
                if (!catchErrors) throw ex;

                ret = "ERROR: " + ex.Message;
                SetVariable("error", ret);

                string errAct = GetVariable("errorAction");
                if (errAct == "print") Console.WriteLine(ret);
                else if (errAct == "msgbox") MessageBox.Show(ret, "wesh", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (errAct.StartsWith("func:")) ExecFunction(errAct.Replace("func:", ""), new string[] { });

                if (GetVariable("stopOnError") == "true")
                {
                    Environment.Exit(1);
                }
            }
            return ret;
        }

        public static string[] PrepareScript(string rawCode)
        {
            rawCode = rawCode.Replace("\r\n", "\n");
            rawCode = rawCode.Replace("\r", "\n");
            rawCode = Regex.Replace(rawCode, @"\n[\s]*"+GetLangEl("comment")+@".*\n", "\n");

            int bCount = 0;
            for (int i = 0; i < rawCode.Length; i++)
            {
                char ch = rawCode[i];

                if (bCount != 0)
                {
                    if (ch == '\n')
                    {
                        StringBuilder sb = new StringBuilder(rawCode);
                        sb[i] = ' ';
                        rawCode = sb.ToString();
                        rawCode = rawCode.Insert(i, " "+new string('\\', bCount)+Lang["cmdDelim"]+" ");
                        i += 3 + bCount;
                    }
                }

                if (ch == Lang["codeBlockStart"][0])
                {
                    if (bCount != 0)
                    {
                        rawCode = rawCode.Insert(i, new string('\\', bCount));
                        i += bCount;
                    }
                    bCount++;
                }
                else if (ch == Lang["codeBlockEnd"][0])
                {
                    if (bCount != 0)
                    {
                        rawCode = rawCode.Insert(i, new string('\\', bCount - 1));
                        i += bCount - 1;
                    }
                    bCount--;
                }
                if (ch == Lang["cmdDelim"][0])
                {
                    if (bCount != 0)
                    {
                        rawCode = rawCode.Insert(i, new string('\\', bCount));
                        i += bCount;
                    }
                }
            }

            rawCode = Regex.Replace(rawCode, @"([\\]*)"+GetLangEl("codeBlockStart")+@"[\s]*([\\]*)"+GetLangEl("cmdDelim"), "$1" + Lang["codeBlockStart"]);
            rawCode = Regex.Replace(rawCode, GetLangEl("codeBlockStart")+@"[\s]*\n", Lang["codeBlockStart"]);
            rawCode = Regex.Replace(rawCode, GetLangEl("cmdDelim") + "[\\s]*\\n", GetLangEl("cmdDelim"));
            rawCode = Regex.Replace(rawCode, @"([\\]*)"+GetLangEl("cmdDelim")+@"[\s]*([\\]*)"+GetLangEl("codeBlockEnd"), " $2" + Lang["codeBlockEnd"]);

            return rawCode.Split('\n');
        }

        public static string ExecScript(string rawCode)
        {
            string[] code = PrepareScript(rawCode);
            string output = "";

            foreach (string line in code)
            {
                if (line.Trim().Length == 0 || line.Trim()[0] == Lang["comment"][0]) continue;
                string ex = Exec(line);
                if (ex.Trim().Length != 0) output += ex + Environment.NewLine;
            }
            return output;
        }
    }
}
