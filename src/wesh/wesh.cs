﻿using System;
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
using Microsoft.VisualBasic;

namespace wesh
{
    delegate string Command(string[] args);

    class WESH
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);
        
        public const double Version = 0.6;
        public const string VersionStr = "WESH v0.6 [17.03.2023]";
        public const string WeshNull = "__WESH_NULL";
        public static readonly string WeshPath = Process.GetCurrentProcess().MainModule.FileName;
        public static readonly string WeshDir = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).Directory.FullName;
        private static readonly Random rand = new Random((int)DateTime.Now.Ticks);

        public static Dictionary<string, Command> Commands = new Dictionary<string, Command>()
        {
            {".", (args)=>{
                return EvalJS(String.Join(" ", args));
            } },

            {"echo", (args)=>{
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
                    if(!Variables.ContainsKey(sp[0]) || !UserObjects.ContainsKey(Variables[sp[0]])) return "ОШИБКА: Объект не найден.";
                    if (UserObjects[Variables[sp[0]]].ContainsKey(sp[1]))
                    {
                        UserObjects[Variables[sp[0]]][sp[1]] = ExecSub(args[1]);
                    }
                    else
                    {
                        UserObjects[Variables[sp[0]]].Add(sp[1], ExecSub(args[1]));
                    }
                }
                else
                {
                    if (Variables.ContainsKey(args[0]))
                    {
                        Variables[args[0]] = ExecSub(args[1]);
                    }
                    else
                    {
                        Variables.Add(args[0], ExecSub(args[1]));
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
                SetVariable(args[0], args[1]);
                return "";
            } },

            {"var.delete", (args)=>{
                Variables.Remove(args[0]);
                return "";
            } },

            {"exit", (args)=>{
                Environment.Exit((args.Length > 0?ToInt(args[0]):0));
                return "";
            } },

            {"exec", (args)=>{
                return ExecSub(args[0]);
            } },

            {"load", (args)=>{
                foreach(string arg in args)
                {
                    string code = "";
                    if(GetPath(arg) != null) code = File.ReadAllText(GetPath(arg));
                    else if(arg.StartsWith("http")) code = new WebClient().DownloadString(arg);
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

            {"if", (args)=>{
                if (Cond(args[0]))
                {
                    return ExecSub(args[1]);
                }
                else
                {
                    return args.Length > 2?ExecSub(args[2]):"";
                }
            } },

            {"ife", (args)=>{
                if (Cond(ExecSub(args[0])))
                {
                    return ExecSub(args[1]);
                }
                else
                {
                    return args.Length > 2?ExecSub(args[2]):"";
                }
            } },

            {"while", (args)=>{
                string r = "";
                while (Cond(args[0]))
                {
                    r += ExecSub(args[1])+"\n";
                }
                return r;
            } },

            {"for", (args)=>{
                string r = "";
                for(int i = ToInt(args[1]); i < ToInt(args[2]); i++)
                {
                    r += ExecSub(args[3].Replace($"@{args[0]}", i.ToString()))+"\n";
                }
                return r;
            } },

            {"foreach", (args)=>{
                string[] s = ExecSub(args[0]).Replace("\r", "").Split('\n');
                string ret = "";
                for(int i = 0; i < s.Length; i++)
                {
                    string o = ExecSub(args[1].Replace("@s", s[i]).Replace("@i", i.ToString()));
                    if(o.Length > 0) ret += o + Environment.NewLine;
                }
                return ret;
            } },

            {"foreachIf", (args)=>{
                string[] s = ExecSub(args[0]).Replace("\r", "").Split('\n');
                string ret = "";
                for(int i = 0; i < s.Length; i++)
                {
                    if(Cond(ExecSub(args[1].Replace("@s", s[i]))))
                    {
                        string o = ExecSub(args[2].Replace("@s", s[i]).Replace("@i", i.ToString()));
                        if(o.Length > 0) ret += o + Environment.NewLine;
                    }
                }
                return ret;
            } },

            {"func", (args)=>{
                Functions.Add(args[0], args[1]);
                return "";
            } },

            {"f", (args)=>{
                if(!Functions.ContainsKey(args[0])) return "";
                string code = Functions[args[0]];

                for(int i = 1; i < args.Length; i++)
                {
                    code = code.Replace("@#"+i, args[i]);
                }

                return ExecSub(code);
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
                if(Directory.Exists(GetPath(args[0]))){
                    Variables["currDir"] = GetPath(args[0]);
                    return "";
                }
                return $"Папка \"{GetPath(args[0])}\" не найдена.";
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
                return Interaction.InputBox(args[0], args[1], args[2]);
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
                        if(bargs[0] == "-s") bargs[1] = GetPath(bargs[1]);
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

            {"cmd.list", (args)=>{
                return String.Join(Environment.NewLine, Commands.Keys.ToArray());
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

            {"str.isMatch", (args)=>{
                return Regex.IsMatch(args[0], args[1])?"true":"false";
            } },

            {"str.equals", (args)=>{
                return BoolToString(args[0].Equals(args[1]));
            } },

            {"str.indexOf", (args)=>{
                return args[0].IndexOf(args[1]).ToString();
            } },

            {"str.replace", (args)=>{
                return args[0].Replace(args[1], args[2]);
            } },

            {"fs.readFile", (args)=>{
                string p = GetPath(args[0]);
                if(!File.Exists(p)) return $"Файл \"{p}\" не найден.";
                return File.ReadAllText(p);
            } },

            {"fs.writeFile", (args)=>{
                File.WriteAllText(GetPath(args[0], true), args[1]);
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
                Directory.Delete(GetPath(args[0]));
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
                return String.Join(Environment.NewLine, list);
            } },

            {"reg.createKey", (args)=>{
                var key = GetRegistryKey(args[0], true);
                key.CreateSubKey(args[1]);
                key.Close();
                return "";
            } },

            {"reg.write", (args)=>{
                var key = GetRegistryKey(args[0], true);
                key.SetValue(args[1], args[2]);
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
                File.WriteAllText(GetPath(args[1]), text);
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
                    string o = ExecSub(args[1].Replace("@k", kvp.Key).Replace("@v", kvp.Value));
                    if(o.Length > 0) ret += o + Environment.NewLine;
                }
                return ret;
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
                var arr = UserObjects[args[0]];
                var len = ToInt(arr["length"]);

                var newArr = new Dictionary<string, string>();

                for(int i = 0; i < len + 1; i++)
                {
                    if(i < ToInt(args[1])) newArr[i.ToString()] = arr[i.ToString()];
                    else if(i == ToInt(args[1])) newArr[i.ToString()] = args[2];
                    else newArr[i.ToString()] = arr[(i - 1).ToString()];
                }


                newArr.Add("length", (len+1).ToString());

                arr.Clear();
                foreach(var kvp in newArr) arr.Add(kvp.Key, kvp.Value);

                return args[2];
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
                HotKey.SetHotKey(args[0], false, () =>
                {
                    ExecSub(args[1]);
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

            {"http.server", (args)=>{
                ServeFolder(GetPath(args[0]), ToInt(args[1]), false);
                return "";
            } },

            {"proc.list", (args)=>{
                var ps = Process.GetProcesses();
                string s = "";
                foreach(Process p in ps)
                {
                    s += p.ProcessName + Environment.NewLine;
                }
                return s;
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
                if(args.Length > 3) p.StartInfo.Verb = args[3];
                p.Start();
                if(args.Length > 2 && args[2] == "true") p.WaitForExit();
                return (args.Length > 2 && args[2] == "true")?p.StandardOutput.ReadToEnd():"";
            } },

            {"proc.kill", (args)=>{
                var ps = Process.GetProcessesByName(args[0]);
                foreach(Process p in ps)
                {
                    p.Kill();
                }
                return "";
            } },

            {"proc.getInfo", (args)=>{
                string name = GetRandomName();
                var ps = Process.GetProcessesByName(args[0]);
                if(ps.Length == 0) return "not_found";
                var p = ps[0];
                UserObjects.Add(name, new Dictionary<string, string>
                {
                    {"name", p.ProcessName},
                    {"id", p.Id.ToString()},
                    {"fileName", p.MainModule.FileName},
                    {"args", p.StartInfo.Arguments},
                    //TODO: add all process properties
                });
                return name;
            } },

            {"proc.activate", (args)=>{
                var ps = Process.GetProcessesByName(args[0]);
                if(ps.Length > 0)
                {
                    ps[0].StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    SetForegroundWindow(ps[0].MainWindowHandle);
                }
                return "";
            } },

            {"proc.sendKeys", (args)=>{
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
                    r += $"{kvp.Key}: {kvp.Value}{Environment.NewLine}";
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
                else if(args[1] == "color") return $"{el.ForeColor.R},{el.ForeColor.G},{el.ForeColor.B}";
                else if(args[1] == "bgcolor") return $"{el.BackColor.R},{el.BackColor.G},{el.BackColor.B}";

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

                if(args[1] == "click") el.Click += (o,e)=>ExecSub(args[2]);
                if(args[1] == "keydown") el.KeyDown += (o,e)=>ExecSub(args[2].Replace("@alt", BoolToString(e.Alt)).Replace("@ctrl", BoolToString(e.Control)).Replace("@shift", BoolToString(e.Shift)).Replace("@key", e.KeyCode.ToString()));

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

            {"env.get", (args)=>{
                return Environment.GetEnvironmentVariable(args[0], (args.Length>1&&args[1]=="true")?EnvironmentVariableTarget.Machine:EnvironmentVariableTarget.User);
            } },

            {"env.set", (args)=>{
                Environment.SetEnvironmentVariable(args[0], args[1], (args.Length>2&&args[2]=="true")?EnvironmentVariableTarget.Machine:EnvironmentVariableTarget.User);
                return "";
            } }
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
            { "varOperator", "@" },
            { "execOperator", "&" },
            { "mathOperator", "%" },
            { "doNotParseOperator", "~" },
        };

        public static Dictionary<string, string> Variables = new Dictionary<string, string>()
        {
            { "weshDir", WeshDir },
            { "weshPath", WeshPath },
            { "currDir", Environment.CurrentDirectory },
            { "null", WeshNull },
            { "error", "" },
            { "modulesDir", WeshDir + "\\wesh-modules" },
            { "modulesUrl", "https://nekit270.ch/wesh-modules/MODULE_NAME.weshm" },
        };

        public static List<string> Constants = new List<string>()
        {
            "weshPath", "weshDir", "null"
        };

        public static Dictionary<string, Dictionary<string, string>> UserObjects = new Dictionary<string, Dictionary<string, string>>();
        public static Dictionary<string, Control> Controls = new Dictionary<string, Control>();
        public static Dictionary<string, string> Functions = new Dictionary<string, string>();

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
                if (Variables.ContainsKey(sp[0]) && UserObjects.ContainsKey(Variables[sp[0]]) && UserObjects[Variables[sp[0]]].ContainsKey(sp[1]))
                {
                    return UserObjects[Variables[sp[0]]][sp[1]];
                }
                else
                {
                    return WeshNull;
                }
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
            if (Constants.Contains(name)) throw new Exception(name + " является константой.");
            if (name.Contains('.'))
            {
                string[] sp = name.Split('.');
                if (!Variables.ContainsKey(sp[0]) || !UserObjects.ContainsKey(Variables[sp[0]])) throw new Exception("Объект не найден.");
                if (UserObjects[Variables[sp[0]]].ContainsKey(sp[1]))
                {
                    UserObjects[Variables[sp[0]]][sp[1]] = value;
                }
                else
                {
                    UserObjects[Variables[sp[0]]].Add(sp[1], value);
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

        private static string GetPath(string p)
        {
            return GetPath(p, false);
        }

        private static string GetPath(string p, bool f)
        {
            if (p.Length == 0) p = ".";
            Environment.CurrentDirectory = Variables["currDir"];
            string path = Path.IsPathRooted(p) ? p : Path.GetFullPath(p);
            if (f || File.Exists(path) || Directory.Exists(path)) return path;
            return null;
        }

        private static string GetRandomName()
        {
            string cs = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            char[] chars = new char[30];
            for(int i = 0; i < chars.Length; i++)
            {
                chars[i] = cs[rand.Next(cs.Length)];
            }

            return new String(chars);
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

        private static string GetRequest(string url)
        {
            var req = WebRequest.Create(url);

            var res = (HttpWebResponse)req.GetResponse();
            var str = new StreamReader(res.GetResponseStream()).ReadToEnd();
            res.Close();

            return str;
        }

        private static string PostRequest(string url, string data)
        {
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

        [-v]            Выводит версию WESH.
        [-h]            Выводит справочное сообщение.
        [-c <команда>]  Выполняет указанную команду.
        [-f <файл>]     Выполняет команды из файла в пакетном режиме.
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
            server.Prefixes.Add($"http://127.0.0.1:{port}/");
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
                    buffer = Encoding.UTF8.GetBytes($"<!doctype html><center><h1>404 Not Found</h1><p>File {req.RawUrl} not found on this server.</p></center>");
                }
                res.ContentLength64 = buffer.Length;
                output.Write(buffer, 0, buffer.Length);
                output.Flush();
                output.Close();

                if (!q) Console.WriteLine($"[{req.RemoteEndPoint}] {req.RawUrl}\t{res.StatusCode}");
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
                return Int32.Parse(s);
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
                return Eval.JScriptEvaluate(expr, Microsoft.JScript.Vsa.VsaEngine.CreateEngine()).ToString();
            }
            catch (Exception)
            {
                return expr;
            }
            
        }

        private static bool Cond(string expr)
        {
            return StringToBool(EvalJS(expr));
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

        public static string Exec(string cmd)
        {
            List<string> args = new List<string>();

            if (cmd == null || cmd.Trim().Length == 0) return "";

            cmd = cmd.Replace("$SM", GetLangEl("cmdDelim"));

            Regex rgx = new Regex(@"[^\\]{1}" + GetLangEl("cmdDelim"));
            MatchCollection mc = rgx.Matches(cmd);
            if (mc.Count > 0)
            {
                foreach (Match m in mc)
                {
                    cmd = cmd.Replace(m.Value, "\x01");
                }

                string r = "";
                foreach (string c in cmd.Split('\x01'))
                {
                    if (c.Trim().Length > 0 && c.Trim() != "\x01") r += Exec(c) + Environment.NewLine;
                }
                return r;
            }

            cmd = cmd.Trim().Replace(GetLangEl("codeBlockEnd"), GetLangEl("codeBlockStart")).Replace("\\"+ GetLangEl("codeBlockStart"), "$Q3");

            cmd = Regex.Replace(cmd, GetLangEl("codeBlockStart")+@"([^"+ GetLangEl("codeBlockStart") + @"]*)"+ GetLangEl("codeBlockStart"), new MatchEvaluator((m) =>
            {
                return m.Groups[1].Value.Replace("\\"+ GetLangEl("cmdDelim"), GetLangEl("cmdDelim")).Replace(GetLangEl("q1"), "$Q1").Replace(GetLangEl("q2"), "$Q2").Replace(" ", "$SP").Replace(GetLangEl("argsDelim"), "$CM").Replace(GetLangEl("varOperator"), "$AT").Replace(GetLangEl("execOperator"), "$AM").Replace(GetLangEl("mathOperator"), "$PR");
            }));
            cmd = Regex.Replace(cmd, GetLangEl("q1")+@"([^" + GetLangEl("q1") + @"]*)" + GetLangEl("q1"), new MatchEvaluator((m) => {
                return m.Groups[1].Value.Replace(GetLangEl("q2"), "$Q2").Replace(" ", "$SP").Replace(GetLangEl("argsDelim"), "$CM");
            }));
            cmd = Regex.Replace(cmd, GetLangEl("q2") + @"([^"+ GetLangEl("q2") +"]*)" + GetLangEl("q2"), new MatchEvaluator((m) =>
            {
                return m.Groups[1].Value.Replace(GetLangEl("q1"), "$Q1").Replace(" ", "$SP").Replace(GetLangEl("argsDelim"), "$CM");
            }));

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
                string arg = args[j];

                if (arg.Trim().Length < 2) continue;

                arg = arg.Replace("$SP", " ");
                arg = arg.Replace("$CM", GetLangEl("argsDelim"));
                arg = arg.Replace("$Q1", GetLangEl("q1"));
                arg = arg.Replace("$Q2", GetLangEl("q2"));
                arg = arg.Replace("$Q3", GetLangEl("codeBlockStart"));
                arg = arg.Replace("$PR", GetLangEl("mathOperator"));

                if (arg.StartsWith(GetLangEl("doNotParseOperator")))
                {
                    arg = arg.Substring(1);
                    args[j] = arg;
                    continue;
                }

                MatchCollection vMatches = new Regex(GetLangEl("varOperator") + @"[a-zA-Z0-9_"+ GetLangEl("objectDelim") + "]+").Matches(arg);
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

                if (arg[0] == Lang["execOperator"][0] && arg[1] != Lang["operatorBlockStart"][0])
                {
                    arg = Exec(arg.Substring(1));
                }

                MatchCollection mMatches = new Regex(GetLangEl("mathOperator") + GetLangEl("operatorBlockStart") + "[^" + GetLangEl("operatorBlockEnd") + "]+" + GetLangEl("operatorBlockEnd")).Matches(arg);
                if (mMatches.Count > 0)
                {
                    foreach (Match mm in mMatches)
                    {
                        string val = mm.Value;
                        val = val.Replace(Lang["mathOperator"] + Lang["operatorBlockStart"], "").Replace(Lang["operatorBlockEnd"], "");
                        arg = arg.Replace(mm.Value, EvalJS(val));
                    }
                }

                if (arg[0] == Lang["mathOperator"][0] && arg[1] != Lang["operatorBlockStart"][0])
                {
                    arg = EvalJS(arg.Substring(1));
                }

                arg = arg.Replace("$AM", Lang["execOperator"]).Replace("$AT", Lang["varOperator"]);

                args[j] = arg;
            }

            if (!Commands.ContainsKey(cmd))
            {
                if(args.Count > 0 && args[0] == "=")
                {
                    SetVariable(cmd, args[1]);
                    return "";
                }
                string msg = $"ОШИБКА: Команда {cmd} не найдена.";
                Variables["error"] = msg;
                return msg;
            }

            string ret = "";
            try
            {
                ret = Commands[cmd](args.ToArray());
            }catch (Exception ex)
            {
                ret = "ОШИБКА: "+ex.Message;
            }
            return ret;
        }

        public static string ExecSub(string cmd)
        {
            cmd = Regex.Replace(cmd, @"^ "+ GetLangEl("cmdDelim"), "");
            cmd = Regex.Replace(cmd, @"^ \\"+ GetLangEl("cmdDelim"), "");
            cmd = Regex.Replace(cmd, GetLangEl("cmdDelim")+@"\s+\\"+ GetLangEl("codeBlockStart"), "\\"+GetLangEl("codeBlockStart"));
            cmd = cmd.Replace("\n", "").Replace(Lang["argsDelim"] + " \\" + Lang["codeBlockStart"] + " " + Lang["cmdDelim"], Lang["argsDelim"] + " \\" + Lang["codeBlockStart"] + " ").Replace(Lang["cmdDelim"], "$SM");
            return Exec(cmd);
        }

        public static string ExecScript(string rawCode)
        {
            string output = "";
            rawCode = rawCode.Replace("\r\n", "\n");
            rawCode = rawCode.Replace("\r", "\n");
            rawCode = rawCode.Replace(" " + GetLangEl("cmdDelim") + "\n", " \\" + GetLangEl("cmdDelim") + " ");
            string[] code = rawCode.Split('\n');
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