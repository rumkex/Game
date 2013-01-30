using System.Collections.Generic;
using System.Drawing;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Particles;
using Calcifer.Engine.Particles.Emitters;
using Calcifer.UI;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using QuickFont;

namespace Demo
{
    internal class UIRenderer : Calcifer.UI.IRenderer, Calcifer.Engine.Particles.IRenderer
    {
        private readonly Dictionary<string, object> properties;
        private TextureManager texManager;
        private QFont font;

        private ParticleManager particleManager;
        private IList<Particle> particleList;

        public UIRenderer()
        {
            font = new QFont("../ui/lubalin.ttf", 25, FontStyle.Bold);
            texManager = TextureManager.FromFile("../ui/ui.xml");
            particleManager = new ParticleManager(this);
            var fireEmitter = new LineEmitter
                              {
                                  Start = Vector3.Zero,
                                  End = Vector3.UnitX,
                                  Intensity = 200f,
                                  Lifetime = 2f,
                                  MaxVelocity = -Vector3.UnitY*75f
                              };
            particleManager.AddEmitter(fireEmitter);
            properties = new Dictionary<string, object>();
        }

        public void Begin()
        {
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ColorMaterial);
            GL.ClearStencil(1);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            Shader.Current.Disable();
            var vport = new int[4];
            GL.GetInteger(GetPName.Viewport, vport);
            var ortho = Matrix4.CreateOrthographicOffCenter(vport[0], vport[2], vport[3], vport[1], -1f, 1f);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadMatrix(ref ortho);
        }

        public void End()
        {
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            Shader.Current.Enable();
            GL.Disable(EnableCap.Blend);
            GL.AlphaFunc(AlphaFunction.Gequal, 0.5f);
            GL.Enable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.Lighting);
        }

        public void Render(UIElement element)
        {
            switch (element.Style)
            {
                case "Frame":
                    RenderFrame(element);
                    break;
                case "Button":
                    RenderButton(element);
                    break;
                case "None":
                    break;
                default:
                    texManager.DrawElement("missing", element.Position, element.Size);
                    break;
            }
            properties.Clear();
        }

        private void RenderButton(UIElement element)
        {
            GL.Enable(EnableCap.StencilTest);
            GL.Clear(ClearBufferMask.StencilBufferBit);

            // Stencil mask to prevent leaking
            GL.ColorMask(false, false, false, false);

            GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
            GL.StencilFunc(StencilFunction.Never, 0, 1);
            texManager.Begin();
            texManager.DrawElement("main-background", element.Position, element.Size);
            texManager.End();

            GL.StencilFunc(StencilFunction.Never, 1, 1);
            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Greater, 0f);
            texManager.Begin();
            DrawBorders(element);
            texManager.End();
            GL.Disable(EnableCap.AlphaTest);

            GL.StencilFunc(StencilFunction.Equal, 0, 1);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            GL.ColorMask(true, true, true, true);

            if (element.State == "Selected") // draw fire
            {
                texManager.Begin();
                texManager.DrawElement("main-background", element.Position, element.Size);
                texManager.End();
                particleManager.Update(0.016f);
                DrawFire(element);
            }
            else // draw bright rectangle
            {
                texManager.Begin();
                texManager.DrawElement("main-active", element.Position, element.Size);
                texManager.End();
            }

            var size = font.Measure(element.Text);
            font.Print(element.Text, element.Size, QFontAlignment.Centre,
                       new Vector2(element.Position.X + element.Width / 2f,
                                   element.Position.Y + element.Height / 2f - size.Height / 2f));
            GL.Disable(EnableCap.StencilTest);

            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Gequal, 0.9f);
            texManager.Begin();
            DrawBorders(element);
            texManager.End();
            GL.Disable(EnableCap.AlphaTest);
        }

        private void RenderFrame(UIElement element)
        {
            texManager.Begin();
            texManager.DrawElement("main-background", element.Position, element.Size);
            DrawBorders(element);
            texManager.End();
        }

        private void DrawFire(UIElement element)
        {
            var size = new Size(40, 40);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One); // additive blending
            texManager.Begin();
            var x = element.Position.X - size.Width/2;
            var y = element.Position.Y + element.Height - size.Height/2;
            var width = element.Width;
            for (int i = 0; i < particleList.Count; i++)
            {
                var p = particleList[i];
                if (!p.IsActive) continue;
                texManager.DrawElement("fireParticle", new Point(x + (int)(p.Position.X * width), y + (int)p.Position.Y), size, alpha: p.TTL / 2f);
            }
            texManager.End();
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha); // normal blending
        }
        
        private void DrawBorders(UIElement element)
        {
            texManager.DrawElement("main-borderLight",  // top border
                element.Position + new Size(element.Padding.Left, 0),
                new Size(element.PaddedWidth, element.Padding.Top));
            texManager.DrawElement("main-borderLight", // left border
                element.Position + new Size(0, element.Padding.Top),
                new Size(element.Padding.Left, element.PaddedHeight),
                rotation: RotateFlipType.Rotate270FlipNone);
            texManager.DrawElement("main-borderDark", // bottom border
                element.PaddedPosition + new Size(0, element.PaddedHeight),
                new Size(element.PaddedWidth, element.Padding.Bottom));
            texManager.DrawElement("main-borderDark", // right border
                element.PaddedPosition + new Size(element.PaddedWidth, 0),
                new Size(element.Padding.Right, element.PaddedHeight),
                rotation: RotateFlipType.Rotate270FlipNone);
            texManager.DrawElement("main-cornerTopLeft",
                element.Position,
                new Size(element.Padding.Left, element.Padding.Top));
            texManager.DrawElement("main-cornerTopRight",
                new Point(element.Position.X + element.PaddedWidth + element.Padding.Left, element.Position.Y),
                new Size(element.Padding.Right, element.Padding.Top));
            texManager.DrawElement("main-cornerBottomLeft",
                new Point(element.Position.X, element.Position.Y + element.PaddedHeight + element.Padding.Top),
                new Size(element.Padding.Left, element.Padding.Bottom));
            texManager.DrawElement("main-cornerBottomRight",
                element.PaddedPosition + element.PaddedSize,
                new Size(element.Padding.Right, element.Padding.Bottom));
        }
        
        private T GetProperty<T>(string name)
        {
            return (T)properties[name];
        }

        public void SetProperty<T>(string name, T value)
        {
            properties.Add(name, value);
        }

        public void Update(IList<Particle> particles)
        {
            particleList = particles;
        }
    }
}
