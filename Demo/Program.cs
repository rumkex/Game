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
            camera = new Camera(Matrix4.LookAt(5f * Vector3.One, Vector3.Zero, Vector3.UnitY));
        }

        static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            Log.Output[LogLevel.Any] = (level, s) => Console.WriteLine("({0})[{1}] {2}", (DateTime.Now - startTime).TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture), level, s);
            var g = new Game();
            g.Run();
        }

        private void InitGL()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Gequal);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.VertexArray);
            GL.Enable(EnableCap.IndexArray);

            GL.MatrixMode(MatrixMode.Projection);
            var m = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, (float)ClientSize.Width / ClientSize.Height,1f, 100f);
            GL.LoadMatrix(ref m);
            GL.MatrixMode(MatrixMode.Modelview);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitGL();
            content = new ContentManager();
            // TODO: change how loaders/providers are added?
            content.Providers.Add(new FilesystemProvider(".."));
            content.Loaders.Add(new GdiTextureLoader(content));
            content.Loaders.Add(new ObjLoader(content));
            content.Loaders.Add(new SmdLoader(content));
            content.Loaders.Add(new SmdAnimLoader(content));
            
            var serializer = new XmlSerializer(typeof(Map));
            var map = (Map) serializer.Deserialize(content.Providers.LoadAsset("assets/subset.xml"));
            var sceneBuilder = new SceneBuilder(content);
            sceneBuilder.CreateFromMap(map);

            physicsService = new PhysicsService();
            luaService = new LuaService();

            entities.SetTrigger(c => c is IUpdateable, (sender, args) => RegisterUpdateables(args.Components));
            entities.Synchronize();

            scenegraph = new Scenegraph();
            scenegraph.Builder.AddLight(3.0f * Vector3.One, new Vector4(0.6f * Vector3.One, 1.0f), new Vector4(0.6f * Vector3.One, 1.0f), new Vector4(0.7f * Vector3.One, 1.0f));
            foreach (var entity in entities)
            {
                scenegraph.Builder.AddModelFromEntity(entity);
            }
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
            var current = updateables.First;
            while (current != null) // cannot use enumeration since triggers may fire and modify the collection
            {
                current.Value.Update(e.Time);
                current = current.Next;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            scenegraph.Render(new BaseRenderPass(camera));
            SwapBuffers();
        }
    }
}
