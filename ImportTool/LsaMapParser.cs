using System.IO;
using System.Linq;
using Calcifer.Engine.Scenery;
using Calcifer.Utilities;
using Calcifer.Utilities.Logging;
using OpenTK;
using System;
using System.Text;
using System.Globalization;

namespace ImportTool
{
    class LsaMapParser
    {
        private struct BaseInfo
        {
            public string Name;
            public string AssetName;
            public Vector3 Translation, Rotation;
        }

        private static class ObjectType
        {
            public const string Particles = "OBJECT_TYPE_PARTICLES";
            public const string Sunlight = "OBJECT_TYPE_SUN_LIGHT";
            public const string Omnilight = "OBJECT_TYPE_OMNILIGHT";
            public const string Spotlight = "OBJECT_TYPE_SPOTLIGHT";
            public const string Actor = "OBJECT_TYPE_CHARACTER";
            public const string Static = "OBJECT_TYPE_STATIC";
            public const string MovingBox = "OBJECT_TYPE_MOVING_BOX";
            public const string PushingBox = "OBJECT_TYPE_PUSHING_BOX";
            public const string Projectile = "OBJECT_TYPE_PROJECTILE";
            public const string Sensor = "OBJECT_TYPE_SENSOR";
        }

        private TextParser parser;
        private MapBuilder builder;
        private string baseDir = "";

        public Map Load(string mapPath)
        {
            mapPath = mapPath.Replace('\\', '/');
            baseDir = Directory.GetParent(Path.GetDirectoryName(mapPath)) + "/";
			if (!File.Exists(mapPath))
			{
				Log.WriteLine(LogLevel.Fatal, "'{0}' was not found!", mapPath);
				return null;
			}
            builder = new MapBuilder();
            var reader = new StreamReader(mapPath);
            parser = new TextParser(reader) {AutoMultiline = true};

            parser.ReadLine(); // all those are for skipping the junk
            var lightCount = parser.ReadInt();
            parser.ReadLine();
            var clipPlanes = parser.ReadVector3(); // clip planes. no idea what they are and what is their purpose
            SetupCamera();
            SetupLight();
            while (parser.NextLine() != null)
                ParseObject();
            reader.Close();
            return builder.GetMap();
        }

        private void SetupCamera()
        {
			Log.WriteLine(LogLevel.Warning, "Camera setup not implemented");
            // TODO: Implement camera
            parser.ReadLine();
            var cameraPosition = parser.ReadVector3();
            var cameraTarget = parser.ReadVector3();
            parser.ReadLine();
            var aspectRatio = parser.ReadFloat();
            parser.ReadLine();
            var fieldOfView = parser.ReadFloat();
        }

        private void SetupLight()
		{
			Log.WriteLine(LogLevel.Warning, "Lights not implemented");
            // TODO: Implement lights
            parser.ReadLine();
            var angleStart = parser.ReadFloat();
            var angleEnd = parser.ReadFloat();
            parser.ReadLine();
            var color = parser.ReadVector3();
        }

