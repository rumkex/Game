using Jitter.Dynamics;
using Jitter.LinearMath;
using System;
using System.Collections.Generic;
using System.Threading;
namespace Jitter.Collision.Shapes
{
    public delegate void ShapeUpdatedHandler();

	public abstract class Shape : ISupportMappable
	{
		private struct ClipTriangle
		{
			public JVector n1;
			public JVector n2;
			public JVector n3;
			public int generation;
		}
		internal JMatrix inertia = JMatrix.Identity;
		internal float mass = 1f;
		internal JBBox boundingBox = JBBox.LargeBox;
		internal JVector geomCen = JVector.Zero;
		internal bool providesMaterial;
	    public event ShapeUpdatedHandler ShapeUpdated;
		public bool ProvidesMaterial
		{
			get
			{
				return this.providesMaterial;
			}
		}
		public JMatrix Inertia
		{
			get
			{
				return this.inertia;
			}
			protected set
			{
				this.inertia = value;
			}
		}
		public float Mass
		{
			get
			{
				return this.mass;
			}
			protected set
			{
				this.mass = value;
			}
		}
		public JBBox BoundingBox
		{
			get
			{
				return this.boundingBox;
			}
		}
		public object Tag
		{
			get;
			set;
		}
		public Shape()
		{
			this.providesMaterial = false;
		}

		public virtual Material MaterialAt(JVector point, JVector delta)
		{
			throw new NotSupportedException();
		}

