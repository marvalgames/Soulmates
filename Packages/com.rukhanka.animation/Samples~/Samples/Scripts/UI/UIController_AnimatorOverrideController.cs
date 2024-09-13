using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class UIController_AnimatorOverrideController: MonoBehaviour
{
    public TextMeshProUGUI animationListText;
    public AnimatorOverrideController overrideController;
    List<KeyValuePair<AnimationClip, AnimationClip>> originalOverrides = new ();
    
/////////////////////////////////////////////////////////////////////////////////

    void Start()
    {
        overrideController.GetOverrides(originalOverrides);
        SetAnimationList(true);
    }

/////////////////////////////////////////////////////////////////////////////////

    void OnDestroy()
    {
        GameObjectWorldSwitcher(true);
    }

/////////////////////////////////////////////////////////////////////////////////

    void SetAnimationList(bool on)
    {
        var animList = "Used animations: ";
        var e = originalOverrides.Count;
        for (var i = 0; i < e; ++i)
        {
            var kv = originalOverrides[i];
            animList += on ? kv.Value.name : kv.Key.name;
            if (i < e - 1)
                animList += ", ";
        }
        animationListText.text = animList;
    }

/////////////////////////////////////////////////////////////////////////////////

    public void UseOverrideAnimationChange(bool on)
    {
        EntityWorldSwitcher(on);
        GameObjectWorldSwitcher(on);
        SetAnimationList(on);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    void GameObjectWorldSwitcher(bool on)
    {
        for (var i = 0; i < originalOverrides.Count; ++i)
        {
            var kv = originalOverrides[i];
            var srcAnim = kv.Key;
            var dstAnim = on ? kv.Value : null;
            overrideController[srcAnim] = dstAnim;
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    void EntityWorldSwitcher(bool on)
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var q = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<AnimatorOverrideAnimations>()
            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
            .Build(em);
        
        var chunks = q.ToArchetypeChunkArray(Allocator.Temp);
        var entityHandle = em.GetEntityTypeHandle();
        foreach (var c in chunks)
        {
            var entities = c.GetNativeArray(entityHandle);
            for (int i = 0, ce = c.Count; i < ce; ++i)
            {
                var e = entities[i];
                em.SetComponentEnabled<AnimatorOverrideAnimations>(e, on);
            }
        }
    }
}
}

