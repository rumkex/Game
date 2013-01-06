using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Calcifer.Engine.Components;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Pipeline;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Graphics.Animation;
using Calcifer.Engine.Physics;
using Calcifer.Engine.Scenery;
using Calcifer.Engine.Scripting;
using Calcifer.Utilities;
using ComponentKit;
using ComponentKit.Model;
using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using OpenTK;

namespace Demo
{
    public class SceneBuilder
    {
        private Map map;
        private ContentManager content;

        public SceneBuilder(ContentManager content)
        {
            this.content = content;
        }

        public void CreateFromMap(Map m)
        {
            map = m;
            foreach (var entityEntry in m.Entities)
            {
                var name = entityEntry.Key;
                var e = Entity.Create(name);
                foreach (var componentEntry in entityEntry.Value)
                    BuildComponent(e, componentEntry.Value);
            }
        }

        private void BuildComponent(IEntityRecord e, ComponentDefinition def)
        {
            switch (def.Type)
            {
                case "transform":
                    BuildTransformComponent(def, e);
		            break;
	            case "mesh":
					BuildMeshComponent(def, e);
		            break;
                case "animation":
					BuildAnimationComponent(def, e);
		            break;
                case "physics":
					BuildPhysicsComponent(def, e);
		            break;
                case "luaScript":
					BuildLuaComponent(def, e);
		            break;
                case "luaStorage":
					BuildStorageComponent(def, e);
		            break;
                case "health":
					BuildHealthComponent(def, e);
		            break;
				case "sensor":
					BuildSensorComponent(def, e);
		            break;
				case "crate":
					BuildCrateComponent(def, e);
					break;
				case "motion":
					BuildMotionComponent(def, e);
					break;
                default:
                    throw new Exception("Unknown component type: " + def.Type);
            }
        }

	    private void BuildMotionComponent(ComponentDefinition def, IEntityRecord entityRecord)
	    {
		    entityRecord.Add(new MotionComponent());
	    }

	    private void BuildCrateComponent(ComponentDefinition def, IEntityRecord entityRecord)
	    {
		    entityRecord.Add(new CrateComponent());
	    }

	    private void BuildSensorComponent(ComponentDefinition def, IEntityRecord entityRecord)
	    {
		    entityRecord.Add(new SensorComponent());
	    }

	    private void BuildHealthComponent(ComponentDefinition def, IEntityRecord entityRecord)
        {
            entityRecord.Add(new HealthComponent(int.Parse(def["hp"] ?? "100")));
        }

        private void BuildMeshComponent(ComponentDefinition def, IEntityRecord entityRecord)
        {
            entityRecord.Add(LoadAsset<MeshData>(map.Assets[def["meshData"]]));
        }

        private void BuildAnimationComponent(ComponentDefinition def, IEntityRecord entityRecord)
        {
            var restPose = LoadAsset<AnimationData>(map.Assets[def["restPose"]]).Frames[0];
			var type = def["controllerType"];
			restPose.CalculateWorld();
	        var c = new BlendAnimationController(restPose);
            if (def["animations"] != null)
            foreach (var animName in def["animations"].Split(';'))
                c.AddAnimation(LoadAsset<AnimationData>(map.Assets[animName]));
	        entityRecord.Add(c);
        }

        private void BuildPhysicsComponent(ComponentDefinition def, IEntityRecord entityRecord)
        {
            var type = def["type"];
	        RigidBody body;
            switch (type)
            {
                case "trimesh":
                    var physData = LoadAsset<PhysicsData>(map.Assets[def["physData"]]);
		            var tris = physData.Shapes[0].Triangles.Select(t => new TriangleVertexIndices(t.X, t.Y, t.Z)).ToList();
		            var verts = physData.Shapes[0].Vertices.Select(v => v.Position.ToJVector()).ToList();
		            var materials = physData.Shapes.Select(g => new Tuple<int, int, string>(g.Offset, g.Count, g.Material.Name)).ToList();
		            var octree = new Octree(verts, tris);
		            entityRecord.Add(new TerrainComponent(materials, octree));
		            Shape shape = new TriangleMeshShape(octree);
					body = new RigidBody(shape) {Material = {Restitution = 0f, KineticFriction = 0f}};
		            break;
                case "hull":
                    physData = LoadAsset<PhysicsData>(map.Assets[def["physData"]]);
		            shape = new MinkowskiSumShape(physData.Shapes.Select(
						g => new ConvexHullShape(g.Vertices.Select(v => v.Position.ToJVector()).ToList())
						));
					body = new RigidBody(shape);
                    break;
                case "sphere":
                    shape = new SphereShape(float.Parse(def["radius"], CultureInfo.InvariantCulture));
					body = new RigidBody(shape);
                    break;
                case "box":
                    var d = def["size"].ConvertToVector();
		            shape = new BoxShape(2.0f*d.ToJVector());
					body = new RigidBody(shape) {Position = JVector.Backward*d.Z};
                    break;
                case "capsule":
		            var height = float.Parse(def["height"], CultureInfo.InvariantCulture);
		            var radius = float.Parse(def["radius"], CultureInfo.InvariantCulture);
		            shape = new CapsuleShape(height, radius);
					body = new RigidBody(shape)
						       {
							       Position = JVector.Backward*(0.5f*height + radius),
								   Orientation = JMatrix.CreateRotationX(MathHelper.PiOver2)
						       };
		            break;
                default:
                    throw new Exception("Unknown shape: " + type);
			}
			bool isStatic = Convert.ToBoolean(def["static"] ?? "false");
	        body.IsStatic = isStatic;
            entityRecord.Add(new PhysicsComponent(body));
        }

        private void BuildLuaComponent(ComponentDefinition def, IEntityRecord entityRecord)
        {
            entityRecord.Add(new LuaComponent(def["source"] ?? File.ReadAllText(def["sourceRef"])));
        }

        private void BuildStorageComponent(ComponentDefinition def, IEntityRecord entityRecord)
        {
            entityRecord.Add(new LuaStorageComponent(def["nodes"].Split(';')));
        }

        private void BuildTransformComponent(ComponentDefinition def, IEntityRecord entityRecord)
        {
            var r = def["rotation"].ConvertToVector();
            entityRecord.Add(new TransformComponent
                        {
                            Translation = def["translation"].ConvertToVector(),
                            Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, r.X)*
                                       Quaternion.FromAxisAngle(Vector3.UnitY, r.Y)*
                                       Quaternion.FromAxisAngle(Vector3.UnitZ, r.Z),
                            Scale = def["scale"].ConvertToVector()
                        });
        }

        private T LoadAsset<T>(AssetReference r) where T: class, IResource
        {
            return r.Composite ? content.Load<CompositeResource>(r.Source).OfType<T>().First() : content.Load<T>(r.Source);
        }
    }
}