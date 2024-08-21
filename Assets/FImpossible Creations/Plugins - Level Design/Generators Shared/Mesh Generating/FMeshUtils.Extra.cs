using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FIMSpace.Generating
{
    public static partial class FMeshUtils
    {
        public static void RenameMesh(Mesh m)
        {
#if UNITY_EDITOR
            m.name = "verts=" + m.vertexCount;
#endif
        }

        public static void RenameMesh(Mesh m, Transform parent)
        {
#if UNITY_EDITOR
            m.name = parent?.name + " verts=" + m.vertexCount;
#endif
        }

        /// <summary>
        /// Gets no-forward axes
        /// </summary>
        public static Vector3 GetSelectiveAxes(Quaternion rotation)
        {
            return GetSelectiveAxes(rotation, Vector3.forward);
        }

        public static Vector3 GetSelectiveAxes(Quaternion rotation, Vector3 forward)
        {
            Vector3 axisMul = rotation * forward;
            axisMul.x = Mathf.Abs(axisMul.x - 1f);
            axisMul.y = Mathf.Abs(axisMul.y - 1f);
            axisMul.z = Mathf.Abs(axisMul.z - 1f);
            return axisMul;
        }

        /// <summary>
        /// Gets forward axis
        /// </summary>
        public static Vector3 GetUniqueAxis(Quaternion rotation)
        {
            return GetUniqueAxis(rotation, Vector3.forward);
        }

        /// <summary>
        /// Gets forward axis
        /// </summary>
        public static Vector3 GetUniqueAxis(Quaternion rotation, Vector3 targetForward)
        {
            return rotation * targetForward;
        }



        public static float DistanceToBoundsBorders(Bounds bounds, Vector3 vPos)
        {
            float nrst, dist;
            dist = Mathf.Abs(vPos.x - bounds.min.x); nrst = dist;
            dist = Mathf.Abs(vPos.x - bounds.max.x); if (dist < nrst) { nrst = dist; }
            dist = Mathf.Abs(vPos.z - bounds.min.z); if (dist < nrst) { nrst = dist; }
            dist = Mathf.Abs(vPos.z - bounds.max.z); if (dist < nrst) { nrst = dist; }
            dist = Mathf.Abs(vPos.y - bounds.min.y); if (dist < nrst) { nrst = dist; }
            dist = Mathf.Abs(vPos.y - bounds.max.y); if (dist < nrst) { nrst = dist; }
            return nrst;
        }

        public static void TransformList(List<Vector3> profileShape, Matrix4x4 matrix4x4)
        {
            for (int i = 0; i < profileShape.Count; i++)
            {
                profileShape[i] = matrix4x4.MultiplyPoint3x4(profileShape[i]);
            }
        }

        public static void RotateVertices(Vector3 rotate, List<Vector3> verts)
        {
            if (rotate != Vector3.zero)
            {
                Matrix4x4 mx = Matrix4x4.Rotate(Quaternion.Euler(rotate));
                for (int v = 0; v < verts.Count; v++) verts[v] = mx.MultiplyPoint3x4(verts[v]);
            }
        }

    }
}