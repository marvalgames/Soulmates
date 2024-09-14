using Unity.Entities;
using UnityEngine;


public struct SplitComponent : IComponentData, IEnableableComponent
{
    public bool split;
    public bool scale;
    public float timeRemaining;
    public bool isRunning;
    //public Entity splitPrefab;
}

public class CharacterSplitAuthoring : MonoBehaviour
{
    [SerializeField] bool scale = true;
    [SerializeField] private float timeRemaining = .5f;

    private class CharacterSplitAuthoringBaker : Baker<CharacterSplitAuthoring>
    {
        public override void Bake(CharacterSplitAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new SplitComponent() { timeRemaining= authoring.timeRemaining,  isRunning = true, split = false, scale = authoring.scale });
        }
    }
}