        private void ParseObject()
        {
            var info = new BaseInfo();
            parser.ReadLine();
            info.Name = parser.ReadLine();
            parser.ReadLine();
            info.AssetName = parser.ReadLine();
            parser.ReadLine();
            info.Translation = parser.ReadVector3(); // Translation vector of base Transform
            parser.ReadLine();
            info.Rotation = parser.ReadVector3(); // Euler angles of base Transform
            parser.ReadLine();
			info.AssetName = info.AssetName.Replace('\\', '/');

            builder.BeginEntity(info.Name);
			Log.WriteLine(LogLevel.Info, "Parsing {0}...", info.Name);
            var posCount = parser.ReadInt();
            if (posCount > 0)
            {
                // add nodes as separate entities.
                var sb = new StringBuilder();
                for (int i = 0; i < posCount; i++)
                {
                    var nodeName = info.Name + ".node" + i;
                    if (sb.Length != 0)
                        sb.Append(";");
                    sb.Append(nodeName);
                    ParseNode(nodeName, i == 0);
                }
                builder.BeginComponent("WaypointComponent");
                builder.AddParameter("nodes", sb.ToString());
                builder.EndComponent();
            }

            var fn = Path.GetFileNameWithoutExtension(info.AssetName);
            builder.AddAsset(fn + ".mesh", info.AssetName.EndsWith(".obj"), info.AssetName);
            builder.BeginComponent("RenderComponent");
            builder.AddParameter("meshData", fn + ".mesh");
            builder.EndComponent();
            
            parser.ReadLine();
            var type = parser.ReadLine();
            
            if (info.AssetName.EndsWith(".smd") && type != ObjectType.Actor)
            {
                // it's probably an animated object, check for idle animation
                var anim = Directory.GetFiles(baseDir + Path.GetDirectoryName(info.AssetName), "anims/*.smd", SearchOption.AllDirectories).FirstOrDefault();
                if (anim != null)
                {
                    builder.AddAsset(fn + ".rest", false, info.AssetName);
                    builder.AddAsset(fn + ".animation", false, anim.Remove(0, baseDir.Length).Replace('\\', '/'));
                    builder.BeginComponent("SimpleAnimationController");
                    builder.AddParameter("animData", fn + ".animation");
                    builder.AddParameter("restPose", fn + ".rest");
                    builder.EndComponent();
                }
            }

            switch (type)
            {
                case ObjectType.Actor:
                    ParseActor(info);
                    break;
                case ObjectType.Static:
                    ParseProp(info);
                    break;
                case ObjectType.Projectile:
                    ParseProjectile();
                    break;
                case ObjectType.Particles:
                    ParseParticles();
                    break;
                case ObjectType.Sensor:
                    ParseSensor();
                    break;
                case ObjectType.PushingBox:
                    ParsePushableBox();
                    break;
                case ObjectType.MovingBox:
                    ParseMovableBox();
                    break;
                default:
                    Log.WriteLine(LogLevel.Fatal, "unknown LSA object type: " + type);
                    break;
            }
            
            parser.ReadLine();// #script_filename_whatever
            var scriptName = parser.ReadLine();
            parser.ReadLine(); // #script_length
            var len = parser.ReadInt();
            parser.ReadLine();

            string source = "";
            if (scriptName != "none")
            {
                builder.BeginComponent("LuaComponent");
                builder.AddParameter("sourceRef", Path.Combine(baseDir, scriptName));
                builder.EndComponent();
            } else if (len != 0)
            {
                var s = new StringBuilder(); 
                while (s.Length < len)
                    s.Append(parser.ReadLine() + "\n");
                source = s.ToString();

                builder.BeginComponent("LuaComponent");
                builder.AddParameter("source", source);
                builder.EndComponent();
            }

            if (source.Length <= len)
                parser.ReadLine();

            builder.EndEntity();
        }

        private void ParseBox()
        {
            parser.ReadLine(); // #box
            var dim = parser.ReadVector3();
            parser.ReadLine(); // #box offset
            var off = parser.ReadVector3();
            
            builder.BeginComponent("PhysicsComponent");
            builder.AddParameter("type", "box");
            builder.AddParameter("size", dim.ConvertToString());
            builder.AddParameter("offset", off.ConvertToString());
            builder.EndComponent();
        }
        
        private void ParseMovableBox()
		{
            ParseBox();
            builder.BeginComponent("WaypointMovableComponent");
            builder.EndComponent();
        }

        private void ParsePushableBox()
		{
            ParseBox();
            builder.BeginComponent("CrateComponent");
			builder.EndComponent();
        }

        private void ParseSensor()
		{
            parser.ReadLine(); // #box
			var dim = parser.ReadVector3();

            builder.BeginComponent("PhysicsComponent");
			builder.AddParameter("type", "box");
			builder.AddParameter("size", dim.ConvertToString());
			builder.EndComponent();
            builder.BeginComponent("SensorComponent");
			builder.EndComponent();
        }

        private void ParseProp(BaseInfo info)
        {
            // No additional info here
            var fn = Path.GetFileNameWithoutExtension(info.AssetName);
            if (info.AssetName.EndsWith(".obj"))
            {
                bool isLevel = info.AssetName.Contains("levels");
                builder.AddAsset(fn + ".hull", true, info.AssetName);
                builder.BeginComponent("PhysicsComponent");
                builder.AddParameter("static", "true");
                builder.AddParameter("physData", fn + ".hull");
                builder.AddParameter("type", isLevel ? "trimesh" : "hull");
                builder.EndComponent();
                if (isLevel)
                {
                    builder.BeginComponent("TerrainComponent");
                    builder.AddParameter("physData", fn + ".hull");
                    builder.EndComponent();
                }
            }
        }