		protected void RaiseShapeUpdated()
		{
			if (this.ShapeUpdated != null)
			{
				this.ShapeUpdated();
			}
		}
		public virtual void MakeHull(ref List<JVector> triangleList, int generationThreshold)
		{
			float num = 0f;
			if (generationThreshold < 0)
			{
				generationThreshold = 4;
			}
			Stack<Shape.ClipTriangle> stack = new Stack<Shape.ClipTriangle>();
			JVector[] array = new JVector[]
			{
				new JVector(-1f, 0f, 0f),
				new JVector(1f, 0f, 0f),
				new JVector(0f, -1f, 0f),
				new JVector(0f, 1f, 0f),
				new JVector(0f, 0f, -1f),
				new JVector(0f, 0f, 1f)
			};
			int[,] array2 = new int[,]
			{

				{
					5,
					1,
					3
				},

				{
					4,
					3,
					1
				},

				{
					3,
					4,
					0
				},

				{
					0,
					5,
					3
				},

				{
					5,
					2,
					1
				},

				{
					4,
					1,
					2
				},

				{
					2,
					0,
					4
				},

				{
					0,
					2,
					5
				}
			};
			for (int i = 0; i < 8; i++)
			{
				stack.Push(new Shape.ClipTriangle
				{
					n1 = array[array2[i, 0]],
					n2 = array[array2[i, 1]],
					n3 = array[array2[i, 2]],
					generation = 0
				});
			}
			List<JVector> list = new List<JVector>();
			while (stack.Count > 0)
			{
				Shape.ClipTriangle clipTriangle = stack.Pop();
				JVector jVector;
				this.SupportMapping(ref clipTriangle.n1, out jVector);
				JVector jVector2;
				this.SupportMapping(ref clipTriangle.n2, out jVector2);
				JVector jVector3;
				this.SupportMapping(ref clipTriangle.n3, out jVector3);
				float val = (jVector2 - jVector).LengthSquared();
				float val2 = (jVector3 - jVector2).LengthSquared();
				float val3 = (jVector - jVector3).LengthSquared();
				if (Math.Max(Math.Max(val, val2), val3) > num && clipTriangle.generation < generationThreshold)
				{
					Shape.ClipTriangle item = default(Shape.ClipTriangle);
					Shape.ClipTriangle item2 = default(Shape.ClipTriangle);
					Shape.ClipTriangle item3 = default(Shape.ClipTriangle);
					Shape.ClipTriangle item4 = default(Shape.ClipTriangle);
					item.generation = clipTriangle.generation + 1;
					item2.generation = clipTriangle.generation + 1;
					item3.generation = clipTriangle.generation + 1;
					item4.generation = clipTriangle.generation + 1;
					item.n1 = clipTriangle.n1;
					item2.n2 = clipTriangle.n2;
					item3.n3 = clipTriangle.n3;
					JVector jVector4 = 0.5f * (clipTriangle.n1 + clipTriangle.n2);
					jVector4.Normalize();
					item.n2 = jVector4;
					item2.n1 = jVector4;
					item4.n3 = jVector4;
					jVector4 = 0.5f * (clipTriangle.n2 + clipTriangle.n3);
					jVector4.Normalize();
					item2.n3 = jVector4;
					item3.n2 = jVector4;
					item4.n1 = jVector4;
					jVector4 = 0.5f * (clipTriangle.n3 + clipTriangle.n1);
					jVector4.Normalize();
					item.n3 = jVector4;
					item3.n1 = jVector4;
					item4.n2 = jVector4;
					stack.Push(item);
					stack.Push(item2);
					stack.Push(item3);
					stack.Push(item4);
				}
				else
				{
					if (((jVector3 - jVector) % (jVector2 - jVector)).LengthSquared() > 1.19209287E-12f)
					{
						triangleList.Add(jVector);
						triangleList.Add(jVector2);
						triangleList.Add(jVector3);
					}
				}
			}
		}
		public virtual void GetBoundingBox(ref JMatrix orientation, out JBBox box)
		{
			JVector zero = JVector.Zero;
			zero.Set(orientation.M11, orientation.M21, orientation.M31);
			this.SupportMapping(ref zero, out zero);
			box.Max.X = orientation.M11 * zero.X + orientation.M21 * zero.Y + orientation.M31 * zero.Z;
			zero.Set(orientation.M12, orientation.M22, orientation.M32);
			this.SupportMapping(ref zero, out zero);
			box.Max.Y = orientation.M12 * zero.X + orientation.M22 * zero.Y + orientation.M32 * zero.Z;
			zero.Set(orientation.M13, orientation.M23, orientation.M33);
			this.SupportMapping(ref zero, out zero);
			box.Max.Z = orientation.M13 * zero.X + orientation.M23 * zero.Y + orientation.M33 * zero.Z;
			zero.Set(-orientation.M11, -orientation.M21, -orientation.M31);
			this.SupportMapping(ref zero, out zero);
			box.Min.X = orientation.M11 * zero.X + orientation.M21 * zero.Y + orientation.M31 * zero.Z;
			zero.Set(-orientation.M12, -orientation.M22, -orientation.M32);
			this.SupportMapping(ref zero, out zero);
			box.Min.Y = orientation.M12 * zero.X + orientation.M22 * zero.Y + orientation.M32 * zero.Z;
			zero.Set(-orientation.M13, -orientation.M23, -orientation.M33);
			this.SupportMapping(ref zero, out zero);
			box.Min.Z = orientation.M13 * zero.X + orientation.M23 * zero.Y + orientation.M33 * zero.Z;
		}
		public virtual void UpdateShape()
		{
			this.GetBoundingBox(ref JMatrix.InternalIdentity, out this.boundingBox);
			this.CalculateMassInertia();
			this.RaiseShapeUpdated();
		}
		public static float CalculateMassInertia(Shape shape, out JVector centerOfMass, out JMatrix inertia)
		{
			float num = 0f;
			centerOfMass = JVector.Zero;
			inertia = JMatrix.Zero;
			if (shape is Multishape)
			{
				throw new ArgumentException("Can't calculate inertia of multishapes.", "shape");
			}
			List<JVector> list = new List<JVector>();
			shape.MakeHull(ref list, 3);
			float num2 = 0.0166666675f;
			float num3 = 0.008333334f;
			JMatrix value = new JMatrix(num2, num3, num3, num3, num2, num3, num3, num3, num2);
			for (int i = 0; i < list.Count; i += 3)
			{
				JVector jVector = list[i];
				JVector jVector2 = list[i + 1];
				JVector jVector3 = list[i + 2];
				JMatrix jMatrix = new JMatrix(jVector.X, jVector2.X, jVector3.X, jVector.Y, jVector2.Y, jVector3.Y, jVector.Z, jVector2.Z, jVector3.Z);
				float num4 = jMatrix.Determinant();
				JMatrix value2 = JMatrix.Multiply(jMatrix * value * JMatrix.Transpose(jMatrix), num4);
				JVector value3 = 0.25f * (list[i] + list[i + 1] + list[i + 2]);
				float num5 = 0.166666672f * num4;
				inertia += value2;
				centerOfMass += num5 * value3;
				num += num5;
			}
			inertia = JMatrix.Multiply(JMatrix.Identity, inertia.Trace()) - inertia;
			centerOfMass *= 1f / num;
			float x = centerOfMass.X;
			float y = centerOfMass.Y;
			float z = centerOfMass.Z;
			JMatrix jMatrix2 = new JMatrix(-num * (y * y + z * z), num * x * y, num * x * z, num * y * x, -num * (z * z + x * x), num * y * z, num * z * x, num * z * y, -num * (x * x + y * y));
			JMatrix.Add(ref inertia, ref jMatrix2, out inertia);
			return num;
		}
		public virtual void CalculateMassInertia()
		{
			this.mass = Shape.CalculateMassInertia(this, out this.geomCen, out this.inertia);
		}
		public abstract void SupportMapping(ref JVector direction, out JVector result);
		public void SupportCenter(out JVector geomCenter)
		{
			geomCenter = this.geomCen;
		}
	}
}
