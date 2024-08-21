

// Runtime component, SceneSystem uses Entities.Hash128 to identify scenes.

using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public struct SceneLoader : IComponentData
{
    public Hash128 Guid;
}

#if UNITY_EDITOR
// Authoring component, a SceneAsset can only be used in the Editor
public class SceneLoaderAuthoring : MonoBehaviour
{
    public UnityEditor.SceneAsset Scene;

    class Baker : Baker<SceneLoaderAuthoring>
    {
        public override void Bake(SceneLoaderAuthoring authoring)
        {
            var path = AssetDatabase.GetAssetPath(authoring.Scene);
            var guid = AssetDatabase.GUIDFromAssetPath(path);
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e, new SceneLoader { Guid = guid });

        }
    }
}
#endif
