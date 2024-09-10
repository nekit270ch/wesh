using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Globalization;

namespace wesh
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            if(!Directory.Exists(WESH.Variables["modulesDir"]))
            {
                try{
                    Directory.CreateDirectory(WESH.Variables["modulesDir"]);
                }catch(Exception e){
                    Console.WriteLine("ERROR: Failed to create "+WESH.Variables["modulesDir"]+": "+e.Message);
                }
            }

            try{
                using(Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RsScript")){
                    using(StreamReader reader = new StreamReader(stream)){
                        string script = reader.ReadToEnd();
                        WESH.ExecScript(script);
                        Environment.Exit(0);
                    }
                }
            }catch(Exception){}

            if (args.Length == 0)
            {
                while (true)
                {
                    Console.Write("wesh ["+WESH.Variables["currDir"]+"] > ");
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
                            Console.WriteLine("Файл \""+args[1]+"\" не найден.");
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
                            Console.WriteLine("Файл \""+args[1]+"\" не найден.");
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
