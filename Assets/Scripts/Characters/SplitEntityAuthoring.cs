using Unity.Entities;
using UnityEngine;


public struct SplitterComponent : IComponentData, IEnableableComponent
{
    //public bool split;
    public Entity splitPrefab;
}

public class SplitEntityAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject splitPrefab;
    private class SplitEntityAuthoringBaker : Baker<SplitEntityAuthoring>
    {
        public override void Bake(SplitEntityAuthoring authoring)
        {
            var e = GetEntity(authoring.splitPrefab, TransformUsageFlags.Dynamic);
            AddComponent(GetEntity(TransformUsageFlags.None), new SplitterComponent() {splitPrefab = e});
        }
        
    }
}