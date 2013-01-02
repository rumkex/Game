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
        private string baseDir = "../";
        private string baseName;

        public LsaMapParser()
        {
        }

        public Map Load(string mapPath)
        {            
            builder = new MapBuilder();
            baseName = Path.GetFileNameWithoutExtension(mapPath);
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
                builder.BeginComponent("luaStorage");
                builder.AddParameter("nodes", sb.ToString());
                builder.EndComponent();
            } 
            parser.ReadLine();
            var type = parser.ReadLine();
            switch (type)
            {
                case ObjectType.Actor:
                    ParseActor(info);
                    break;
                case ObjectType.Static:
                    ParseProp(info);
                    break;
                case ObjectType.Projectile:
                    ParseProjectile(info);
                    break;
                case ObjectType.Particles:
                    ParseParticles(info);
                    break;
                case ObjectType.Sensor:
                    ParseSensor(info);
                    break;
                case ObjectType.PushingBox:
                    ParsePushableBox(info);
                    break;
                case ObjectType.MovingBox:
                    ParseMovableBox(info);
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
                builder.BeginComponent("luaScript");
                builder.AddParameter("sourceRef", Path.Combine(baseDir, scriptName));
                builder.EndComponent();
            } else if (len != 0)
            {
                var s = new StringBuilder(); 
                while (s.Length < len)
                    s.Append(parser.ReadLine() + "\n");
                source = s.ToString();

                builder.BeginComponent("luaScript");
                builder.AddParameter("source", source);
                builder.EndComponent();
            }

            if (source.Length <= len)
                parser.ReadLine();

            builder.EndEntity();
        }
        
        private void ParseMovableBox(BaseInfo info)
		{
			Log.WriteLine(LogLevel.Warning, "Movable boxes not implemented");
            // TODO: Do something with box entities
            parser.ReadLine(); // #box
            var dim = parser.ReadVector3();
            parser.ReadLine(); // #box offset
            var off = parser.ReadVector3();
        }

        private void ParsePushableBox(BaseInfo info)
		{
            parser.ReadLine(); // #box
            var dim = parser.ReadVector3();
            parser.ReadLine(); // #box offset
			var off = parser.ReadVector3(); // TODO: implement offset

			var fn = Path.GetFileNameWithoutExtension(info.AssetName);
			builder.AddAsset(fn + ".mesh", "MeshData", true, info.AssetName);
			builder.BeginComponent("mesh");
			builder.AddParameter("meshData", fn + ".mesh");
			builder.EndComponent();

			builder.BeginComponent("physics");
			builder.AddParameter("type", "box");
			builder.AddParameter("size", dim.ConvertToString());
			builder.EndComponent();
			builder.BeginComponent("crate");
			builder.EndComponent();
        }

        private void ParseSensor(BaseInfo info)
		{
            parser.ReadLine(); // #box
			var dim = parser.ReadVector3();
			builder.BeginComponent("physics");
			builder.AddParameter("type", "box");
			builder.AddParameter("size", dim.ConvertToString());
			builder.EndComponent();
			builder.BeginComponent("sensor");
			builder.EndComponent();
        }

        private void ParseProp(BaseInfo info)
        {
            // No additional info here
            var fn = Path.GetFileNameWithoutExtension(info.AssetName);
            builder.AddAsset(fn + ".mesh", "MeshData", true, info.AssetName);
            builder.AddAsset(fn + ".hull", "PhysicsData", true, info.AssetName);
            builder.BeginComponent("mesh");
            builder.AddParameter("meshData", fn + ".mesh");
            builder.EndComponent();
            if (fn != baseName)
            {
                builder.BeginComponent("physics");                
                builder.AddParameter("type", "hull");
                builder.AddParameter("physData", fn + ".hull");
                builder.EndComponent();
            } else
            {
                builder.BeginComponent("physics");            
                builder.AddParameter("type", "trimesh");
                builder.AddParameter("physData", fn + ".hull");
                builder.AddParameter("static", "true");
                builder.EndComponent();
            }
        }

        private void ParseActor(BaseInfo info)
        {
            parser.ReadLine();
            var r = parser.ReadFloat(); // capsule radius 
            parser.ReadLine();
            var h = parser.ReadFloat(); // capsule height
            builder.BeginComponent("physics");            
            builder.AddParameter("type", "capsule");
            builder.AddParameter("radius", r.ToString(CultureInfo.InvariantCulture));
            builder.AddParameter("height", h.ToString(CultureInfo.InvariantCulture));
            builder.EndComponent();

            // TODO: Animations stuff
            var fn = Path.GetFileNameWithoutExtension(info.AssetName);
            var path = Path.GetDirectoryName(info.AssetName);
            builder.AddAsset(fn + ".mesh", "MeshData", false, info.AssetName);
            builder.AddAsset(fn + ".rest", "AnimationData", false, info.AssetName);
            builder.BeginComponent("mesh");
            builder.AddParameter("meshData", fn + ".mesh");
            builder.EndComponent();

            builder.BeginComponent("animation");
            builder.AddParameter("restPose", fn + ".rest");
            builder.AddParameter("controllerType", "blend");
            var sb = new StringBuilder();
            var animsPath = Path.Combine(baseDir, Path.Combine(path, "anims"));
            foreach (var anim in Directory.GetFiles(animsPath, "*.smd"))
            {
                var animname = Path.GetFileNameWithoutExtension(anim);
                var alias = fn + ".animation." + animname;
                builder.AddAsset(alias, "AnimationData", false, anim.Remove(0, baseDir.Length));
                
                if (sb.Length != 0)
                    sb.Append(";");
                sb.Append(alias);
            }
            builder.AddParameter("animations", sb.ToString());
            builder.EndComponent();

            builder.BeginComponent("health");
            builder.AddParameter("hp", "100");
            builder.EndComponent();
        }

        private void ParseProjectile(BaseInfo info)
        {
            parser.ReadLine();
            var r = parser.ReadFloat(); // projectile radius
            builder.BeginComponent("physics");            
            builder.AddParameter("type", "sphere");
            builder.AddParameter("radius", r.ToString(CultureInfo.InvariantCulture));
            builder.EndComponent();
        }

        private void ParseParticles(BaseInfo info)
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
                builder.BeginComponent("transform");
                builder.AddParameter("translation", t.ConvertToString());
                builder.AddParameter("rotation", r.ConvertToString());
                builder.AddParameter("scale", s.ConvertToString());
                builder.EndComponent();
            }

            builder.BeginEntity(nodeName);
            builder.BeginComponent("transform");
            builder.AddParameter("translation", t.ConvertToString());
            builder.AddParameter("rotation", r.ConvertToString());
            builder.AddParameter("scale", s.ConvertToString());
            builder.EndComponent();
            builder.EndEntity();
        }
    }
}
