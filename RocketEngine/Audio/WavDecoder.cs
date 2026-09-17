using Assimp;
using KumaEngine.OpenAL;
using KumaEngine.OpenAL.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Audio
{
    public static class WavDecoder
    {
        public static AudioStream Decode(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            string chunkId = new string(reader.ReadChars(4));
            if (chunkId != "RIFF") throw new InvalidDataException("Invalid RIFF header");

            reader.ReadInt32();
            string format = new string(reader.ReadChars(4));
            if (format != "WAVE") throw new InvalidDataException("Invalid WAVE format");

            ushort channels = 1;
            int sampleRate = 44100;
            ushort bitsPerSample = 16;
            byte[] pcm = [];

            while (stream.Position < stream.Length)
            {
                string subChunkId = new string(reader.ReadChars(4));
                uint subChunkSize = reader.ReadUInt32();

                if (subChunkId == "fmt ")
                {
                    ushort audioFormat = reader.ReadUInt16();
                    channels = reader.ReadUInt16();
                    sampleRate = reader.ReadInt32();
                    reader.ReadUInt32();
                    reader.ReadUInt16();
                    bitsPerSample = reader.ReadUInt16();

                    if (subChunkSize > 16) stream.Seek(subChunkSize - 16, SeekOrigin.Current);
                }
                else if (subChunkId == "data")
                {
                    pcm = reader.ReadBytes((int)subChunkSize);
                    break;
                }
                else stream.Seek(subChunkSize, SeekOrigin.Current);
            }

            uint buffer = AL.GenBuffer();
            AL.BufferData<byte>(buffer, GetAudioFormat(channels,bitsPerSample), pcm, sampleRate);

            uint source = AL.GenSource();

            AL.SetSource(source, ALSourceInteger.Buffer, (int)buffer);

            return new(source);
        }

        static ALFormat GetAudioFormat(ushort channels, ushort bitsPerSample)
        {
            var format = bitsPerSample switch
            {
                8 => ALFormat.Mono8,
                _ => ALFormat.Mono16,
            };

            if (channels >= 2) format += channels;

            return format;
        }
    }
}
