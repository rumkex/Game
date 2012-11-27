﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Pipeline;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Graphics.Buffers;
using Calcifer.Engine.Graphics.Primitives;
using Calcifer.Engine.Physics;
using Calcifer.Utilities;
using Calcifer.Utilities.Logging;
using OpenTK;
using OpenTK.Graphics;

namespace Demo.Import
{
    internal struct Triple
    {
        public int X, Y, Z;

        public Triple(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class ObjLoader : ResourceLoader<CompositeResource>
    {
        public ObjLoader(ContentManager parent)
            : base(parent)
        {
        }

        public override CompositeResource Load(string name, Stream stream)
        {
            var parser = new TextParser(new StreamReader(stream)) {DetectQuotes = true};

            var submeshes = new Dictionary<string, GeometryBuilder>();
            var materials = new Dictionary<string, Material>();
            var currentMaterial = "";
            var points = new List<Vector3>(new[] { Vector3.Zero });
            var normals = new List<Vector3>(new[] { Vector3.Zero });
            var texCoords = new List<Vector2>(new[] { Vector2.Zero });
            while (parser.NextLine() != null)
            {
                if (parser.CurrentLine.StartsWith("#")) continue;
                var op = parser.ReadString();
                switch (op)
                {
                    case "p":
                        // Point
                        break;

                    case "v":
                        // Vertex
                        points.Add(parser.ReadVector3());
                        break;

                    case "vt":
                        // TexCoord
                        texCoords.Add(parser.ReadVector2());
                        break;

                    case "vn":
                        // Normal
                        normals.Add(parser.ReadVector3());
                        break;

                    case "f":
                        // Face
                        if (currentMaterial == null) break;
                        var f = ParseFace(parser.CurrentLine.Split(' '));
                        submeshes[currentMaterial].Add(f.Select(t => new SkinnedVertex(points[t.X], normals[t.Y], texCoords[t.Z])).ToList(), normals.Count <= 1);
                        break;

                    case "o":
                    case "g":
                        if (currentMaterial != null) submeshes[currentMaterial].NextGeometry();
                        break;
                        
                    case "usemtl":
                        currentMaterial = parser.ReadString();
                        if (!submeshes.ContainsKey(currentMaterial))
                            submeshes.Add(currentMaterial, new GeometryBuilder {Material = materials[currentMaterial]});
                        else
                            submeshes[currentMaterial].NextGeometry();
                        break;
                    case "mtllib":
                        var mtlFile = Path.Combine(Path.GetDirectoryName(name) ?? "", parser.CurrentLine.Substring("mtllib ".Length));
                        foreach (var mat in LoadMaterialsFrom(mtlFile))
                            materials.Add(mat.Name, mat);
                        break;
                    case "s":
                        Log.WriteLine(LogLevel.Info, "Smoothing groups aren't supported");
                        break;
                    default:
                        throw new ParserException("Unknown OBJ operator: " + op, parser.LineNumber);
                }
            }
            var glist = new List<Geometry>();
            var plist = new List<Geometry>();
            foreach (var builder in submeshes)
            {
                if (!builder.Key.EndsWith("level.png")) glist.AddRange(builder.Value.GetGeometry());
                else plist.AddRange(builder.Value.GetGeometry());
            }
            var mesh = new MeshData(glist);
            var pmesh = new PhysicsData(plist);
            return new CompositeResource(mesh, pmesh);
        }
        
        private IEnumerable<Material> LoadMaterialsFrom(string mtlFile)
        {
            var stream = Parent.Providers.LoadAsset(mtlFile);
            var parser = new TextParser(new StreamReader(stream));
            Material current = null;
            while (parser.NextLine() != null)
            {
                if (parser.CurrentLine.StartsWith("#")) continue;
                if (parser.CurrentLine.Length == 0) continue;
                var op = parser.ReadString();
                switch (op)
                {
                    case "newmtl":
                        if (current != null) yield return current;
                        var name = parser.ReadString();
                        current = new Material(name);
                        break;
                    case "Ka":
                        var ka = parser.ReadVector3();
                        if (current != null) current.Ambient = new Color4(ka.X, ka.Y, ka.Z, 1.0f);
                        break;
                    case "Kd":
                        var kd = parser.ReadVector3();
                        if (current != null) current.Diffuse = new Color4(kd.X, kd.Y, kd.Z, 1.0f);
                        break;
                    case "Ks":
                        var ks = parser.ReadVector3();
                        if (current != null) current.Diffuse = new Color4(ks.X, ks.Y, ks.Z, 1.0f);
                        break;
                    case "map_Kd":
                        var diffuseName = parser.ReadString();
                        if (current != null) current.DiffuseMap = Parent.Load<Texture>(Path.Combine(Path.GetDirectoryName(mtlFile) ?? "", diffuseName));
                        break;
                }
            }
            if (current != null) yield return current;
        }

        public override bool Supports(string name, Stream stream)
        {
            return name.EndsWith(".obj");
        }

        private static IEnumerable<Triple> ParseFace(IList<string> indices)
        {
            for (var i = 1; i < indices.Count - 1; i++)
            {
                var parameters = indices[i].Split('/');
                var vert = int.Parse(parameters[0]);
                // Texcoords and normals are optional in .obj files
                var tex = parameters[1] != "" ? int.Parse(parameters[1]) : 0;
                var norm = parameters[2] != "" ? int.Parse(parameters[2]) : 0;
                yield return new Triple(vert, norm, tex);
            }
        }
    }
}