        private void ParseActor(BaseInfo info)
		{
			parser.ReadLine();
			var h = parser.ReadFloat(); // capsule height
            parser.ReadLine();
            var r = parser.ReadFloat(); // capsule radius 

            builder.BeginComponent("PhysicsComponent");
            builder.AddParameter("type", "capsule");
            builder.AddParameter("radius", r.ToString(CultureInfo.InvariantCulture));
            builder.AddParameter("height", h.ToString(CultureInfo.InvariantCulture));
            builder.EndComponent();

            var fn = Path.GetFileNameWithoutExtension(info.AssetName);
            var path = Path.GetDirectoryName(info.AssetName);
            builder.AddAsset(fn + ".rest", false, info.AssetName);
            builder.BeginComponent("BlendAnimationController");
            builder.AddParameter("restPose", fn + ".rest");
            var sb = new StringBuilder();
            foreach (var anim in Directory.GetFiles(baseDir + path, "anims/*.smd", SearchOption.AllDirectories))
            {
                var animname = Path.GetFileNameWithoutExtension(anim);
                var alias = fn + ".animation." + animname;
                builder.AddAsset(alias, false, anim.Remove(0, baseDir.Length).Replace('\\', '/'));
                
                if (sb.Length != 0)
                    sb.Append(";");
                sb.Append(alias);
            }
            builder.AddParameter("animations", sb.ToString());
			builder.EndComponent();

			builder.BeginComponent("MotionComponent");
			builder.EndComponent();

            builder.BeginComponent("HealthComponent");
            builder.AddParameter("hp", info.Name == "heroe" ? "100": "3");
            builder.EndComponent();

            if (info.Name == "heroe")
            {
                builder.BeginComponent("PlayerStateComponent");
                builder.EndComponent();
            }
        }

        private void ParseProjectile()
        {
            parser.ReadLine();
            var r = parser.ReadFloat(); // projectile radius

            builder.BeginComponent("PhysicsComponent");            
            builder.AddParameter("type", "sphere");
            builder.AddParameter("radius", r.ToString(CultureInfo.InvariantCulture));
            builder.EndComponent();

            builder.BeginComponent("ProjectileComponent");
            builder.EndComponent();
        }

        private void ParseParticles()
		{
			Log.WriteLine(LogLevel.Warning, "Particles not implemented");
            // TODO: Implement particles
            parser.ReadLine();// #source_type
            var sourceType = parser.ReadInt();
            parser.ReadLine();// #texture_name
            var textureName = parser.ReadLine();
            parser.ReadLine();// #vortex        
            var vortex = parser.ReadInt();
            parser.ReadLine();// #glow
            var glow = parser.ReadInt();
            parser.ReadLine();// #speed_cone_angle
            var coneAngle = parser.ReadFloat();
            parser.ReadLine();// #speed
            var speed = parser.ReadFloat();
            parser.ReadLine();// #flow
            var flow = parser.ReadFloat();
            parser.ReadLine();// #acceleration
            var acc = parser.ReadVector3();
            parser.ReadLine();// #rot_speed
            var rotSpeed = parser.ReadFloat();
            parser.ReadLine();// #size
            var size = parser.ReadFloat();
            parser.ReadLine();// #life_time
            var lifetime = parser.ReadFloat();
            parser.ReadLine();// #start_color
            var startColor = parser.ReadVector4();
            parser.ReadLine();// #end_color
            var endColor = parser.ReadVector4();
            parser.ReadLine();// #rectangle_width
            var rwidth = parser.ReadFloat();
            parser.ReadLine();// #rectangle_length
            var rheight = parser.ReadFloat();
            parser.ReadLine();// #disc_inner_radius
            var inRadius = parser.ReadFloat();
            parser.ReadLine();// #disc_outer_radius
            var outRadius = parser.ReadFloat();
            parser.ReadLine(); // #line_length
            var lineLength = parser.ReadFloat();
        }

        private void ParseNode(string nodeName, bool addTransform)
        {
            parser.ReadLine(); // #node id
            parser.ReadLine(); // #position
            var t = parser.ReadVector3();
            parser.ReadLine();
            var r = parser.ReadVector3(); // Euler angles
            parser.ReadLine();
            var s = parser.ReadVector3();
            r = r / 180f * (float)Math.PI; // Convert to radians
            
            if (addTransform)
            {
                builder.BeginComponent("TransformComponent");
                builder.AddParameter("translation", t.ConvertToString());
                builder.AddParameter("rotation", r.ConvertToString());
                builder.AddParameter("scale", s.ConvertToString());
                builder.EndComponent();
            }

            builder.BeginEntity(nodeName);
            builder.BeginComponent("TransformComponent");
            builder.AddParameter("translation", t.ConvertToString());
            builder.AddParameter("rotation", r.ConvertToString());
            builder.AddParameter("scale", s.ConvertToString());
            builder.EndComponent();
            builder.EndEntity();
        }
    }
}
