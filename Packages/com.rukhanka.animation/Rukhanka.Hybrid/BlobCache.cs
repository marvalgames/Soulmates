#if UNITY_EDITOR

using System;
using System.IO;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class BlobCache: AssetPostprocessor
{
    //  Need to have two separate versions because blobs are different depending on RUKHANKA_DEBUG_INFO
#if RUKHANKA_DEBUG_INFO
    static readonly int BLOB_VERSION = 1;
#else
    static readonly int BLOB_VERSION = 2;
#endif
    public static string GetBlobCacheDirPath() => $"{Environment.CurrentDirectory.Replace('\\', '/')}/Library/Rukhanka.Animation";
    public static string GetAnimationBlobCacheDirPath() => GetBlobCacheDirPath() + "/AnimationCache";
    public static string GetControllerBlobCacheDirPath() => GetBlobCacheDirPath() + "/ControllerCache";
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConstructCachedAnimationBlobPath(AnimationClip animationClip, Avatar avatar)
    {
        var animationHash = BakingUtils.ComputeAnimationHash(animationClip, avatar);
        var avatarName = avatar != null ? avatar.name : "NO_AVATAR";
        var cacheFileName = $"{animationClip.name}_{avatarName}_{animationHash}.blob";
        cacheFileName = string.Join("_", cacheFileName.Split(Path.GetInvalidFileNameChars()));
        var cacheFilePath = $"{GetAnimationBlobCacheDirPath()}/{cacheFileName}";
        return cacheFilePath;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BlobAssetReference<AnimationClipBlob> LoadBakedAnimationFromCache(AnimationClip animationClip, Avatar avatar)
	{
    #if RUKHANKA_NO_BLOB_CACHE
        var dummy = BLOB_VERSION;
        return default;
    #else
		var cachedBlobPath = ConstructCachedAnimationBlobPath(animationClip, avatar);
        if (!File.Exists(cachedBlobPath))
            return default;
        
        BlobAssetReference<AnimationClipBlob>.TryRead(cachedBlobPath, BLOB_VERSION, out var rv);
        return rv;
    #endif
	}
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static void SaveBakedAnimationToCache(AnimationClip animationClip, Avatar avatar, BlobAssetReference<AnimationClipBlob> bar)
	{
    #if !RUKHANKA_NO_BLOB_CACHE
		var cachedBlobPath = ConstructCachedAnimationBlobPath(animationClip, avatar);
        Directory.CreateDirectory(GetAnimationBlobCacheDirPath());
        using (var writer = new StreamBinaryWriter(cachedBlobPath))
        {
            writer.Write(BLOB_VERSION);
            writer.Write(bar);
        }
    #endif
	}
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConstructCachedControllerBlobPath(AnimatorController controller)
    {
        var controllerHash = BakingUtils.ComputeControllerHash(controller);
        var cacheFilePath = $"{GetControllerBlobCacheDirPath()}/{controller.name}_{controllerHash}.blob";
        return cacheFilePath;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BlobAssetReference<ControllerBlob> LoadBakedControllerFromCache(AnimatorController controller)
	{
    #if RUKHANKA_NO_BLOB_CACHE
        return default;
    #else
		var cachedBlobPath = ConstructCachedControllerBlobPath(controller);
        if (!File.Exists(cachedBlobPath))
            return default;
        
        BlobAssetReference<ControllerBlob>.TryRead(cachedBlobPath, BLOB_VERSION, out var rv);
        return rv;
    #endif
	}
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static void SaveBakedControllerToCache(AnimatorController controller, BlobAssetReference<ControllerBlob> bar)
	{
    #if !RUKHANKA_NO_BLOB_CACHE
		var cachedBlobPath = ConstructCachedControllerBlobPath(controller);
        Directory.CreateDirectory(GetControllerBlobCacheDirPath());
        using (var writer = new StreamBinaryWriter(cachedBlobPath))
        {
            writer.Write(BLOB_VERSION);
            writer.Write(bar);
        }
    #endif
	}
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    static UnityEngine.Object[] LoadAllAssetsAtPath(string assetPath)
    {
        var rv = typeof(SceneAsset).Equals(AssetDatabase.GetMainAssetTypeAtPath(assetPath)) ?
            //  "Do not use ReadObjectThreaded on scene objects!" Fix
            new[] { AssetDatabase.LoadMainAssetAtPath(assetPath) } :
            AssetDatabase.LoadAllAssetsAtPath(assetPath);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
    #if !RUKHANKA_NO_BLOB_CACHE
        //  Delete cached file in case of source has changed
        var controllerBlobCachePath = GetControllerBlobCacheDirPath();
        Directory.CreateDirectory(controllerBlobCachePath);
        var cachedControllerBlobPaths = Directory.GetFiles(controllerBlobCachePath);
        
        var animationBlobCachePath = GetAnimationBlobCacheDirPath();
        Directory.CreateDirectory(animationBlobCachePath);
        var cachedAnimationBlobPaths = Directory.GetFiles(animationBlobCachePath);
        
        foreach (var ia in importedAssets)
        {
            var assets = LoadAllAssetsAtPath(ia);
            foreach (var o in assets)
            {
                //  Clips cache
                if (o as AnimationClip)
                {
                    var animationID = BakingUtils.GetAssetID(o);
                    var animationHash = BakingUtils.ComputeAnimationHash(animationID, 0);
                    var cachedFileEnding = $"{animationHash}.blob".Substring(16);
                    foreach (var p in cachedAnimationBlobPaths)
                    {
                        if (p.EndsWith(cachedFileEnding))
                            File.Delete(p);
                    }
                }
                //  Controller cache
                if (o as AnimatorController)
                {
                    var controllerHashHash = BakingUtils.ComputeControllerHash(o as AnimatorController);
                    var cachedFileEnding = $"{controllerHashHash}.blob";
                    foreach (var p in cachedControllerBlobPaths)
                    {
                        if (p.EndsWith(cachedFileEnding))
                        {
                            File.Delete(p);
                            break;
                        }
                    }
                }
            }
        }
    #endif
    }
}
}

#endif

