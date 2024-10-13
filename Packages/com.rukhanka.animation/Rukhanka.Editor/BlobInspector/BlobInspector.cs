using System;
using System.Collections.Generic;
using System.IO;
using Rukhaka.Editor;
using Rukhanka.Hybrid;
using Rukhanka.Toolbox;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
public class BlobInspector : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset menuAsset = default;
    [SerializeField]
    private VisualTreeAsset blobDBPaneAsset = default;
    [SerializeField]
    private VisualTreeAsset blobCachePaneAsset = default;
    [SerializeField]
    private VisualTreeAsset blobEntryAsset = default;
    [SerializeField]
    private VisualTreeAsset blobCacheEntryAsset = default;
    
    VisualElement
        menuElem,
        blobCachePane,
        blobDBPane
        ;
    
    TwoPaneSplitView splitView;
    
    internal enum BlobType
    {
        AnimatorController,
        AnimationClip,
        RigInfo,
        SkinnedMeshInfo,
        AvatarMask,
        Total
    }
    
    internal class BlobAssetInfo
    {
        public BlobType blobType;
        public UnsafeUntypedBlobAssetReference blobAsset;
        public List<Entity> refEntities;
    }
    
    class BlobAssetsSummary
    {
        public int sizeInBytes;
        public int totalCount;
    }
        
    Dictionary<Hash128, BlobAssetInfo> allBlobAssets = new ();
    BlobAssetsSummary blobAssetsSummary;
        
    internal static World currentWorld;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [MenuItem("Window/Rukhanka.Animation/Blob Inspector")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<BlobInspector>();
        wnd.minSize = new Vector2(1000, 400);
        wnd.titleContent = new GUIContent("Rukhanka.Animation Blob Inspector");
    }
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        var root = rootVisualElement;

        splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        root.Add(splitView);
        
        menuElem = menuAsset.Instantiate();
        menuElem.Query<Button>().ForEach((btn) => {btn.RegisterCallback<ClickEvent>(ev => MenuButtonClick(btn));});
        splitView.Add(menuElem);
        
        blobDBPane = blobDBPaneAsset.Instantiate()[0];
        blobCachePane = blobCachePaneAsset.Instantiate()[0];
        splitView.Add(blobDBPane);
        
        MenuButtonClick(menuElem.Q("blobDBBtn") as Button);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void MenuButtonClick(Button btn)
    {
        menuElem.Query<Button>().ForEach((btn) => {btn.style.backgroundColor = new StyleColor(Color.clear);});
        btn.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 1));

        splitView.RemoveAt(1);
        switch (btn.name)
        {
            case "blobDBBtn":
                SwitchToBlobDBPane();
                break;
            case "blobCacheBtn":
                SwitchToBlobCachePane();
                break;
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillBlobDBInifo()
    {
        FillWorldList();
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillWorldList()
    {
        var worldSelector = blobDBPane.Q<DropdownField>("worldSelector");
        worldSelector.RegisterValueChangedCallback((worldName) => { ChangeWorld(worldName.newValue); });
        worldSelector.choices.Clear();
        foreach (var world in World.All)
        {
            worldSelector.choices.Add(world.Name);
        }
        if (worldSelector.index < 0 || worldSelector.index > worldSelector.choices.Count)
            worldSelector.index = 0;
        worldSelector.value = worldSelector.choices[worldSelector.index];
        
        var worldReloadBtn = worldSelector.Q<Button>("worldReloadBtn");
        Action clickLambda = () => { SwitchToBlobDBPane(); ChangeWorld(worldSelector.choices[worldSelector.index]); };
        var clk = new Clickable(clickLambda);
        worldReloadBtn.clickable = clk;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void ChangeWorld(string worldName)
    {
        World world = null;
        for (var i = 0; i < World.All.Count && world == null; ++i)
        {
            if (World.All[i].Name == worldName)
                world = World.All[i];
        }
        
        if (world != null)
        {
            GatherAllBlobAssets(world);
            var totalInfoLabel = blobDBPane.Q<Label>("blobInfoTotal");
            totalInfoLabel.text = GatherBlobDBInfo();
            CreateBlobAssetList();
        }
        currentWorld = world;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    unsafe void RegisterBlobAsset(BlobAssetInfo bai, Hash128 hash)
    {
        blobAssetsSummary.totalCount += 1;
        blobAssetsSummary.sizeInBytes += bai.blobAsset.m_data.Header->Length;
        allBlobAssets.Add(hash, bai);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void GatherAllBlobAssets(World world)
    {
        allBlobAssets.Clear();
        blobAssetsSummary = new ();
        
        //  Gather blob assets from database
        var dbQ = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<BlobDatabaseSingleton>()
            .Build(world.EntityManager);
        
        if (dbQ.TryGetSingleton<BlobDatabaseSingleton>(out var db))
        {
            foreach (var kv in db.animations)
            {
                if (!BlobDatabaseSingleton.IsBlobValid(kv.Value))
                    continue;
                
                var blobInfoEntry = new BlobAssetInfo()
                {
                    blobAsset = UnsafeUntypedBlobAssetReference.Create(kv.Value),
                    blobType = BlobType.AnimationClip,
                    refEntities = new ()
                };
                RegisterBlobAsset(blobInfoEntry, kv.Key);
            }
            
            foreach (var kv in db.avatarMasks)
            {
                if (!BlobDatabaseSingleton.IsBlobValid(kv.Value))
                    continue;
                
                var blobInfoEntry = new BlobAssetInfo()
                {
                    blobAsset = UnsafeUntypedBlobAssetReference.Create(kv.Value),
                    blobType = BlobType.AvatarMask,
                    refEntities = new ()
                };
                RegisterBlobAsset(blobInfoEntry, kv.Key);
            }
        }
        
        //  Gather animator, animation and avatar mask blob assets from entities
        var eQ = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<AnimatorControllerLayerComponent>()
            .Build(world.EntityManager);
        var animControllersChunks = eQ.ToArchetypeChunkArray(Allocator.Temp);
        var controllerLayerBufHandle = world.EntityManager.GetBufferTypeHandle<AnimatorControllerLayerComponent>(true);
        var entityHandle = world.EntityManager.GetEntityTypeHandle();
        for (var i = 0; i < animControllersChunks.Length; ++i)
        {
            var chunk = animControllersChunks[i];
            var bufAcc = chunk.GetBufferAccessor(ref controllerLayerBufHandle);
            var entities = chunk.GetNativeArray(entityHandle);
            for (var k = 0; k < chunk.Count; ++k)
            {
                var lb = bufAcc[k];
                var e = entities[k];
                for (var l = 0; l < lb.Length; ++l)
                {
                    var acl = lb[l];
                    
                    if (!allBlobAssets.TryGetValue(acl.controller.Value.hash, out var blobInfoEntry))
                    {
                        blobInfoEntry = new BlobAssetInfo()
                        {
                            blobAsset = UnsafeUntypedBlobAssetReference.Create(acl.controller),
                            blobType = BlobType.AnimatorController,
                            refEntities = new ()
                        };
                        RegisterBlobAsset(blobInfoEntry, acl.controller.Value.hash);
                        
                        ref var layers = ref acl.controller.Value.layers;
                        for (var m = 0; m < layers.Length; ++m)
                        {
                            ref var layer = ref layers[m];
                            if (allBlobAssets.TryGetValue(layer.avatarMaskBlobHash, out var avatarMaskBlobInfo))
                                avatarMaskBlobInfo.refEntities.Add(e);
                        }
                    }

                    ref var anims = ref acl.animations.Value.animations;
                    for (var m = 0; m < anims.Length; ++m)
                    {
                        var anmHash = anims[m];
                        if (allBlobAssets.TryGetValue(anmHash, out var anmBlob))
                            anmBlob.refEntities.Add(e);
                    }

                    blobInfoEntry.refEntities.Add(e);
                }
            }
        }
        
        //  Gather skinned mesh blob assets from entities
        var eSMR = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<AnimatedSkinnedMeshComponent>()
            .Build(world.EntityManager);
        
        var smrChunks = eSMR.ToArchetypeChunkArray(Allocator.Temp);
        var smrTypeHandle = world.EntityManager.GetComponentTypeHandle<AnimatedSkinnedMeshComponent>(true);
        for (var i = 0; i < smrChunks.Length; ++i)
        {
            var chunk = smrChunks[i];
            var smrs = chunk.GetNativeArray(ref smrTypeHandle);
            var entities = chunk.GetNativeArray(entityHandle);
            for (var k = 0; k < chunk.Count; ++k)
            {
                var smr = smrs[k];
                var e = entities[k];
                    
                if (!allBlobAssets.TryGetValue(smr.smrInfoBlob.Value.hash, out var smrBlobInfo))
                {
                    smrBlobInfo = new BlobAssetInfo()
                    {
                        blobAsset = UnsafeUntypedBlobAssetReference.Create(smr.smrInfoBlob),
                        blobType = BlobType.SkinnedMeshInfo,
                        refEntities = new ()
                    };
                    RegisterBlobAsset(smrBlobInfo, smr.smrInfoBlob.Value.hash);
                }
                smrBlobInfo.refEntities.Add(e);
            }
        }
        
        //  Gather rig definition blob assets from entities
        var eRig = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<RigDefinitionComponent>()
            .Build(world.EntityManager);
        
        var rigChunks = eRig.ToArchetypeChunkArray(Allocator.Temp);
        var rigDefTypeHandle = world.EntityManager.GetComponentTypeHandle<RigDefinitionComponent>(true);
        for (var i = 0; i < rigChunks.Length; ++i)
        {
            var chunk = rigChunks[i];
            var rigs = chunk.GetNativeArray(ref rigDefTypeHandle);
            var entities = chunk.GetNativeArray(entityHandle);
            for (var k = 0; k < chunk.Count; ++k)
            {
                var rigDef = rigs[k];
                var e = entities[k];
                    
                if (!allBlobAssets.TryGetValue(rigDef.rigBlob.Value.hash, out var rigBlobInfo))
                {
                    rigBlobInfo = new BlobAssetInfo()
                    {
                        blobAsset = UnsafeUntypedBlobAssetReference.Create(rigDef.rigBlob),
                        blobType = BlobType.RigInfo,
                        refEntities = new ()
                    };
                    RegisterBlobAsset(rigBlobInfo, rigDef.rigBlob.Value.hash);
                }
                rigBlobInfo.refEntities.Add(e);
            }
        }
        
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    string GatherBlobDBInfo()
    {
        var rv = $"Summary: {blobAssetsSummary.totalCount} blob assets, total memory {CommonTools.FormatMemory(blobAssetsSummary.sizeInBytes)}";
        return rv;
    }

    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void SwitchToBlobCachePane()
    {
        splitView.Add(blobCachePane);
        var clearCacheBtn = rootVisualElement.Q<Button>("clearCacheBtn");
        var disableEnableCacheBtn = rootVisualElement.Q<Button>("disableEnableCacheBtn");
        
        Action clearCacheLambda = () =>
        {
            Directory.Delete(BlobCache.GetControllerBlobCacheDirPath(), true);
            Directory.Delete(BlobCache.GetAnimationBlobCacheDirPath(), true);
            FillBlobCacheInfo();
        };
        clearCacheBtn.clickable = new Clickable(clearCacheLambda);
        
        Action disableEnableCacheLambda = () =>
        {
            var bt = EditorUserBuildSettings.activeBuildTarget;
            var btg = BuildPipeline.GetBuildTargetGroup(bt);
            var target = NamedBuildTarget.FromBuildTargetGroup(btg);
            PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
        #if RUKHANKA_NO_BLOB_CACHE
            var l = new List<string>(defines);
            l.Remove("RUKHANKA_NO_BLOB_CACHE");
            defines = l.ToArray();
        #else
            var newDefines = new string[defines.Length + 1];
            Array.Copy(defines, newDefines, defines.Length);
            newDefines[^1] = "RUKHANKA_NO_BLOB_CACHE";
            defines = newDefines;
        #endif
            PlayerSettings.SetScriptingDefineSymbols(target, defines);
            CompilationPipeline.RequestScriptCompilation();
        };
        disableEnableCacheBtn.clickable = new Clickable(disableEnableCacheLambda);
    #if RUKHANKA_NO_BLOB_CACHE
        disableEnableCacheBtn.text = "Enable Cache";
    #else
        disableEnableCacheBtn.text = "Disable Cache";
        var blobCacheDisabledLabel = rootVisualElement.Q<VisualElement>("blobCacheDisabledLabel");
        blobCacheDisabledLabel.style.display = DisplayStyle.None;
    #endif
        
        FillBlobCacheInfo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void SwitchToBlobDBPane()
    {
        splitView.Add(blobDBPane);
    #if RUKHANKA_DEBUG_INFO
        var noDebugInfoWarning = blobDBPane.Q("noRukhankaDebugInfoWarning");
        noDebugInfoWarning.style.display = DisplayStyle.None;
    #endif
        FillBlobDBInifo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    unsafe void CreateBlobAssetList()
    {
        var groups = new Foldout[]
        {
            blobDBPane.Q<Foldout>("animatorsGroup"),
            blobDBPane.Q<Foldout>("animationClipsGroup"),
            blobDBPane.Q<Foldout>("rigsGroup"),
            blobDBPane.Q<Foldout>("skinnedMeshesGroup"),
            blobDBPane.Q<Foldout>("avatarMasksGroup"),
        };
        
        var foldoutLabels = new string[]
        {
            "Animator Controller Blobs",
            "Animation Clip Blobs",
            "Rig Blobs",
            "Skinned Mesh Blobs",
            "Avatar Mask Blobs",
        };
        
        var countArr = new int[(int)BlobType.Total];
        var memoryArr = new int[(int)BlobType.Total];
        
        for (var i = 0; i < (int)BlobType.Total; ++i)
        {
            groups[i].Clear();
        }
        
        foreach (var ba in allBlobAssets)
        {
            var bti = (int)ba.Value.blobType;
            countArr[bti] += 1;
            memoryArr[bti] += ba.Value.blobAsset.m_data.Header->Length;
            
            var blobEntry = blobEntryAsset.Instantiate();
            FillBlobAssetInfoText(blobEntry, ba.Value);
            if (groups[bti].childCount % 2 == 0)
                blobEntry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
            groups[bti].Add(blobEntry);
        }
        
        for (var i = 0; i < (int)BlobType.Total; ++i)
        {
            groups[i].text = $"{countArr[i]} {foldoutLabels[i]} ({CommonTools.FormatMemory(memoryArr[i])})";
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    unsafe void FillBlobAssetInfoText(TemplateContainer blobEntry, BlobAssetInfo ba)
    {
        var infoLabel = blobEntry.Q<Label>("infoLabel");
        var nameLabel = blobEntry.Q<Label>("nameAndHashLabel");
        var infoBtn = blobEntry.Q<Button>("infoBtn");
        
        switch (ba.blobType)
        {
        case BlobType.AnimationClip:
        {
            var acb = ba.blobAsset.Reinterpret<AnimationClipBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText += $" '{acb.Value.name.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: {CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length)}";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                AnimatorClipBlobInfoWindow.animationClipBlob = ba;
                var wnd = GetWindow<AnimatorClipBlobInfoWindow>();
                wnd.Show();
            };
            break;
        }
        case BlobType.AnimatorController:
        {
            var acb = ba.blobAsset.Reinterpret<ControllerBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText += $" '{acb.Value.name.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: { CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length) }";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                AnimatorControllerBlobInfoWindow.controllerBlob = ba;
                var wnd = GetWindow<AnimatorControllerBlobInfoWindow>();
                wnd.Show();
            };
            break;
        }
        case BlobType.RigInfo:
        {
            var acb = ba.blobAsset.Reinterpret<RigDefinitionBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText = $" '{acb.Value.name.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: {CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length)}";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                RigBlobInfoWindow.rigBlob = ba;
                var wnd = GetWindow<RigBlobInfoWindow>();
                wnd.Show();
            };
            break;
        }
        case BlobType.AvatarMask:
        {
            var acb = ba.blobAsset.Reinterpret<AvatarMaskBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText += $" '{acb.Value.name.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: { CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length) }";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                AvatarMaskInfoWindow.avatarMaskBlob = ba;
                var wnd = GetWindow<AvatarMaskInfoWindow>();
                wnd.Show();
            };
            break;
        }
        case BlobType.SkinnedMeshInfo:
        {
            var acb = ba.blobAsset.Reinterpret<SkinnedMeshInfoBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText += $" '{acb.Value.skeletonName.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: {CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length)}";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                SkinnedMeshBlobInfoWindow.skinnedMeshBlob = ba;
                var wnd = GetWindow<SkinnedMeshBlobInfoWindow>();
                wnd.Show();
            };
            break;
        }
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillBlobCacheInfo()
    {
        var cachePathLabel = rootVisualElement.Q<Label>("blobCachePathLabel");
        cachePathLabel.text = $"Cache Path: '{BlobCache.GetBlobCacheDirPath()}'";
        
        var controllerMem = FillBlobList("animatorControllerBlobsFoldout", "Animator Controller Blobs", BlobCache.GetControllerBlobCacheDirPath());
        var animationsMem = FillBlobList("animationBlobsFoldout", "Animation Clip Blobs", BlobCache.GetAnimationBlobCacheDirPath());
        
        var animMemLabel = rootVisualElement.Q<Label>("totalAnimationsMemLabel");
        var controllerMemLabel = rootVisualElement.Q<Label>("totalControllerMemLabel");
        animMemLabel.text = $"Total animation blob cache size: {CommonTools.FormatMemory(animationsMem)}";
        controllerMemLabel.text = $"Total animator controller blob cache size: {CommonTools.FormatMemory(controllerMem)}";
        
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    long FillBlobList(string foldoutName, string foldoutCaption, string cachePath)
    {
        var foldout = rootVisualElement.Q<Foldout>(foldoutName);
        foldout.Clear();
        var numCachedBlobs = 0;
        var rv = 0L;
        
        if (Directory.Exists(cachePath))
        {
            var files = Directory.GetFiles(cachePath);
            for (var i = 0; i < files.Length; ++i)
            {
                var entry = blobCacheEntryAsset.Instantiate();
                var file = files[i].Replace('\\', '/');
                
                if (i % 2 == 0)
                    entry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
                
                var entryPath = entry.Q<Label>("pathLabel");
                var sizeLabel = entry.Q<Label>("sizeLabel");
                entryPath.text = file.Replace(BlobCache.GetBlobCacheDirPath(), "");
                var fs = new FileInfo(file).Length;
                sizeLabel.text = CommonTools.FormatMemory(fs);
                rv += fs;
                
                foldout.Add(entry);
            }
            numCachedBlobs += files.Length;
        }
        foldout.text = $"{numCachedBlobs} {foldoutCaption}";
        return rv;
    }
}
}