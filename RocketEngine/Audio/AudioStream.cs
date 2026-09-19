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

        public void Play(Vector3 position, bool relative = false)
        {
            SetVec3(ALSourceVector3.Position, position);
            AL.SetSource(source, ALSourceInteger.SourceRelative, relative);

            AL.SourcePlay(source);
        }

        public void Stop() => AL.SourceStop(source);
        public void Pause() => AL.SourcePause(source);
        public void Rewind() => AL.SourceRewind(source);
    }
}
