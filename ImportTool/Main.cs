using System;
using System.IO;
using Calcifer.Engine.Scenery;
using Calcifer.Utilities.Logging;
using System.Xml.Serialization;

namespace ImportTool
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Log.Output[LogLevel.Any] = (level, message) => Console.WriteLine("[{0}] {1}", level, message);
            Log.Verbosity = LogLevel.Any;
            Log.WriteLine(LogLevel.Info, "Testing log.");
            var p = new MainClass();
            p.Run(args);
        }

        void Run(string[] args)
        {			
            if (args.Length < 1)
            {
                Console.WriteLine("No command specified. Use 'help' for list of available commands");
                return;
            }
            try
            {				
                switch (args [0])
                {
                    case "help":
                        ShowHelp();
                        return;
                    case "-c":
                    case "convert":
                        Convert(args [1]);
                        return;
                }
            } catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }
        }

        void Convert(string filename)
        {
            var parser = new LsaMapParser();
            var map = parser.Load(filename);
            var serializer = new XmlSerializer(typeof(Map));
            using (var f = File.Open(filename + ".xml", FileMode.Create))
                serializer.Serialize(f, map);
        }

        void ShowHelp()
        {
            Console.WriteLine("convert [input_file] - upgrades LSA-formatted map");
        }

    }
}
