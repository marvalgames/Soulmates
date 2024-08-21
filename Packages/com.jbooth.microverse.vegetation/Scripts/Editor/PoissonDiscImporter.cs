//////////////////////////////////////////////////////
// MicroVerse
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////

using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System.IO;

// Allows you to read/write any texture format from unity as if it's a native format.
namespace JBooth.MicroVerseCore
{
    [ScriptedImporter(1, "poissondisk")]
    public class PoissonDiscImporter : ScriptedImporter
    {
        public static void Write(Texture2D r, string path, bool mips, bool linear)
        {
            using (var bw = new BinaryWriter(File.Open(path + ".poissondisk", FileMode.OpenOrCreate)))
            {
                bw.Write(0);
                bw.Write((int)r.format);
                bw.Write(r.width);
                bw.Write(r.height);
                bw.Write(mips);
                bw.Write(linear);

                var bytes = r.GetRawTextureData();
                bw.Write(bytes.Length);
                bw.Write(r.GetRawTextureData());
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            using (var r = new BinaryReader(File.Open(ctx.assetPath, FileMode.Open)))
            {
                int version = r.ReadInt32();
                if (version != 0)
                {
                    Debug.LogError("Version mismatch in poissondisk aseset");
                    return;
                }
                TextureFormat format = (TextureFormat)r.ReadInt32();

                int width = r.ReadInt32();
                int height = r.ReadInt32();
                bool mips = r.ReadBoolean();

 
                bool linear = r.ReadBoolean();
                int length = r.ReadInt32();

                byte[] bytes = r.ReadBytes(length);
                var tex = new Texture2D(width, height, format, mips, linear);
                tex.LoadRawTextureData(bytes);
                tex.Apply();
                ctx.AddObjectToAsset("main obj", tex);
                ctx.SetMainObject(tex);
            }

        }
    }
}