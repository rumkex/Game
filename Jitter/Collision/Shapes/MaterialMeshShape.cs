using Jitter.Dynamics;
using Jitter.LinearMath;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jitter.Collision.Shapes
{
	public class MaterialMeshShape : TriangleMeshShape
	{
		protected List<Material> materials;
		private List<int> indices;
		public MaterialMeshShape(IList<Material> materials, Octree octree) : base(octree)
		{
			if (materials.Count != octree.NumTriangles)
			{
				throw new ArgumentOutOfRangeException("materials", "Materials length and triangle count must match");
			}
			this.materials = materials.ToList<Material>();
			this.providesMaterial = true;
			this.indices = new List<int>(1);
		}
		protected override Multishape CreateWorkingClone()
		{
			return new MaterialMeshShape(materials, octree)
			{
				sphericalExpansion = this.sphericalExpansion
			};
		}
		public override Material MaterialAt(JVector point, JVector delta)
		{
			this.indices.Clear();
			this.octree.GetTrianglesIntersectingRay(this.indices, point, delta);
			return (this.indices.Count == 0) ? new Material() : this.materials[this.indices[0]];
		}
	}
}
