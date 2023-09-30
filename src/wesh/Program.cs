using System;
using System.IO;
using System.Linq;

namespace wesh
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if(!Directory.Exists(WESH.Variables["modulesDir"]))
            {
                Directory.CreateDirectory(WESH.Variables["modulesDir"]);
            }

            if (args.Length == 0)
            {
                while (true)
                {
                    Console.Write($"wesh [{WESH.Variables["currDir"]}] > ");
                    Console.WriteLine(WESH.Exec(Console.ReadLine()));
                }
            }

            switch (args[0])
            {
                case "-c":
                    {
                        Console.WriteLine(WESH.Exec(String.Join(" ", args.Skip(1))));
                        break;
                    }
                case "-f":
                    {
                        string fileName = WESH.GetPath(args[1]);

                        if (fileName == null)
                        {
                            Console.WriteLine($"Файл \"{args[1]}\" не найден.");
                            Environment.Exit(1);
                        }

                        WESH.SetVariable("scriptArgs", WESH.CreateArray(args.Skip(2).ToArray()));
                        WESH.SetVariable("scriptPath", fileName);
                        WESH.SetVariable("scriptDir", Path.GetDirectoryName(fileName));

                        WESH.ExecScript(File.ReadAllText(fileName));
                        break;
                    }
                case "-p":
                    {
                        string fileName = WESH.GetPath(args[1]);

                        if (fileName == null)
                        {
                            Console.WriteLine($"Файл \"{args[1]}\" не найден.");
                            Environment.Exit(1);
                        }

                        Console.Write(String.Join(Environment.NewLine, WESH.PrepareScript(File.ReadAllText(fileName))));
                        break;
                    }
                case "-v":
                    {
                        Console.WriteLine(WESH.VersionStr);
                        break;
                    }
                case "-h":
                    {
                        WESH.HelpMessage();
                        break;
                    }
            }
        }
    }
}
