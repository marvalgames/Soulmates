using UnityEngine;

namespace FIMSpace.Generating.Checker
{
    public partial class CheckerField3D
    {

        public void DrawFieldGizmos(bool setMatrix = true, bool drawSphere = true)
        {
            Matrix4x4 preMx = Gizmos.matrix;
            if (setMatrix) Gizmos.matrix = preMx * Matrix;

            float scale = 1f;
            Vector3 drawScale = new Vector3(scale, scale * 0.1f, scale) * 0.95f;// * scale * 0.92f;

            Vector3 startPos = Vector3.zero;//RootPosition * scale;
            Color preCol = Gizmos.color;

            #region Bounds Mode Draw
            if (UseBounds && Bounding.Count > 0)
            {
                for (int i = 0; i < Bounding.Count; i++)
                {
                    Vector3 sze = Bounding[i].size;
                    sze.y *= 0.1f;
                    Gizmos.DrawCube(startPos + Bounding[i].center * scale, sze * scale);
                }
            }
            #endregion
            else // Cell Mode Draw
            {
                bool gridHasHeight = false;
                if (Grid.GetMaxSizeInCells().y > 1) gridHasHeight = true;

                for (int i = 0; i < Grid.AllApprovedCells.Count; i++)
                {
                    var cell = Grid.AllApprovedCells[i];

                    //Gizmos.DrawWireCube(startPos + cell.Pos.V3IntToV3() * scale, drawScale);
                    if (cell.IsGhostCell) Gizmos.color = new Color(preCol.r, preCol.g, preCol.b, preCol.a * 0.65f);

                    Vector3 cellBoxPos = startPos + cell.Pos.V3IntToV3() * scale;

                    if (gridHasHeight)
                    {
                        bool cellBelow = false;
                        cellBelow = FGenerators.NotNull(Grid.GetCell(new Vector3Int(cell.Pos.x, cell.Pos.y - 1, cell.Pos.z), false));

                        bool cellAbove = false;
                        cellAbove = FGenerators.NotNull(Grid.GetCell(new Vector3Int(cell.Pos.x, cell.Pos.y + 1, cell.Pos.z), false));

                        if (cellBelow)
                        {
                            if (!cellAbove)
                                Gizmos.DrawCube(cellBoxPos - new Vector3(0, scale * 0.1f), new Vector3(drawScale.x, drawScale.x * 0.35f, drawScale.z));
                            else
                                Gizmos.DrawCube(cellBoxPos, new Vector3(drawScale.x, drawScale.x * 0.6f, drawScale.z));
                        }
                        else
                        {
                            if (cellAbove)
                                Gizmos.DrawCube(cellBoxPos + new Vector3(0, scale * 0.1f), new Vector3(drawScale.x, drawScale.x * 0.35f, drawScale.z));
                        }

                        if (!cellAbove && !cellBelow)
                            Gizmos.DrawCube(cellBoxPos, drawScale);
                    }
                    else
                    {
                        Gizmos.DrawCube(cellBoxPos, drawScale);
                    }


                    if (cell.IsGhostCell) Gizmos.color = preCol;
                }
            }

            if (drawSphere) Gizmos.DrawSphere(startPos, scale * 0.25f);

            if (setMatrix) Gizmos.matrix = preMx;
        }

        public bool DrawFieldHandles(float scaleUp = 1f)
        {
            bool clicked = false;
            float scale = scaleUp;
            //Vector3 drawScale = new Vector3(scale, scale * 0.1f, scale) * 0.92f;
            Vector3 startPos = Vector3.zero;//RootPosition * scale;

#if UNITY_EDITOR

            if (UnityEditor.Handles.Button(Matrix.MultiplyPoint(startPos), RootRotation, scale * 0.5f, scale * 0.3f, UnityEditor.Handles.SphereHandleCap))
            {
                //Event.current.Use();
                clicked = true;
            }
#endif

            return clicked;
        }

        public void DrawFieldGizmosBounding()
        {
            //if (setMatrix) Gizmos.matrix = Matrix;

            Bounds b = GetFullBoundsWorldSpace();
            Gizmos.DrawWireCube(b.center, b.size);

            //if (setMatrix) Gizmos.matrix = Matrix4x4.identity;
        }

    }
}