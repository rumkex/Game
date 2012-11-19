using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Demo.Import;
using OpenTK;

namespace Demo
{
    public class Game: GameWindow
    {
        public Game() : base(800, 600)
        {}

        static void Main(string[] args)
        {
            var test = "\"quoted string\" 123 1.23 \n 132 \n \n \n 15";
            var parser = new TextParser(new StringReader(test)) {DetectQuotes = true, AutoMultiline = true};
            Console.WriteLine("'" + parser.ReadString() + "'");
            Console.WriteLine(parser.ReadVector3());
            Console.WriteLine(parser.ReadInt());
            Console.ReadLine();
            //var g = new Game();
            //g.Run();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
        }
    }
}
