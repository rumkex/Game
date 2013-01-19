using System;
using System.Collections.Generic;
using System.IO;
using Calcifer.Engine.Scenery;
using Calcifer.Utilities.Logging;
using System.Xml.Serialization;
using System.Linq;

namespace ImportTool
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Log.Output[LogLevel.Any] = (level, message) => Console.WriteLine("[{0}] {1}", level, message);
            Log.Verbosity = LogLevel.Any;
            var p = new MainClass();
            p.Run(args);
        }

        void Run(string[] args)
        {
            var argStack = new Stack<string>(args.Reverse());
            if (argStack.Count < 1)
            {
                Console.WriteLine("No command specified. Use 'help' for list of available commands");
                return;
            }
	        try
	        {
                while (argStack.Count > 0)
                {
                    var sw = argStack.Pop();
                    switch (sw)
                    {
                        case "help":
                            ShowHelp();
                            break;
                        case "-c":
                        case "convert":
                            Convert(argStack.Pop());
                            break;
                        case "-a":
                        case "append":
                            Append(argStack.Pop());
                            break;
                        case "-o":
                        case "output":
                            Write(argStack.Pop());
                            break;
                    }
                }
	        }
	        catch (IndexOutOfRangeException)
	        {
		        Console.WriteLine("Not enough arguments.");
		        return;
	        }
	        //catch (Exception ex)
	        //{
		    //    Log.WriteLine(LogLevel.Fatal, ex.Message);
	        //}
        }

        private Map map;

        private void Write(string filename)
        {
            if (map == null)
            {
                Log.WriteLine(LogLevel.Error, "Nothing to output");
                return;
            }
            var serializer = new XmlSerializer(typeof(Map));
            using (var f = File.Open(filename, FileMode.Create))
                serializer.Serialize(f, map);
            map = null;
        }

        private void Append(string filename)
        {
            if (map == null)
            {
                Log.WriteLine(LogLevel.Error, "Nothing to append to");
                return;
            }
            var parser = new LsaMapParser();
            var appendMap = parser.Load(filename);
            foreach (var asset in appendMap.Assets) if (!map.Assets.Contains(asset.Name))  map.Assets.Add(asset);
            foreach (var def in appendMap.Definitions) if (!map.Definitions.Contains(def.Name)) map.Definitions.Add(def);
        }

        void Convert(string filename)
        {
            var parser = new LsaMapParser();
            map = parser.Load(filename);
        }

        void ShowHelp()
        {
            Console.WriteLine("convert [input_file] - upgrades LSA-formatted map");
        }

    }
}
