using System;
using System.Collections.Generic;
using System.Linq;
using Calcifer.Engine.Components;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Data;
using Calcifer.Engine.Scenegraph;
using ComponentKit;
using ComponentKit.Model;
using Demo.Import;
using OpenTK;

namespace Demo
{
    public class Game: GameWindow
    {
        private ContentManager content;
        private Scenegraph scenegraph;
        private IEntityRecordCollection entities;

        private LinkedList<IUpdateable> updateables;

        private Game() : base(800, 600)
        {
            updateables = new LinkedList<IUpdateable>();
        }

        static void Main(string[] args)
        {
            var g = new Game();
            g.Run();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            content = new ContentManager();
            // TODO: change how loaders/providers are added?
            content.Providers.Add(new FilesystemProvider("../assets"));
            content.Loaders.Add(new ObjLoader(content));
            
            EntityRegistry.Current.SetTrigger(c => c is IUpdateable, (sender, args) => RegisterUpdateables(args.Components));
            Entity.Create("test", new TestComponent());
            EntityRegistry.Current.Synchronize();
            Entity.Find("test").GetComponent<TestComponent>().Test();
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
            SwapBuffers();
        }
    }

    public class TestComponent : Component
    {
        protected override void OnAdded(ComponentStateEventArgs registrationArgs)
        {
            base.OnAdded(registrationArgs);
            Console.WriteLine("Added!");
        }

        public void Test()
        {
            Console.WriteLine("Record = " + Record);
        }
    }
}
