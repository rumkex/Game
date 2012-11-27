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
                    e.Add(BuildComponent(componentEntry.Value));
            }
        }

        private IComponent BuildComponent(ComponentDefinition def)
        {
            switch (def.Type)
            {
                case "transform":
                    return BuildTransformComponent(def);
                case "mesh":
                    return BuildMeshComponent(def);
                case "animation":
                    return BuildAnimationComponent(def);
                case "physics":
                    return BuildPhysicsComponent(def);
                case "luaScript":
                    return BuildLuaComponent(def);
                case "luaStorage":
                    return BuildStorageComponent(def);
                case "health":
                    return BuildHealthComponent(def);
                default:
                    throw new Exception("Unknown component type: " + def.Type);
            }
        }

        private IComponent BuildHealthComponent(ComponentDefinition def)
        {
            return new HealthComponent(int.Parse(def["hp"] ?? "100"));
        }

        private IComponent BuildMeshComponent(ComponentDefinition def)
        {
            var meshData = LoadAsset<MeshData>(map.Assets[def["meshData"]]);
            return meshData;
        }

        private IComponent BuildAnimationComponent(ComponentDefinition def)
        {
            var restPose = LoadAsset<AnimationData>(map.Assets[def["restPose"]]).Frames[0];
            var type = def["controllerType"];
            var c = new BlendAnimationController(restPose);
            foreach (var animName in def["animations"].Split(';'))
                c.AddAnimation(LoadAsset<AnimationData>(map.Assets[animName]));
            return c;
        }

        private IComponent BuildPhysicsComponent(ComponentDefinition def)
        {
            bool isStatic = Convert.ToBoolean(def["static"] ?? "false");
            var type = def["type"];
            Shape shape;
            switch (type)
            {
                case "trimesh":
                    var physData = LoadAsset<PhysicsData>(map.Assets[def["physData"]]);
                    var compound = physData.Shapes.Select(subShape => 
                        new Octree(subShape.Positions, subShape.Triangles)).Select(octree => 
                            new CompoundShape.TransformedShape(new TriangleMeshShape(octree), JMatrix.Identity, JVector.Zero)
                            ).ToList();
                    shape = new CompoundShape(compound);
                    break;
                case "hull":
                    physData = LoadAsset<PhysicsData>(map.Assets[def["physData"]]);
                    compound = physData.Shapes.Select(subShape => 
                        new CompoundShape.TransformedShape(new ConvexHullShape(subShape.Positions), JMatrix.Identity, JVector.Zero)
                        ).ToList();
                    shape = new CompoundShape(compound);
                    break;
                case "sphere":
                    shape = new SphereShape(float.Parse(def["radius"], CultureInfo.InvariantCulture));
                    break;
                case "box":
                    var d = def["size"].ConvertToVector();
                    shape = new BoxShape(d.ToJVector());
                    break;
                case "capsule":
                    shape = new CapsuleShape(float.Parse(def["height"], CultureInfo.InvariantCulture), float.Parse(def["radius"], CultureInfo.InvariantCulture));
                    break;
                default:
                    throw new Exception("Unknown shape: " + type);
            }
            return new PhysicsComponent(shape, isStatic);
        }

        private IComponent BuildLuaComponent(ComponentDefinition def)
        {
            return new LuaComponent(def["source"] ?? File.ReadAllText(def["sourceRef"]));
        }

        private IComponent BuildStorageComponent(ComponentDefinition def)
        {
            return new LuaStorageComponent(def["nodes"].Split(';'));
        }

        private IComponent BuildTransformComponent(ComponentDefinition def)
        {
            var r = def["rotation"].ConvertToVector();
            var c = new TransformComponent
                        {
                            Translation = def["translation"].ConvertToVector(),
                            Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, r.X)*
                                       Quaternion.FromAxisAngle(Vector3.UnitY, r.Y)*
                                       Quaternion.FromAxisAngle(Vector3.UnitZ, r.Z),
                            Scale = def["scale"].ConvertToVector()
                        };
            return c;
        }

        private T LoadAsset<T>(AssetReference r) where T: class, IResource
        {
            return r.Composite ? content.Load<CompositeResource>(r.Source).OfType<T>().First() : content.Load<T>(r.Source);
        }
    }
}