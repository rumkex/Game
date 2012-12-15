using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Pipeline;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Graphics.Animation;
using Calcifer.Engine.Graphics.Primitives;
using Calcifer.Utilities;
using Calcifer.Utilities.Logging;
using OpenTK;

namespace Demo.Import
{
    public class SmdAnimLoader : ResourceLoader<AnimationData>
    {
        public SmdAnimLoader(ContentManager parent) : base(parent)
        {
        }

        public override AnimationData Load(string name, Stream stream)
        {
            var parser = new TextParser(new StreamReader(stream)) { DetectQuotes = true };
            var header = parser.NextLine();
            Debug.Assert(header.StartsWith("version 1"), "header.StartsWith(\"version 1\")");

            var frames = new List<Pose>();
            string chunk;
            while ((chunk = parser.NextLine()) != null)
                switch (chunk)
                {
                    case "nodes":
                        frames.Add(ParseNodes(parser));
                        break;
                    case "skeleton":
                        ParseSkeleton(parser, frames);
                        break;
                    default:
                        Log.WriteLine(LogLevel.Warning, "ignoring chunk '{0}' at line {1}", chunk, parser.LineNumber);
                        while (parser.NextLine() != "end") { }
                        break;
                }
            return new AnimationData(Path.GetFileNameWithoutExtension(name), 60.0f, frames);
        }

        private void ParseSkeleton(TextParser parser, List<Pose> frames)
        {
            while (parser.NextLine() != "end")
            {
                var curFrame = new Pose(frames.Last()); 
                var frameHeader = parser.CurrentLine; // we don't really use it since variable rate animations aren't supported
                for (int i = 0; i < curFrame.BoneCount; i++ )
                {
                    parser.NextLine();
                    var id = parser.ReadInt();
                    var t = parser.ReadVector3();
                    var r = parser.ReadVector3();
                    var q = Quaternion.FromAxisAngle(Vector3.UnitX, r.X) *
                            Quaternion.FromAxisAngle(Vector3.UnitY, r.Y) *
                            Quaternion.FromAxisAngle(Vector3.UnitZ, r.Z);
                    var tr = new Transform(q, t);
                    curFrame.SetTransform(id, tr);
                    frames.Add(curFrame);
                }
            }
        }

        private Pose ParseNodes(TextParser parser)
        {
            var bones = new List<Bone>();
            while (parser.NextLine() != "end")
            {
                var id = parser.ReadInt();
                var name = parser.ReadString();
                var parent = parser.ReadInt();
                bones.Add(new Bone(parent, id, name));
            }
            return new Pose(bones);
        }

        public override bool Supports(string name, Stream stream)
        {
            var header = Encoding.ASCII.GetBytes("version 1");
            var data = new byte[header.Length];
            stream.Read(data, 0, data.Length);
            return header.SequenceEqual(data);
        }
    }
}