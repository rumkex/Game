using System;
using System.IO;
using System.Text;
using Calcifer.Engine.Audio;
using Calcifer.Engine.Content;
using Calcifer.Engine.Content.Pipeline;

namespace Demo.Import
{
    sealed class WaveLoader : ResourceLoader<Sound>
    {
        public WaveLoader(ContentManager parent) : base(parent)
        {
        }

        public override bool Supports(string name, Stream stream)
        {
            const int formatOffset = 8;
            const int compressionFormatOffset = 20;
            var buffer = new byte[4];
            stream.Read(buffer, 0, buffer.Length);
            if (Encoding.ASCII.GetString(buffer) != "RIFF") return false;
            stream.Seek(formatOffset, SeekOrigin.Begin);
            stream.Read(buffer, 0, buffer.Length);
            if (Encoding.ASCII.GetString(buffer) != "WAVE") return false;
            // Technically, now we know that it's valid WAVE file, but we don't like compression,
            // so we check for PCM format flag.
            stream.Seek(compressionFormatOffset, SeekOrigin.Begin);
            stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt16(buffer, 0) == 1;
        }

        public override Sound Load(string name, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
