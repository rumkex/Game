﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Calcifer.Engine;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Pipeline;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Graphics.Buffers;
using Calcifer.Engine.Physics;
using Calcifer.Utilities;
using Calcifer.Utilities.Logging;
using Demo.Components;
using Jitter.Collision;
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

            var builder = new GeometryBuilder();
            var materials = new Dictionary<string, Material>();
            var currentMaterial = "";
            var points = new List<Vector3>(new[] { Vector3.Zero });
            var normals = new List<Vector3>(new[] { Vector3.Zero });
            var texCoords = new List<Vector2>(new[] { Vector2.Zero });
            while (parser.NextLine() != null)
            {
                if (parser.CurrentLine.StartsWith("#") || parser.CurrentLine.Length == 0) continue;
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
                        var f = ParseFace(parser.CurrentLine.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries));
                        var list = f.Select(t => new SkinnedVertex(points[t.X], normals[t.Y], texCoords[t.Z])).ToList();
                        builder.Add(list, list[0].Normal.LengthSquared < 0.5f);
                        break;

                    case "o":
                    case "g":
                        //builder.NextGeometry();
                        break;
                        
                    case "usemtl":
                        currentMaterial = parser.ReadString();
                        builder.NextGeometry();
                        builder.Material = materials[currentMaterial];
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
			foreach (var g in builder.GetGeometry())
            {
                if (!g.Material.Name.EndsWith("level.png")) glist.Add(g);
                else plist.Add(g);
            }
            RebuildGeometry(glist);
            RebuildGeometry(plist);
            var mesh = new MeshData(glist);
            if (plist.Count > 0)
            {
                var tris = plist[0].Triangles.Select(t => new TriangleVertexIndices(t.X, t.Y, t.Z)).ToList();
                var verts = plist[0].Vertices.Select(v => v.Position.ToJVector()).ToList();
                var physMaterials = plist.Select(g => new Tuple<int, int, TerrainType>(g.Offset / Vector3i.Size, g.Count, SelectMaterial(g.Material.Name))).ToList();
                var pmesh = new PhysicsData(verts, tris);
                return new CompositeResource(mesh, pmesh, new MaterialData(physMaterials));
            }
            return new CompositeResource(mesh);
        }

        private TerrainType SelectMaterial(string name)
        {
            if (name.Contains("water_level.png"))
                return TerrainType.Water;
            if (name.Contains("grass_level.png"))
                return TerrainType.Grass;
            if (name.Contains("dirt_level.png"))
                return TerrainType.Dirt;
            if (name.Contains("snow_level.png"))
                return TerrainType.Snow;
            if (name.Contains("wood_level.png"))
                return TerrainType.Wood;
            if (name.Contains("metal_level.png"))
                return TerrainType.Metal;
            if (name.Contains("ladder_level.png"))
                return TerrainType.Ladder;
            if (name.Contains("obstacle_level.png"))
                return TerrainType.Obstacle;
            return TerrainType.None;
        }

        private void RebuildGeometry(List<Geometry> source)
        {
            var vertindex = new Dictionary<ushort, ushort>();
            var trilist = new List<Vector3i>();
            var vlist = new List<SkinnedVertex>();
            var tcount = 0;
            foreach (var g in source)
            {
                for (var i = 0; i < g.Count; i++)
                {
                    var t = g.Triangles[g.Offset / Vector3i.Size + i];
                    t.X = RebuildVertex(t.X, g.Vertices, vlist, vertindex);
                    t.Y = RebuildVertex(t.Y, g.Vertices, vlist, vertindex);
                    t.Z = RebuildVertex(t.Z, g.Vertices, vlist, vertindex);
                    trilist.Add(t);
                }
                g.Offset = tcount * Vector3i.Size;
                tcount += g.Count;
            }
            var tarray = trilist.ToArray();
            var varray = vlist.ToArray();
            foreach (var g in source)
            {
                g.Triangles = tarray;
                g.Vertices = varray;
            }
        }

        private ushort RebuildVertex(ushort i, SkinnedVertex[] vertices, List<SkinnedVertex> vlist, Dictionary<ushort, ushort> vertindex)
        {
            if (!vertindex.ContainsKey(i))
            {
                vertindex.Add(i, (ushort)vlist.Count);
                vlist.Add(vertices[i]);
            }
            return vertindex[i];
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
                        if (current != null) current.Specular = new Color4(ks.X, ks.Y, ks.Z, 1.0f);
                        break;
                    case "map_Kd":
                        var diffuseName = parser.ReadString();
                        if (current != null) current.DiffuseMap = Parent.Load<Texture2D>(Path.Combine(Path.GetDirectoryName(mtlFile) ?? "", diffuseName));
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
            for (var i = 1; i < indices.Count; i++)
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

    public class MaterialData : IResource
    {
        private readonly List<Tuple<int, int, TerrainType>> materials;

        public MaterialData(List<Tuple<int, int, TerrainType>> physMaterials)
        {
            materials = physMaterials;
        }
        
        public TerrainType GetMaterial(int index)
        {
            var match = materials.FirstOrDefault(t => index >= t.Item1 && index < t.Item1 + t.Item2);
            return match != null ? match.Item3 : TerrainType.None;
        }

        public object Clone()
        {
            return new MaterialData(materials);
        }
    }
}
