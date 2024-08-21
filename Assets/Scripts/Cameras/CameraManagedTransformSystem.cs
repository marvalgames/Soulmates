using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Cameras
{
    [RequireMatchingQueriesForUpdate]
    public partial class CameraManagedTransformSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithoutBurst().ForEach((ref CameraControlsComponent cameraControlsComponent) =>
                {
                    var cam = Camera.main.transform;

                    cameraControlsComponent.localTransform =
                        LocalTransform.FromPositionRotation(cam.position, cam.rotation);
                    cameraControlsComponent.forward = cam.forward;
                    cameraControlsComponent.right = cam.right;
                }
            ).Run();
        }
    }
}