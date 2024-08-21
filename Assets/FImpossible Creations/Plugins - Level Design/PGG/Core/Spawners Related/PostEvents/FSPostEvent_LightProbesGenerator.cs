using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "FS Post Event - Light Probes Generator", menuName = "FImpossible Creations/Procedural Generation/PE Light Probe Instance", order = 1)]
    public class FSPostEvent_LightProbesGenerator : FieldSpawnerPostEvent_Base
    {

        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
#if UNITY_EDITOR
            // Generate object for light probes
            GameObject probeObj = new GameObject(name + "-LightProbe");
            LightProbeGroup probe = probeObj.AddComponent<LightProbeGroup>();

            // Light probes position list
            List<Vector3> positions = new List<Vector3>();

            // Shortcuts for the main important references
            var grid = generatedRef.Grid;
            var fieldBounds = generatedRef.FieldBounds;
            var container = generatedRef.FieldTransform;

            // Calculation variables
            int ProbesPerCell = helper.RequestVariable("Probes Per Cell", 2).IntV;
            Vector3 cellsSize = generatedRef.ParentSetup.GetCellUnitSize();
            float cellOffsetY = cellsSize.y * 0.5f;

            // If there is no multiple Y levels, let's use room height as height reference (80%)
            if (grid.GetMin().y == grid.GetMax().y)
                cellOffsetY = Mathf.LerpUnclamped(cellOffsetY, fieldBounds.size.y * 0.4f, 0.8f);

            if (ProbesPerCell > 1)
            {
                // Generating few probes per cell basing on the cell dimensions and position in grid space
                for (int i = 0; i < grid.AllApprovedCells.Count; i++)
                {
                    var cell = grid.AllApprovedCells[i];
                    if (cell.DontGenerateLightProbes) continue;

                    Vector3 cellCenterPos = cell.WorldPos(cellsSize);
                    cellCenterPos.y += cellOffsetY;

                    Bounds cellBounds = new Bounds(cellCenterPos, cellsSize);
                    cellBounds.size *= 0.75f; // Make voxel sligthly smaller to not overlap probes into walls

                    float maxV = ProbesPerCell - 1;

                    for (int x = 0; x < ProbesPerCell; x++)
                        for (int y = 0; y < ProbesPerCell; y++)
                            for (int z = 0; z < ProbesPerCell; z++)
                            {
                                Vector3 probePos = cellBounds.min;
                                probePos.x = Mathf.Lerp(cellBounds.min.x, cellBounds.max.x, (float)x / maxV);
                                probePos.y = Mathf.Lerp(cellBounds.min.y, cellBounds.max.y, (float)y / maxV);
                                probePos.z = Mathf.Lerp(cellBounds.min.z, cellBounds.max.z, (float)z / maxV);
                                positions.Add(probePos);
                            }
                }
            }
            else
            {
                for (int i = 0; i < grid.AllApprovedCells.Count; i++)
                {
                    var cell = grid.AllApprovedCells[i];
                    if (cell.DontGenerateLightProbes) continue;

                    Vector3 cellCenterPos = cell.WorldPos(cellsSize);
                    cellCenterPos.y += cellOffsetY;

                    Bounds cellBounds = new Bounds(cellCenterPos, cellsSize);
                    cellBounds.size *= 0.75f; // Make voxel sligthly smaller to not overlap probes into walls

                    Vector3 probePos = cellCenterPos;
                    probePos.y = cellBounds.min.y;
                    positions.Add(probePos);

                    probePos = cellCenterPos;
                    probePos.y = cellBounds.max.y;
                    positions.Add(probePos);
                }
            }

            // Assign calculated probe positions
            probe.probePositions = positions.ToArray();

            // Put generated light probes object inside generated field parent
            probeObj.transform.SetParent(container, true);
            probeObj.transform.ResetCoords();

            generatedRef.Instantiated.Add(probeObj);
#endif
        }


#if UNITY_EDITOR
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            PostEventInfo = "Unity Light probe features are Editor Only";

            FieldVariable count = helper.RequestVariable("Probes Per Cell", 2);
            count.helper.x = 1;
            count.helper.y = 4;

            FieldVariable.Editor_DrawTweakableVariable(count);
        }
#endif

    }
}