using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Pipeline;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Graphics.Animation;
using Calcifer.Engine.Graphics.Buffers;
using Calcifer.Engine.Graphics.Primitives;
using Calcifer.Utilities.Logging;
using OpenTK;
using OpenTK.Graphics;

namespace Demo.Import
{
    public class SmdLoader: ResourceLoader<MeshData>
    {
        public SmdLoader(ContentManager parent) : base(parent)
        {
        }

        public override MeshData Load(string name, Stream stream)
        {
            var parser = new TextParser(new StreamReader(stream)) {DetectQuotes = true};
            var header = parser.NextLine();
            Debug.Assert(header == "version 1", "header == \"version 1\"");

            var geometry = new List<Geometry>();

            string chunk;
            while((chunk = parser.NextLine()) != null)
                switch(chunk)
                {
                    case "triangles":
                        LoadTriangles(name, parser, geometry);
                        break;
                    default:
                        Log.WriteLine(LogLevel.Warning, "ignoring chunk '{0}' at line {1}", chunk, parser.LineNumber);
                        while (parser.NextLine() != "end") { }
                        break;
                }
            return new MeshData(geometry);
        }

        private void LoadTriangles(string name, TextParser parser, List<Geometry> geometry)
        {
            var builders = new Dictionary<string, GeometryBuilder>();
            while (parser.NextLine() != "end")
            {
                var material = parser.ReadLine();
                if (!builders.ContainsKey(material))
                {
                    builders.Add(material, new GeometryBuilder());
                    builders[material].Material = GenerateMaterial(name, material);
                }
                if (string.IsNullOrEmpty(material)) throw new ParserException("Unexpected end of SMD file", parser.LineNumber);
                var v1 = Transform(ReadVertex(parser));
                var v2 = Transform(ReadVertex(parser));
                var v3 = Transform(ReadVertex(parser));
                builders[material].Add(v1, v2, v3);
            }
            parser.ReadLine();
            geometry.AddRange(builders.Select(builder => builder.Value.ToGeometry()));
        }

        private Material GenerateMaterial(string name, string material)
        {
            // THIS IS A NON-STANDARD IMPLEMENTATION
            // materials shouldn't be generated this way,
            // since studiomdl files are supposed to be acommpanied by material config
            // TODO: SMD material definition files
            var path = Path.GetDirectoryName(name);
            if (path == null) return null;
            var diffuse = Parent.Load<Texture>(Path.Combine(path, material + ".png"));
            return new Material(name) {
				Ambient = new Color4(0.2f, 0.2f, 0.2f, 1.0f), 
				Diffuse = new Color4(0.6f, 0.6f, 0.6f, 1.0f), 
				Specular = new Color4(0.2f, 0.2f, 0.2f, 1.0f), 
				Shininess = 10.0f, DiffuseMap = diffuse};
        }

        //Matrix to convert model to proper coordinates
        private static Matrix4 rot = Matrix4.CreateRotationX(-MathHelper.PiOver2) * Matrix4.CreateRotationY(MathHelper.PiOver2);

        private SkinnedVertex Transform(SkinnedVertex v)
        {
            Vector3 res;
            Vector3.Transform(ref v.Position, ref rot, out res);
            v.Position = res;
            Vector3.Transform(ref v.Normal, ref rot, out res);
            v.Normal = res;
            return v;
        }

        private SkinnedVertex ReadVertex(TextParser parser)
        {
            var baseBone = parser.ReadInt();
            var pos = parser.ReadVector3();
            var normal = parser.ReadVector3();
            var uv = parser.ReadVector2();
            var wcount = parser.ReadInt();
            var wsum = 0.0f;
            var list = new Dictionary<int, float>();
            for (var i = 0; i < wcount; i++)
            {
                var bone = parser.ReadInt();
                var weight = parser.ReadFloat();
                wsum += weight;
                if (weight > 0) list.Add(bone, weight);
            }
            // base bone usually has no influence, but we should account for it nevertheless
            if (list.ContainsKey(baseBone))
                list[baseBone] += 1 - wsum; 
            else
                list.Add(baseBone, 1 - wsum);

            var weights = list.OrderByDescending(p => p.Value).Take(4).ToList();
            var sum = weights.Sum(p => p.Value);
            var normWeights = weights.Select(p => new KeyValuePair<int, float>(p.Key, p.Value/sum)).ToDictionary(p => p.Key, p => p.Value);
            return new SkinnedVertex(pos, normal, uv, normWeights.Keys.ToList(), normWeights.Values.ToList());
        }


        public override bool Supports(string name, Stream stream)
        {
            var header = Encoding.ASCII.GetBytes("version 1");
            var data = new byte[header.Length];
            stream.Read(data, 0, data.Length);
            return header.SequenceEqual(data);
        }
    }

    public class SmdAnimLoader : ResourceLoader<AnimationData>
    {
        public SmdAnimLoader(ContentManager parent) : base(parent)
        {
        }

        public override AnimationData Load(string name, Stream stream)
        {
            var parser = new TextParser(new StreamReader(stream)) { DetectQuotes = true };
            var header = parser.NextLine();
            Debug.Assert(header == "version 1", "header == \"version 1\"");

            var geometry = new List<Geometry>();

            string chunk;
            while ((chunk = parser.NextLine()) != null)
                switch (chunk)
                {
                    case "triangles":
                        LoadTriangles(name, parser, geometry);
                        break;
                    default:
                        Log.WriteLine(LogLevel.Warning, "ignoring chunk '{0}' at line {1}", chunk, parser.LineNumber);
                        while (parser.NextLine() != "end") { }
                        break;
                }
            // TODO: SMD settings (FPS, etc.) in external file
            return new AnimationData(Path.GetFileNameWithoutExtension(name), 60.0f);
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
