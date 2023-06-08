using System;
using System.IO;

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
                        Console.WriteLine(WESH.Exec(args[1]));
                        break;
                    }
                case "-f":
                    {
                        if (!File.Exists(args[1]))
                        {
                            Console.WriteLine($"Файл \"{args[1]}\" не найден.");
                            Environment.Exit(1);
                        }
                        WESH.ExecScript(File.ReadAllText(args[1]));
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
