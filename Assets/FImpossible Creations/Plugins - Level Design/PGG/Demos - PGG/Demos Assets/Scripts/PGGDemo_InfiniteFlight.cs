using FIMSpace;
using FIMSpace.Generating;
using System.Collections.Generic;
using UnityEngine;

public class PGGDemo_InfiniteFlight : MonoBehaviour
{
    [Header("Path Generating Settings")]
    public float AverageSegmentLength = 14f;
    public int GenerateForward = 8;
    public int MaxSegmentRotationAngle = 25;
    public float FlightSpeed = 8f;
    public bool RemoveBackCells = false;

    [Header("Debugging")]
    public FlexibleGenerator Generator;
    public int Radius = 2;

    [Header("Debugging")]
    public List<Vector3> NextPoints;

    private float flyProgress = 0f;
    private Quaternion targetRotation;
    private int compleatedSegments = 0;
    //private Vector3 lastDir;
    //float randomCurve = 0f;

    void Start()
    {
        NextPoints.Add(transform.position);
        targetRotation = transform.rotation;

        if (Generator)
        {
            Generator.transform.position = transform.position + Vector3.down;
            Generator.Prepare();
            for (int i = 0; i < GenerateForward; i++) GeneratePoint();
            GenerateSegments();
        }

        //lastDir = transform.forward;
    }

    void Update()
    {
        GeneratePoints();

        if (NextPoints.Count < 3) return;

        Vector3 targetPosition = NextPoints[1];
        Vector3 toNext = targetPosition - NextPoints[0];

        float flyStep = 1f / Vector3.Distance(targetPosition, NextPoints[0]);

        flyProgress += flyStep * FlightSpeed * Time.deltaTime;

        //Vector3 deflect = Vector3.Lerp(NextPoints[0], NextPoints[1], 0.5f) + (lastDir.normalized + (NextPoints[1] - NextPoints[2]).normalized).normalized * toNext.magnitude * randomCurve;
        //targetPosition = GetCurvedSegmentPos(NextPoints[0],  targetPosition, deflect, flyProgress);
        targetPosition = Vector3.LerpUnclamped(NextPoints[0], targetPosition, flyProgress);

        if (flyProgress > 1f)
        {
            flyProgress -= 1f;
            //lastDir = NextPoints[1] - NextPoints[0];
            NextPoints.RemoveAt(0);
            //randomCurve = Random.Range(0f, .5f);
            compleatedSegments += 1;

            if (toClearMemory2.Count > 0)
                if (RemoveBackCells)
                {
                    Generator.Cells.RemoveCells(toClearMemory2);
                    Generator.Cells.RunDirtyCells(false); // Faster computation without surround dirty and calculating
                    Generator.Cells.InstantiateAllRemaining();
                    toClearMemory2.Clear();
                }

            GenerateSegments();
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref sd_smooth, 0.2f, Mathf.Infinity, Time.deltaTime);

        //targetPosition =  GetCurvedSegmentPos(NextPoints[0], NextPoints[1], deflect, flyProgress + 0.1f) - targetPosition;
        //targetRotation = Quaternion.Slerp(targetRotation, Quaternion.LookRotation(targetPosition.normalized), Time.deltaTime * 4f);

        Quaternion tgtRot = Quaternion.Lerp(Quaternion.LookRotation(toNext), Quaternion.LookRotation(NextPoints[2] - NextPoints[1]), flyProgress);

        targetRotation = FEngineering.SmoothDampRotation(targetRotation, tgtRot, ref sd_rot, 0.35f, Time.deltaTime);
        //targetRotation = Quaternion.Slerp(targetRotation, Quaternion.LookRotation(toNext.normalized), Time.deltaTime * 1f);

        transform.rotation = targetRotation;
    }

    Vector3 sd_smooth = Vector3.zero;
    Quaternion sd_rot = Quaternion.identity;

    void GenerateSegments()
    {
        Generator.TestGridSize = Vector2Int.zero;
        cellPositions.Clear();

        for (int i = 0; i < NextPoints.Count - 1; i++) GenerateGridSegment(NextPoints[i], NextPoints[i + 1], i);

        Generator.Cells.AddCells(cellPositions, false);
        Generator.GenerateObjects();
    }

    public Vector3 GetCurvedSegmentPos(Vector3 start, Vector3 deflect, Vector3 end, float t)
    {
        float reversedProgress = 1f - t;
        return reversedProgress * reversedProgress * start + 2f * reversedProgress * t * end + t * t * deflect;
    }

    void GeneratePoints()
    {
        if (NextPoints.Count < GenerateForward)
        {
            GeneratePoint();
        }
    }

    void GeneratePoint()
    {
        Vector3 nextPos = NextPoints[NextPoints.Count - 1];
        Vector3 lastDirection = Vector3.forward;
        if (NextPoints.Count > 2) lastDirection = NextPoints[NextPoints.Count - 1] - NextPoints[NextPoints.Count - 2];

        Quaternion lastRot = Quaternion.LookRotation(lastDirection.normalized);

        float length = Random.Range(AverageSegmentLength * 0.75f, AverageSegmentLength * 1.2f);
        nextPos += (lastRot * Quaternion.Euler(0f, Random.Range(-MaxSegmentRotationAngle, MaxSegmentRotationAngle), 0f)) * Vector3.forward * length;

        NextPoints.Add(nextPos);
    }

    List<Vector3Int> cellPositions = new List<Vector3Int>();
    List<Vector3Int> toClearMemory = new List<Vector3Int>();
    List<Vector3Int> toClearMemory2 = new List<Vector3Int>();
    void GenerateGridSegment(Vector3 start, Vector3 end, int segmentI)
    {
        Quaternion segmentDir = Quaternion.LookRotation(start, end);
        float size = Generator.FieldSetup.GetCellUnitSize().x;
        float segmLen = Vector3.Distance(start, end);
        int toNext = Mathf.CeilToInt(segmLen / size);

        Vector3Int segmDirOff = (segmentDir * Vector3.right).normalized.V3toV3Int();
        segmDirOff.y = 0;

        if (segmentI == 0)
        {
            toClearMemory2.Clear();
            for (int i = 0; i < toClearMemory.Count; i++) toClearMemory2.Add(toClearMemory[i]);

            toClearMemory.Clear();
        }

        for (int i = 0; i < toNext; i++)
        {
            Vector3 worldPos = Vector3.Lerp(start, end, (float)i / (float)toNext);
            Vector3Int goPos = GetCellPos(worldPos);

            for (int x = -Radius; x <= Radius; x++)
            {
                for (int z = -Radius; z <= Radius; z++)
                {
                    Vector3Int p = goPos + new Vector3Int(x, 0, z);
                    if (cellPositions.Contains(p) == false)
                    {
                        cellPositions.Add(p);
                        if (RemoveBackCells) if (segmentI == 0) toClearMemory.Add(p);
                    }
                }
            }
        }

    }

    Vector3Int GetCellPos(Vector3 worldPos)
    {
        return PGGUtils.WorldToGridCellPosition(Generator.transform, Generator.FieldSetup, worldPos, 0, false);
    }

    private void OnDrawGizmosSelected()
    {
        if (NextPoints == null) return;
        if (NextPoints.Count < 2) return;

        for (int i = 0; i < NextPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(NextPoints[i], NextPoints[i + 1]);
        }
    }
}
