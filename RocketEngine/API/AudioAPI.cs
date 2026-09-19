using Assimp;
using KeraLua;
using KumaEngine.Audio;
using KumaEngine.OpenAL.Source;
using KumaEngine.Rendering;
using KumaEngine.UI;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vulkan;
using Vulkan.Xlib;

namespace KumaEngine.API
{
    public static class AudioAPI
    {
        static Dictionary<string, ALSourceFloat> FloatVariableKeys = new()
        {
            {"pitch", ALSourceFloat.Pitch},
            {"gain", ALSourceFloat.Gain},
            {"minGain", ALSourceFloat.MinGain},
            {"maxGain", ALSourceFloat.MaxGain},
            {"maxDistance", ALSourceFloat.MaxDistance},
            {"rolloff", ALSourceFloat.RolloffFactor},
            {"coneOuterGain", ALSourceFloat.ConeOuterGain},
            {"coneInnerAngle", ALSourceFloat.ConeInnerAngle},
            {"coneOuterAngle", ALSourceFloat.ConeOuterAngle},
            {"referenceDistance", ALSourceFloat.ReferenceDistance},
        };

        static Dictionary<string, ALSourceVector3> Vec3VariableKeys = new()
        {
            {"position", ALSourceVector3.Position},
            {"direction", ALSourceVector3.Direction},
            {"velocity", ALSourceVector3.Velocity},
        };

        public static Dictionary<int, AudioStream> AudioStreamHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("createFromWav",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var d = lua.ToString(1);

                lua.PushAudioStream(
                    WavDecoder.Decode(
                        AssetRetriver.FetchAssetPath(AssetKind.Audio,d + ".wav")
                    )
                );

                return 1;
            })
        ];

        public static void PushAudioStream(this Lua lua, AudioStream stream)
        {
            var handle = 0;

            if (AudioStreamHandles.ContainsValue(stream))
                handle = AudioStreamHandles.First(x => x.Value == stream).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                AudioStreamHandles.Add(handle, stream);
            }

            var MODNAME = "surface." + handle;

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction("play", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);

                var pos = L.ToVec3(1);

                AudioStreamHandles[handle].Play(pos);

                return 1;
            });

            lua.PushSafeCFunction("play2D", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);

                AudioStreamHandles[handle].Play(Vector3.Zero,true);

                return 1;
            });

            lua.PushSafeCFunction("pause", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);

                var pos = L.ToVec3(1);

                AudioStreamHandles[handle].Pause();

                return 1;
            });

            lua.PushSafeCFunction("stop", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);

                var pos = L.ToVec3(1);

                AudioStreamHandles[handle].Stop();

                return 1;
            });

            lua.PushSafeCFunction("rewind", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);

                var pos = L.ToVec3(1);

                AudioStreamHandles[handle].Rewind();

                return 1;
            });

            lua.NewTable();

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                if (FloatVariableKeys.TryGetValue(key,out var sourceflt))
                {
                    L.PushNumber(AudioStreamHandles[handle].GetFloat(sourceflt));
                    return 1;
                }

                if (Vec3VariableKeys.TryGetValue(key, out var sourcevec))
                {
                    L.PushVec3(AudioStreamHandles[handle].GetVec3(sourcevec));
                    return 1;
                }

                switch (key)
                {
                    case "isPlaying": L.PushBoolean(AudioStreamHandles[handle].IsPlaying); return 1;
                    case "isPaused": L.PushBoolean(AudioStreamHandles[handle].IsPaused); return 1;
                    case "isStopped": L.PushBoolean(AudioStreamHandles[handle].IsStopped); return 1;
                    case "isLooping": L.PushBoolean(AudioStreamHandles[handle].IsLooping); return 1;
                    default: L.PushNil(); return 1;
                }
            });

            lua.PushSafeCFunction("__newindex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                if (FloatVariableKeys.TryGetValue(key, out var sourceflt))
                    AudioStreamHandles[handle].SetFloat(sourceflt, (float)L.ToNumber(3));

                if (Vec3VariableKeys.TryGetValue(key, out var sourcevec))
                    AudioStreamHandles[handle].SetVec3(sourcevec,L.ToVec3(3));

                switch (key)
                {
                    case "isLooping": AudioStreamHandles[handle].IsLooping = L.ToBoolean(3); return 1;
                }

                return 0;
            });

            lua.SetMetaTable(-2);
        }

        public static AudioStream ToAudioStream(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (AudioStreamHandles.TryGetValue(val, out var go)) return go;
            throw new Exception("Invalid AudioStream handle");
        }
    }
}
