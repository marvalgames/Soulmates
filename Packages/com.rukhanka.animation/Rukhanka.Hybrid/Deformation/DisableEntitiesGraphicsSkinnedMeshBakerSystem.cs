#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateInGroup(typeof(PreBakingSystemGroup))]
public partial class DisableEntitiesGraphicsSkinnedMeshBakerSystem: SystemBase
{
    protected override void OnUpdate()
    {
        var varName = "_IndexToBakerInstances";
        var bakerDataUtilityType = typeof(BakerDataUtility);
        var fieldInfo = bakerDataUtilityType.GetField(varName, BindingFlags.NonPublic | BindingFlags.Static);
        if (fieldInfo == null)
        {
            throw new NullReferenceException($"Cannot find '{varName}' in {bakerDataUtilityType.Name}. Disabling Entities.Graphics skinned mesh baking is failed.");
        }
        
        var typeIndexToBakerInstancesMap = fieldInfo.GetValue(null) as Dictionary<TypeIndex, BakerDataUtility.BakerData[]>;
        if (typeIndexToBakerInstancesMap == null)
        {
            throw new NullReferenceException($"Cannot cast '{varName}' to proper dictionary type. Disabling Entities.Graphics skinned mesh baking is failed.");
        }
        
        var entitiesGraphicsSystemType = typeof(EntitiesGraphicsSystem);
        var skinnedMeshRendererBakerType = entitiesGraphicsSystemType.Assembly.GetType("Unity.Rendering.SkinnedMeshRendererBaker");
        var skinnedMeshRendererIndex = TypeManager.GetTypeIndex<SkinnedMeshRenderer>();
        var smrBakers = typeIndexToBakerInstancesMap[skinnedMeshRendererIndex].ToList();
        for (var i = 0; i < smrBakers.Count; i++)
        {
            var smb = smrBakers[i];
            if (smb.Baker.GetType() == skinnedMeshRendererBakerType)
            {
                smrBakers.RemoveAt(i);
                break;
            }
        }

        typeIndexToBakerInstancesMap[skinnedMeshRendererIndex] = smrBakers.ToArray();
        fieldInfo.SetValue(null, typeIndexToBakerInstancesMap);
    }
}
}

#endif