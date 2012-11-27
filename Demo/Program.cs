using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using Calcifer.Engine.Components;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Data;
using Calcifer.Engine.Content.Pipeline;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Physics;
using Calcifer.Engine.Scenegraph;
using Calcifer.Engine.Scenery;
using Calcifer.Engine.Scripting;
using Calcifer.Utilities.Logging;
using ComponentKit;
using ComponentKit.Model;
using Demo.Import;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Demo
{
    public class Game: GameWindow
    {
        private ContentManager content;
        private Scenegraph scenegraph;
        private IEntityRecordCollection entities;

        private Camera camera;

        private PhysicsService physicsService;
        private LuaService luaService;

        private LinkedList<IUpdateable> updateables;

        private Game() : base(800, 600)
        {
            updateables = new LinkedList<IUpdateable>();
            entities = EntityRegistry.Current;
            camera = new Camera(Matrix4.Identity);
        }

        static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            Log.Output[LogLevel.Any] = (level, s) => Console.WriteLine("({0})[{1}] {2}", (DateTime.Now - startTime).TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture), level, s);
            var g = new Game();
            InitGL();
            g.Run();
        }

        private static void InitGL()
        {
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.VertexArray);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            content = new ContentManager();
            // TODO: change how loaders/providers are added?
            content.Providers.Add(new FilesystemProvider(".."));
            content.Loaders.Add(new GdiTextureLoader(content));
            content.Loaders.Add(new ObjLoader(content));
            content.Loaders.Add(new SmdLoader(content));
            content.Loaders.Add(new SmdAnimLoader(content));
            
            var serializer = new XmlSerializer(typeof(Map));
            var map = (Map) serializer.Deserialize(content.Providers.LoadAsset("assets/test.map.xml"));
            var sceneBuilder = new SceneBuilder(content);
            sceneBuilder.CreateFromMap(map);

            physicsService = new PhysicsService();
            luaService = new LuaService();
            entities.SetTrigger(c => c is IUpdateable, (sender, args) => RegisterUpdateables(args.Components));
            entities.Synchronize();

            scenegraph = new Scenegraph();
            foreach (var entity in entities)
                scenegraph.Builder.AddModelFromEntity(entity);
        }

        private void RegisterUpdateables(IEnumerable<IComponent> components)
        {
            foreach (var component in components)
                if (component.IsOutOfSync)
                    updateables.Remove((IUpdateable) component);
                else
                    updateables.AddLast((IUpdateable) component);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            foreach (var updateable in updateables)
                updateable.Update(e.Time);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            scenegraph.Render(new BaseRenderPass(camera));
            SwapBuffers();
        }
    }
}
