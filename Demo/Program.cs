using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using OpenTK.Input;

namespace Demo
{
    public class Game: GameWindow
    {
        private ContentManager content;
        private Scenegraph scenegraph;
        private IEntityRecordCollection entities;

        private PhysicsService physicsService;
        private LuaService luaService;
        private StateService stateService;

        private LinkedList<IUpdateable> updateables;

        private Game() : base(800, 600)
        {
            updateables = new LinkedList<IUpdateable>();
            entities = EntityRegistry.Current;
        }

        static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            Log.Output[LogLevel.Any] = (level, s) => Console.WriteLine("({0})[{1}] {2}", (DateTime.Now - startTime).TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture), level, s);
			Console.SetBufferSize(120,300);
			Console.SetWindowSize(120,40);
			if (!Directory.Exists("../assets"))
			{
				Log.WriteLine(LogLevel.Fatal, "'../assets' directory not found");
				return;
			} 
			if (!Directory.Exists("../scripts"))
			{
				Log.WriteLine(LogLevel.Fatal, "'../scripts' directory not found");
				return;
			}
			if (!File.Exists("../assets/test.map.xml"))
			{
				Log.WriteLine(LogLevel.Fatal, "'../assets/test.map.xml' not found. ImportTool wasn't ran during build?");
				return;
			}
            var g = new Game();
            g.Run(60);
        }

        private void InitGL()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.VertexArray);
            GL.Enable(EnableCap.IndexArray);
			GL.Enable(EnableCap.Light0);
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
            var map = (Map) serializer.Deserialize(content.Providers.LoadAsset("assets/test.map.xml"));
            var sceneBuilder = new SceneBuilder(content);
            sceneBuilder.CreateFromMap(map);

            physicsService = new PhysicsService();
            luaService = new LuaService();
            stateService = new StateService();

            var viewer = Entity.Create("viewer", new CameraComponent(), new TransformComponent(), new KeyboardControllerComponent());
            viewer.GetComponent<TransformComponent>().Translation = new Vector3(-5f, 1f, 2f);

            entities.SetTrigger(c => c is IUpdateable, (sender, args) => RegisterUpdateables(args.Components));
            entities.SetTrigger(c => c is ISaveable, (sender, args) => stateService.Synchronize(args.Components));
            entities.SetTrigger(c => c is PhysicsComponent, (sender, args) => physicsService.Synchronize(args.Components));
            entities.SetTrigger(c => c is LuaComponent, (sender, args) => luaService.Synchronize(args.Components));
            entities.Synchronize();

            scenegraph = new Scenegraph();
            scenegraph.Builder.AddLight(5.0f * Vector3.UnitZ, new Vector4(0.6f * Vector3.One, 1.0f), new Vector4(0.6f * Vector3.One, 1.0f), new Vector4(0.7f * Vector3.One, 1.0f));
            foreach (var entity in entities)
            {
                scenegraph.Builder.AddModelFromEntity(entity);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(ClientSize);
            GL.MatrixMode(MatrixMode.Projection);
            var m = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, (float)ClientSize.Width / ClientSize.Height, 0.1f, 100f);
            GL.LoadMatrix(ref m);
            GL.MatrixMode(MatrixMode.Modelview);
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
			if (Keyboard[Key.P]) RenderHints<bool>.SetHint("debugPhysics", !RenderHints<bool>.GetHint("debugPhysics"));

            if (Keyboard[Key.F5]) stateService.SaveState();
            if (Keyboard[Key.F6]) stateService.RestoreState();

	        physicsService.Update(e.Time);
            var current = updateables.First;
            while (current != null) // cannot use enumeration since triggers may fire and modify the collection
            {
                current.Value.Update(e.Time);
                current = current.Next;
            }
        }

        private const int FpsSamples = 100;
        private Queue<double> fpsQueue = new Queue<double>(FpsSamples);

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            fpsQueue.Enqueue(1 / e.Time);
            if (fpsQueue.Count >= FpsSamples) fpsQueue.Dequeue();
            var fps = fpsQueue.Sum() / fpsQueue.Count;
            Title = fps.ToString(CultureInfo.InvariantCulture);

            base.OnRenderFrame(e);
            scenegraph.Render(new BaseRenderPass(CameraComponent.Current));
            SwapBuffers();
        }
    }
}
