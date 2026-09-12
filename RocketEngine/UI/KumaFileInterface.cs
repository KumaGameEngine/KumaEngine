using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace KumaEngine.UI
{
    public class KumaFileInterface : FileInterface
    {
        Dictionary<nint, FileStream> pinnedStreams = new();

        public override void Close(nint file)
        {
            if (!pinnedStreams.ContainsKey(file))
                throw new FileNotFoundException("Could not find file stream to close");

            pinnedStreams.Remove(file);
        }

        public override ulong Length(nint file)
        {
            if (!pinnedStreams.ContainsKey(file))
                throw new FileNotFoundException("Could not find file stream to read");

            return (ulong)pinnedStreams[file].Length;
        }

        public override string LoadFile(string path)
        {
            return File.ReadAllText(AssetRetriver.FetchAssetPath(AssetKind.UI, path));
        }

        public override nint Open(string path)
        {
            var fs = File.OpenRead(AssetRetriver.FetchAssetPath(AssetKind.UI, path));
            var pin = (nint)Random.Shared.Next(0,int.MaxValue);

            pinnedStreams.Add(pin,fs);

            return pin;
        }

        public override ulong Read(ulong size, nint file, out byte[] bytes)
        {
            if (!pinnedStreams.ContainsKey(file))
                throw new FileNotFoundException("Could not find file stream to read");

            bytes = new byte[size];
            return (ulong)pinnedStreams[file].Read(bytes, 0, bytes.Length);
        }

        public override bool Seek(nint file, uint offset, int origin)
        {
            if (!pinnedStreams.TryGetValue(file, out var stream))
                return false;

            try
            {
                stream.Seek((int)offset, (SeekOrigin)origin);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override ulong Tell(nint file)
        {
            if (!pinnedStreams.ContainsKey(file))
                throw new FileNotFoundException("Could not find file stream to read");

            return (ulong)pinnedStreams[file].Position;
        }
    }
}
