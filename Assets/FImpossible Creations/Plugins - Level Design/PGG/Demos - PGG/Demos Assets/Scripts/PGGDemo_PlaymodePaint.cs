using FIMSpace.Generating;
using System.Collections;
using UnityEngine;

public class PGGDemo_PlaymodePaint : MonoBehaviour
{
    public FlexiblePainter Painter;

    [Header("Playmode Painting")]
    public Transform pointer;

    [Header("Animating")]
    public float AnimationDuration = 0.5f;

    private Vector3Int? previousCell = null;
    private bool previousErase = false;

    private void Start()
    {
        if (Painter == null) Painter = GetComponentInChildren<FlexiblePainter>();
        Painter.InstantiatedInfo.SetCustomHandling(true, true);
    }

    private void Update()
    {
        if (!Painter)
        {
            UnityEngine.Debug.Log("[PGG DEMO] Assign Painter Component in the inspector window");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane paintPlane = new Plane(Painter.IsPainting2D ? transform.forward : transform.up, transform.position);

        float planeHit;
        if (paintPlane.Raycast(ray, out planeHit))
        {
            Vector3 hitPoint = ray.GetPoint(planeHit);

            if (pointer) pointer.position = PGGUtils.GetWorldPositionOfCellAt(transform, Painter.FieldSetup, hitPoint, Painter.IsPainting2D);

            //UnityEngine.Debug.DrawRay((hitPoint), Vector3.up, Color.white);
            //UnityEngine.Debug.DrawRay(PGGUtils.GetWorldPositionOfCellAt(transform, Painter.FieldSetup, pointer.position, Painter.IsPainting2D), Vector3.up, Color.green, 1.01f);

            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                Vector3Int gridCellPos = PGGUtils.WorldToGridCellPosition(transform, Painter.FieldSetup, pointer.position, Painter._Editor_YLevel, Painter.IsPainting2D);
                bool erase = Input.GetMouseButton(0) ? false : true;

                // Prevent spam-calling refresh cells
                bool execute = false;

                if (previousErase != erase) { execute = true; }
                else
                if (previousCell == null || previousCell.Value != gridCellPos) execute = true;

                if (execute)
                {
                    Painter.PaintPosition(gridCellPos, erase);
                    Painter.GenerateObjects();
                    previousErase = erase;
                    previousCell = gridCellPos;
                }
            }
        }

        // Launching animation on new generated objects
        if (Painter.InstantiatedInfo.CustomInstantiatedList != null)
        {
            for (int i = 0; i < Painter.InstantiatedInfo.CustomInstantiatedList.Count; i++)
            {
                GameObject ins = Painter.InstantiatedInfo.CustomInstantiatedList[i];
                ExampleSpawnAnimation.AnimHelper helper = new ExampleSpawnAnimation.AnimHelper(ins.transform, 2f, ExampleSpawnAnimation.EStyle.FromDown, ExampleSpawnAnimation.EMode.Move);
                StartCoroutine(ExampleSpawnAnimation.SpawnAnimation(helper, 0f, AnimationDuration, ExampleSpawnAnimation.EEase.Elastic));
            }

            Painter.InstantiatedInfo.CustomInstantiatedList.Clear();
        }

        // Destroying objects with animation
        if (Painter.InstantiatedInfo.CustomToDestroyList != null)
        {
            for (int i = 0; i < Painter.InstantiatedInfo.CustomToDestroyList.Count; i++)
            {
                GameObject ins = Painter.InstantiatedInfo.CustomToDestroyList[i];
                StartCoroutine(IEDestroyAnimation(ins));
            }

            Painter.InstantiatedInfo.CustomToDestroyList.Clear();
        }
    }

    IEnumerator IEDestroyAnimation(GameObject toDestr)
    {
        Vector3 startScale = toDestr.transform.localScale;
        float elapsed = 0f;
        WaitForEndOfFrame endfr = new WaitForEndOfFrame();

        while(elapsed < AnimationDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progr = elapsed / (AnimationDuration * 0.5f);

            yield return endfr;
            if (toDestr == null) yield break;
            toDestr.transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, ExampleSpawnAnimation.Bounce(progr));
        }

        Destroy(toDestr);

        yield break;
    }

}
