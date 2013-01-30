using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using Calcifer.Engine.Content;
using Calcifer.Engine.Graphics.Buffers;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Demo
{
    public class TextureManager
    {
        private Texture2D texture;
        [XmlAttribute("texture")]
        public string TextureName { get; set; }
        
        [XmlArray("quads"), XmlArrayItem("quad")]
        public QuadCollection Elements { get; private set; }

        public TextureManager(string name): this()
        {
            TextureName = name;
            texture = ResourceFactory.LoadFile<Texture2D>(name);
        }
        
        private TextureManager()
        {
            Elements = new QuadCollection();
        }

        public void Begin()
        {
            texture.Bind();
            GL.Begin(BeginMode.Quads);
        }

        public void End()
        {
            GL.End();
            texture.Unbind();
        }

        public void DrawElement(string name, Point position, Size size, float depth = 0f, float alpha = 1f, RotateFlipType rotation = RotateFlipType.RotateNoneFlipNone)
        {
            var quad = Elements[name];
            var u1 = quad.X / (float)texture.Width;
            var u2 = (quad.X + quad.Width) / (float)texture.Width;
            var v1 = 1f - quad.Y / (float)texture.Height;
            var v2 = 1f - (quad.Y + quad.Height) / (float)texture.Height;
            var tc = Rotate(u1, v1, u2, v2, rotation);
            GL.Color4(1f, 1f, 1f, alpha);
            GL.TexCoord2(tc[0]);
            GL.Vertex3(position.X, position.Y, depth);
            GL.TexCoord2(tc[1]);
            GL.Vertex3(position.X, position.Y + size.Height, depth);
            GL.TexCoord2(tc[2]);
            GL.Vertex3(position.X + size.Width, position.Y + size.Height, depth);
            GL.TexCoord2(tc[3]);
            GL.Vertex3(position.X + size.Width, position.Y, depth);
        }

        private Vector2[] Rotate(float u1, float v1, float u2, float v2, RotateFlipType rotation)
        {
            var tc = new Vector2[4];
            switch (rotation)
            {
                case RotateFlipType.RotateNoneFlipNone:
                    tc[0] = new Vector2(u1, v1);
                    tc[1] = new Vector2(u1, v2);
                    tc[2] = new Vector2(u2, v2);
                    tc[3] = new Vector2(u2, v1);
                    break;
                case RotateFlipType.Rotate90FlipNone:
                    tc[0] = new Vector2(u1, v2);
                    tc[1] = new Vector2(u2, v2);
                    tc[2] = new Vector2(u2, v1);
                    tc[3] = new Vector2(u1, v1);
                    break;
                case RotateFlipType.Rotate180FlipNone:
                    tc[0] = new Vector2(u2, v2);
                    tc[1] = new Vector2(u2, v1);
                    tc[2] = new Vector2(u1, v1);
                    tc[3] = new Vector2(u1, v2);
                    break;
                case RotateFlipType.Rotate270FlipNone:
                    tc[0] = new Vector2(u2, v1);
                    tc[1] = new Vector2(u1, v1);
                    tc[2] = new Vector2(u1, v2);
                    tc[3] = new Vector2(u2, v2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("rotation");
            }
            return tc;
        }

        public static TextureManager FromFile(string file)
        {
            var sz = new XmlSerializer(typeof (TextureManager));
            var manager = (TextureManager) sz.Deserialize(File.OpenRead(file));
            manager.texture = ResourceFactory.LoadFile<Texture2D>(manager.TextureName);
            return manager;
        }
    }

    public class QuadCollection : KeyedCollection<string, TexQuad>
    {
        protected override string GetKeyForItem(TexQuad item)
        {
            return item.Name;
        }
    }

    public struct TexQuad
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("X")]
        public int X;
        [XmlAttribute("Y")]
        public int Y;
        [XmlAttribute("width")]
        public int Width;
        [XmlAttribute("height")]
        public int Height;
    }
}