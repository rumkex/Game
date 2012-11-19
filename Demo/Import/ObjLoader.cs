using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Pipeline;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Graphics.Primitives;
using OpenTK;

namespace Demo.Import
{
	public class ObjLoader: ResourceLoader<CompositeResource>
	{
	    public ObjLoader(ContentManager parent) : base(parent)
	    {
	    }

	    public override CompositeResource Load(string name, Stream stream) {
			var parser = new TextParser(new StreamReader(stream));
			var points = new List<Vector3>();
			var normals = new List<Vector3>();
			var texCoords = new List<Vector2>();
			var tris = new List<Vector3i>();
	        var vertices = new List<SkinnedVertex>();
            while(parser.NextLine() != null)
			{
			    var op = parser.ReadString();
				switch(op) {
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
					tris.AddRange(ParseFace(parser.ReadLine().Split(' '), vertices));
					break;
				}
			}
			
Vector3[] p = points.ToArray();
			Vector2[] tc = texCoords.ToArray();
			Vector3[] n = normals.ToArray();
			Tri[] f = tris.ToArray();
			
			// If there are no specified texcoords or normals, we add a dummy one.
			// That way the Points will have something to refer to.
			if(tc.Length == 0) {
				tc = new Vector2[1];
				tc[0] = new Vector2(0, 0);
			}
			if(n.Length == 0) {
				n = new Vector3[1];
				n[0] = new Vector3(1, 0, 0);
			}
				
			return new MeshData(p, n, tc, f);
		}

	    public override bool Supports(string name, Stream stream)
	    {
	        return name.EndsWith(".obj");
	    }
		
		private static IEnumerable<Vector3i> ParseFace(string[] indices, List<SkinnedVertex> vertices) {
			var p = new SkinnedVertex[indices.Length-1];
			for(int i = 0; i < p.Length; i++) {
                p[i] = ParsePoint(indices[i + 1]);
			}
			return Triangulate(p);
			//return new Face(p);
		}
		
		// Takes an array of points and returns an array of triangles.
		// The points form an arbitrary polygon.
		private static Tri[] Triangulate(int[] ps) {
			List<Tri> ts = new List<Tri>();
			if(ps.Length < 3) {
				throw new Exception("Invalid shape!  Must have >2 points");
			}
			
			Point lastButOne = ps[1];
			Point lastButTwo = ps[0];
			for(int i = 2; i < ps.Length; i++) {
				Tri t = new Tri(lastButTwo, lastButOne, ps[i]);
				lastButOne = ps[i];
				lastButTwo = ps[i-1];
				ts.Add(t);
			}
			return ts.ToArray();
		}
		
		private static SkinnedVertex ParsePoint(string s) {
			string[] parameters = s.Split('/');
			int vert, tex, norm;
			vert = tex = norm = 0;
			vert = int.Parse(parameters[0]) - 1;
			// Texcoords and normals are optional in .obj files
			if(parameters[1] != "") {
				tex = int.Parse(parameters[1]) - 1;
			}
			if(parameters[2] != "") {
				norm = int.Parse(parameters[2]) - 1;
			}
            return new SkinnedVertex(vert, norm, tex);
		}
	}
}
