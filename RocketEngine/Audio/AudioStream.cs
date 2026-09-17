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
    public class AudioStream(uint source)
    {
        public bool IsPlaying => AL.GetSourceState(source).HasFlag(ALSourceState.Playing);
        public bool IsStopped => AL.GetSourceState(source).HasFlag(ALSourceState.Stopped);
        public bool IsPaused => AL.GetSourceState(source).HasFlag(ALSourceState.Paused);

        public void Play(Vector3 position, float gain = 1,float pitch = 1, bool looping = false, bool relative = false)
        {
            AL.SetSource(source, ALSourceFloat.Gain, gain);
            AL.SetSource(source, ALSourceFloat.Gain, gain);
            AL.SetSource(source, ALSourceFloat.Pitch, pitch);
            AL.SetSource(source, ALSourceVector3.Position, position.X,position.Y,position.Z);
            AL.SetSource(source, ALSourceInteger.Looping, looping);
            AL.SetSource(source, ALSourceInteger.SourceRelative, relative);

            AL.SourcePlay(source);
        }

        public void Stop() => AL.SourceStop(source);
        public void Pause() => AL.SourcePause(source);
        public void Rewind() => AL.SourceRewind(source);
    }
}
