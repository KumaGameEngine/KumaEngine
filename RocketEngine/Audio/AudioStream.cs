using KumaEngine.OpenAL;
using KumaEngine.OpenAL.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace KumaEngine.Audio
{
    public class AudioStream(ALFormat format, int sampleRate, BinaryReader stream, long pcmStart, uint pcmLenght) : IDisposable
    {
        const int BufferCount = 8;
        const int ChunkMs = 100;

        readonly uint source = AL.GenSource();
        readonly uint[] buffers = new uint[BufferCount];
        readonly uint[] one = new uint[1];
        readonly byte[] chunk = new byte[BytesPerFrame(format) * (sampleRate * ChunkMs / 1000)];
        byte[]? tail;

        bool buffersGenerated;
        long remaining;
        volatile bool stopRequested;
        volatile bool rewindRequested;
        Thread? thread;

        static int BytesPerFrame(ALFormat f) => f switch
        {
            ALFormat.Mono8 => 1,
            ALFormat.Mono16 => 2,
            ALFormat.Stereo8 => 2,
            ALFormat.Stereo16 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(f), "Unsupported audio format")
        };

        public bool IsPlaying => AL.GetSourceState(source).HasFlag(ALSourceState.Playing);
        public bool IsStopped => AL.GetSourceState(source).HasFlag(ALSourceState.Stopped);
        public bool IsPaused => AL.GetSourceState(source).HasFlag(ALSourceState.Paused);

        public bool IsLooping
        {
            get => AL.GetSourceInteger(source, ALSourceInteger.Looping) != 0;
            set => AL.SetSource(source, ALSourceInteger.Looping, value);
        }

        public void SetFloat(ALSourceFloat var, float data) =>
            AL.SetSource(source, var, data);

        public void SetVec3(ALSourceVector3 var, Vector3 data) =>
            AL.SetSource(source, var, data.X, data.Y, data.Z);

        public float GetFloat(ALSourceFloat var) =>
            AL.GetSourceFloat(source, var);

        public Vector3 GetVec3(ALSourceVector3 var)
        {
            var (x,y,z) = AL.GetSourceVector3(source, var);

            return new(x,y,z);
        }

        void FillQueue()
        {
            stream.BaseStream.Position = pcmStart;
            remaining = pcmLenght;

            for (int i = 0; i < buffers.Length; i++)
            {
                if (!Refill(buffers[i])) break;
                one[0] = buffers[i];
                AL.SourceQueueBuffers(source, one);
            }
        }

        void DrainQueue()
        {
            AL.SourceStop(source);
            int left = AL.GetSourceInteger(source, ALSourceInteger.BuffersQueued);
            while (left-- > 0) AL.SourceUnqueueBuffers(source, one);
        }

        bool Refill(uint buf)
        {
            if (remaining <= 0)
            {
                if (!IsLooping) return false;
                stream.BaseStream.Position = pcmStart;
                remaining = pcmLenght;
            }

            int want = (int)Math.Min(remaining, chunk.Length);
            if (want <= 0) return false;

            byte[] data = want == chunk.Length ? chunk : (tail ??= new byte[want]);
            stream.BaseStream.ReadExactly(data, 0, want);

            AL.BufferData<byte>(buf, format, data, sampleRate);
            remaining -= want;
            return true;
        }

        void StreamSamples()
        {
            FillQueue();
            AL.SourcePlay(source);

            while (!stopRequested)
            {
                if (rewindRequested)
                {
                    rewindRequested = false;
                    bool wasPaused = AL.GetSourceState(source) == ALSourceState.Paused;

                    DrainQueue();
                    FillQueue();
                    AL.SourcePlay(source);
                    if (wasPaused) AL.SourcePause(source);

                    continue;
                }

                int processed = AL.GetSourceInteger(source, ALSourceInteger.BuffersProcessed);
                while (processed-- > 0)
                {
                    AL.SourceUnqueueBuffers(source, one);
                    if (Refill(one[0]))
                        AL.SourceQueueBuffers(source, one);
                }

                int queued = AL.GetSourceInteger(source, ALSourceInteger.BuffersQueued);
                if (AL.GetSourceState(source) == ALSourceState.Stopped)
                {
                    if (queued > 0) AL.SourcePlay(source);
                    else break;
                }

                Thread.Sleep(10);
            }

            DrainQueue();
        }

        public void Play(Vector3 position, bool relative = false)
        {
            if (thread is { IsAlive: true }) return;

            if (!buffersGenerated) { AL.GenBuffers(buffers); buffersGenerated = true; }

            SetVec3(ALSourceVector3.Position, position);
            AL.SetSource(source, ALSourceInteger.SourceRelative, relative);

            stopRequested = false;
            thread = new Thread(StreamSamples)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = "AudioStream"
            };
            thread.Start();
        }

        public void Stop()
        {
            stopRequested = true;
            thread?.Join();
        }

        public void Pause() => AL.SourcePause(source);
        public void Resume() => AL.SourcePlay(source);
        public void Rewind()
        {
            if (thread is { IsAlive: true }) 
                rewindRequested = true;
        }

        public void Dispose()
        {
            Stop();

            AL.DeleteSource(source);
            if (buffersGenerated) AL.DeleteBuffers(buffers);

            stream.BaseStream.Close();
            stream.Dispose();
        }
    }
}
