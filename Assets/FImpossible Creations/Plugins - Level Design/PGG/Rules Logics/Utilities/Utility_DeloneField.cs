
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public class DeloneField
    {

        public List<Vertex> Verts { get; private set; }
        public List<Edge> Edges { get; private set; }
        public List<Triangle> Tris { get; private set; }
        public List<Tetrahedron> Tetras { get; private set; }

        public DeloneField(List<Vertex> vertices)
        {
            Edges = new List<Edge>();
            Tris = new List<Triangle>();
            Tetras = new List<Tetrahedron>();
            Verts = new List<Vertex>(vertices);
        }

        public void CalculateConcaveTriangulate(float allowFar = 100f)
        {
            if (Verts.Count <= 1) return;

            Vector3 min = Verts[0].Position;
            Vector3 max = Verts[0].Position;

            foreach (Vertex vertex in Verts)
            {
                if (vertex.Position.x < min.x) min.x = vertex.Position.x;
                if (vertex.Position.x > max.x) max.x = vertex.Position.x;
                if (vertex.Position.y < min.y) min.y = vertex.Position.y;
                if (vertex.Position.y > max.y) max.y = vertex.Position.y;
                if (vertex.Position.z < min.z) min.z = vertex.Position.z;
                if (vertex.Position.z > max.z) max.z = vertex.Position.z;
            }

            if (min.y == max.y) // Flat triangulation
            {
                float deltaMax = Mathf.Max(max.x - min.x, max.z - min.z) * 2;
                Vertex floorPoint1 = new Vertex(new Vector3(min.x - 1, 0, min.z - 1));
                Vertex floorPoint2 = new Vertex(new Vector3(min.x - 1, 0, max.z + deltaMax));
                Vertex floorPoint3 = new Vertex(new Vector3(max.x + deltaMax, 0, min.z - 1));

                Tris.Add(new Triangle(floorPoint1, floorPoint2, floorPoint3));

                foreach (Vertex vertex in Verts)
                {
                    List<Edge> edges = new List<Edge>();

                    foreach (Triangle t in Tris)
                        if (t.CircumCircleContainsZ(vertex.Position))
                        {
                            t.Wrong = true;
                            edges.Add(new Edge(t.U, t.V));
                            edges.Add(new Edge(t.V, t.W));
                            edges.Add(new Edge(t.W, t.U));
                        }


                    for (int i = Tris.Count - 1; i >= 0; i--) if (Tris[i].Wrong) Tris.RemoveAt(i);


                    for (int i = 0; i < edges.Count; i++)
                        for (int j = i + 1; j < edges.Count; j++)
                            if (Edge.VeryCloseZ(edges[i], edges[j]))
                            {
                                edges[i].Wrong = true;
                                edges[j].Wrong = true;
                            }


                    for (int i = edges.Count - 1; i >= 0; i--) if (edges[i].Wrong) edges.RemoveAt(i);


                    foreach (Edge edge in edges)
                    {
                        Tris.Add(new Triangle(edge.S, edge.E, vertex));
                    }

                }


                for (int i = Tris.Count - 1; i >= 0; i--)
                {
                    if (Tris[i].ContainsVertex(floorPoint1.Position)) { Tris.RemoveAt(i); continue; }
                    if (Tris[i].ContainsVertex(floorPoint2.Position)) { Tris.RemoveAt(i); continue; }
                    if (Tris[i].ContainsVertex(floorPoint3.Position)) { Tris.RemoveAt(i); continue; }
                }

                foreach (Triangle t in Tris)
                {
                    AddIfNew(new Edge(t.U, t.V));
                    AddIfNew(new Edge(t.V, t.W));
                    AddIfNew(new Edge(t.W, t.U));
                }

            }
            else // Tetrahedral triangulation
            {
                #region Define Bounding Area

                Bounds baseVolume = new Bounds(Verts[0].Position, Vector3.zero);
                baseVolume.Encapsulate(min);
                baseVolume.Encapsulate(max);

                float volumeScaleHelper = Vector3.Distance(baseVolume.min, baseVolume.max);
                volumeScaleHelper *= allowFar;

                Vertex floorPoint1 = new Vertex(baseVolume.center + new Vector3(0f, -1f, -1f) * volumeScaleHelper);
                Vertex floorPoint2 = new Vertex(baseVolume.center + new Vector3(1f, -1f, 1f) * volumeScaleHelper);
                Vertex floorPoint3 = new Vertex(baseVolume.center + new Vector3(-1f, -1f, 1f) * volumeScaleHelper);
                Vertex topPoint = new Vertex(baseVolume.center + Vector3.up * volumeScaleHelper);

                Tetras.Add(new Tetrahedron(floorPoint1, floorPoint2, floorPoint3, topPoint));

                #endregion


                foreach (Vertex vertex in Verts)
                {
                    List<Triangle> triangles = new List<Triangle>();

                    foreach (Tetrahedron t in Tetras)
                        if (t.CircumCircleContains(vertex.Position))
                        {
                            t.Wrong = true;
                            triangles.Add(new Triangle(t.P1, t.P2, t.P3));
                            triangles.Add(new Triangle(t.P1, t.P2, t.P4));
                            triangles.Add(new Triangle(t.P1, t.P3, t.P4));
                            triangles.Add(new Triangle(t.P2, t.P3, t.P4));
                        }

                    for (int i = 0; i < triangles.Count; i++)
                        for (int j = i + 1; j < triangles.Count; j++)
                            if (Triangle.VeryClose(triangles[i], triangles[j]))
                            {
                                triangles[i].Wrong = true;
                                triangles[j].Wrong = true;
                            }


                    for (int i = Tetras.Count - 1; i >= 0; i--) if (Tetras[i].Wrong) Tetras.RemoveAt(i);

                    for (int i = triangles.Count - 1; i >= 0; i--) if (triangles[i].Wrong) triangles.RemoveAt(i);

                    foreach (Triangle triangle in triangles)
                        Tetras.Add(new Tetrahedron(triangle.U, triangle.V, triangle.W, vertex));
                }


                for (int t = Tetras.Count - 1; t >= 0; t--)
                {
                    if (Tetras[t].ContainsVertex(floorPoint1)) { Tetras.RemoveAt(t); continue; }
                    if (Tetras[t].ContainsVertex(floorPoint2)) { Tetras.RemoveAt(t); continue; }
                    if (Tetras[t].ContainsVertex(floorPoint3)) { Tetras.RemoveAt(t); continue; }
                    if (Tetras[t].ContainsVertex(topPoint)) { Tetras.RemoveAt(t); continue; }
                }

                foreach (Tetrahedron t in Tetras)
                {
                    AddIfNew(new Triangle(t.P1, t.P2, t.P3));
                    AddIfNew(new Triangle(t.P1, t.P2, t.P4));
                    AddIfNew(new Triangle(t.P1, t.P3, t.P4));
                    AddIfNew(new Triangle(t.P2, t.P3, t.P4));

                    AddIfNew(new Edge(t.P1, t.P2));
                    AddIfNew(new Edge(t.P2, t.P3));
                    AddIfNew(new Edge(t.P3, t.P1));
                    AddIfNew(new Edge(t.P4, t.P1));
                    AddIfNew(new Edge(t.P4, t.P2));
                    AddIfNew(new Edge(t.P4, t.P3));
                }
            }

        }

        public DeloneField Copy()
        {
            DeloneField copy = MemberwiseClone() as DeloneField;
            PGGUtils.TransferFromListToList(Verts, copy.Verts);
            PGGUtils.TransferFromListToList(Edges, copy.Edges);
            PGGUtils.TransferFromListToList(Tris, copy.Tris);
            PGGUtils.TransferFromListToList(Tetras, copy.Tetras);
            return copy;
        }

        private void AddIfNew(Triangle t)
        {
            if (!Tris.Contains(t)) Tris.Add(t);
        }

        private void AddIfNew(Edge e)
        {
            if (!Edges.Contains(e)) Edges.Add(e);
        }


#pragma warning disable


        #region Vertex (single position) Helper Class


        public class Vertex : IEquatable<Vertex>
        {
            public Vector3 Position { get; private set; }
            public float x { get { return Position.x; } }
            public float y { get { return Position.y; } }
            public float z { get { return Position.z; } }
            public float sqrMagn { get { return Position.sqrMagnitude; } }

            public Vertex() { }

            public Vertex(Vector3 position)
            {
                Position = position;
            }

            public override bool Equals(object obj)
            {
                if (obj is Vertex v) return Position == v.Position;
                return false;
            }

            public bool Equals(Vertex other)
            {
                return Position == other.Position;
            }

        }

        public class Vertex<T> : Vertex
        {
            public T Item { get; private set; }

            public Vertex(T item)
            {
                Item = item;
            }

            public Vertex(Vector3 position, T item) : base(position)
            {
                Item = item;
            }
        }


        #endregion



        #region Edge Helper Class



        public class Edge
        {
            public Vertex S, E;
            public float Length;
            public bool Wrong;
            /// <summary> For custom use </summary>
            public bool Used;

            public Edge(Vertex s, Vertex e)
            {
                S = s; E = e;
                Length = Vector3.Distance(s.Position, e.Position);
                Used = false;
            }

            public static bool operator ==(Edge a, Edge b)
            {
                return (a.S == b.S || a.S == b.E) && (a.E == b.S || a.E == b.E);
            }

            public static bool operator !=(Edge a, Edge b)
            {
                return !(a == b);
            }

            public static bool VeryCloseZ(Edge a, Edge b)
            {
                return DeloneField.VeryCloseZ(a.S, b.S) && DeloneField.VeryCloseZ(a.E, b.E) || DeloneField.VeryCloseZ(a.S, b.E) && DeloneField.VeryCloseZ(a.E, b.S);
            }
        }




        #endregion



        #region Triangle helper class



        public class Triangle
        {
            public Vertex U { get; set; }
            public Vertex V { get; set; }
            public Vertex W { get; set; }

            public bool Wrong { get; set; }

            public Triangle() { }
            public Triangle(Vertex u, Vertex v, Vertex w)
            {
                U = u; V = v; W = w;
            }

            public bool ContainsVertex(Vector3 v)
            {
                return Vector3.Distance(v, U.Position) < 0.001f || Vector3.Distance(v, V.Position) < 0.001f || Vector3.Distance(v, W.Position) < 0.001f;
            }

            public static bool operator ==(Triangle a, Triangle b)
            {
                return (a.U == b.U || a.U == b.V || a.U == b.W) && (a.V == b.U || a.V == b.V || a.V == b.W) && (a.W == b.U || a.W == b.V || a.W == b.W);
            }

            public static bool operator !=(Triangle a, Triangle b)
            {
                return !(a == b);
            }

            public static bool VeryClose(Triangle a, Triangle b)
            {
                return (DeloneField.VeryClose(a.U, b.U) || DeloneField.VeryClose(a.U, b.V) || DeloneField.VeryClose(a.U, b.W))
                    && (DeloneField.VeryClose(a.V, b.U) || DeloneField.VeryClose(a.V, b.V) || DeloneField.VeryClose(a.V, b.W))
                    && (DeloneField.VeryClose(a.W, b.U) || DeloneField.VeryClose(a.W, b.V) || DeloneField.VeryClose(a.W, b.W));
            }

            public bool CircumCircleContainsZ(Vector3 v)
            {
                Vector3 a = U.Position, b = V.Position, c = W.Position;
                float ab = a.sqrMagnitude, cd = b.sqrMagnitude, ef = c.sqrMagnitude;

                float circX = (ab * (c.z - b.z) + cd * (a.z - c.z) + ef * (b.z - a.z)) / (a.x * (c.z - b.z) + b.x * (a.z - c.z) + c.x * (b.z - a.z));
                float circY = (ab * (c.x - b.x) + cd * (a.x - c.x) + ef * (b.x - a.x)) / (a.z * (c.x - b.x) + b.z * (a.x - c.x) + c.z * (b.x - a.x));

                Vector3 circ = new Vector3(circX / 2f, 0, circY / 2f);
                float circRadius = Vector3.SqrMagnitude(a - circ);
                float dist = Vector3.SqrMagnitude(v - circ);

                return dist <= circRadius;
            }


        }




        #endregion



        #region Tetrahedron Helper Class


        public class Tetrahedron
        {
            public Vertex P1, P2, P3, P4;

            Vector3 Center;
            float RadiusSquare;

            public bool Wrong;

            public Tetrahedron(Vertex a, Vertex b, Vertex c, Vertex d)
            {
                P1 = a; P2 = b; P3 = c; P4 = d;
                CalculateTetraBounds();
            }

            void CalculateTetraBounds()
            {
                float aSqr = P1.sqrMagn, bSqr = P2.sqrMagn, cSqr = P3.sqrMagn, dSqr = P4.sqrMagn;

                #region Determinate Shape

                Matrix4x4 mx = new Matrix4x4
                (
                    new Vector4(P1.x, P2.x, P3.x, P4.x),
                    new Vector4(P1.y, P2.y, P3.y, P4.y),
                    new Vector4(P1.z, P2.z, P3.z, P4.z),
                    new Vector4(1, 1, 1, 1)
                );

                float a = mx.determinant;

                mx = new Matrix4x4
                (
                    new Vector4(aSqr, bSqr, cSqr, dSqr),
                    new Vector4(P1.y, P2.y, P3.y, P4.y),
                    new Vector4(P1.z, P2.z, P3.z, P4.z),
                    new Vector4(1, 1, 1, 1)
                );

                float deternX = mx.determinant;

                mx = new Matrix4x4
                (
                    new Vector4(aSqr, bSqr, cSqr, dSqr),
                    new Vector4(P1.x, P2.x, P3.x, P4.x),
                    new Vector4(P1.z, P2.z, P3.z, P4.z),
                    new Vector4(1, 1, 1, 1)
                );

                float deternY = -mx.determinant;

                mx = new Matrix4x4
                (
                    new Vector4(aSqr, bSqr, cSqr, dSqr),
                    new Vector4(P1.x, P2.x, P3.x, P4.x),
                    new Vector4(P1.y, P2.y, P3.y, P4.y),
                    new Vector4(1, 1, 1, 1)
                );

                float deternZ = mx.determinant;

                mx = new Matrix4x4
                (
                    new Vector4(aSqr, bSqr, cSqr, dSqr),
                    new Vector4(P1.x, P2.x, P3.x, P4.x),
                    new Vector4(P1.y, P2.y, P3.y, P4.y),
                    new Vector4(P1.z, P2.z, P3.z, P4.z)
                );

                float c = mx.determinant;

                #endregion

                Center = new Vector3();
                Center.x = deternX / (2 * a);
                Center.y = deternY / (2 * a);
                Center.z = deternZ / (2 * a);

                RadiusSquare = ((deternX * deternX) + (deternY * deternY) + (deternZ * deternZ) - (4 * a * c)) / (4 * a * a);
            }

            public bool ContainsVertex(Vertex v)
            {
                return VeryClose(v, P1) || VeryClose(v, P2) || VeryClose(v, P3) || VeryClose(v, P4);
            }

            public bool CircumCircleContains(Vector3 v)
            {
                return Vector3.SqrMagnitude(v - Center) <= RadiusSquare;
            }

            public static bool operator ==(Tetrahedron a, Tetrahedron b)
            {
                return (a.P1 == b.P1 || a.P1 == b.P2 || a.P1 == b.P3 || a.P1 == b.P4)
                    && (a.P2 == b.P1 || a.P2 == b.P2 || a.P2 == b.P3 || a.P2 == b.P4)
                    && (a.P3 == b.P1 || a.P3 == b.P2 || a.P3 == b.P3 || a.P3 == b.P4)
                    && (a.P4 == b.P1 || a.P4 == b.P2 || a.P4 == b.P3 || a.P4 == b.P4);
            }

            public static bool operator !=(Tetrahedron a, Tetrahedron b) { return !(a == b); }

        }



        #endregion


#pragma warning enable


        #region Utility Methods


        static bool VeryClose(Vertex a, Vertex b)
        {
            return (a.Position - b.Position).sqrMagnitude < 0.0001f;
        }

        static bool VeryCloseZ(Vertex a, Vertex b)
        {
            return VeryClose(a.Position.x, b.Position.x) && VeryClose(a.Position.z, b.Position.z);
        }

        static bool VeryClose(float x, float y)
        {
            return Mathf.Abs(x - y) <= float.Epsilon * Mathf.Abs(x + y) * 2 || Mathf.Abs(x - y) < float.MinValue;
        }


        #endregion


        #region Extra Methods


        public static List<Edge> MinimumEdgesAllVertexConnectionGroup(List<Edge> Edges, Vertex start, Func<Vertex, Vertex, bool> checkIfAllowed = null )
        {
            if (Edges.Count == 0) return null;

            List<Vertex> openList = new List<Vertex>();
            for (int i = 0; i < Edges.Count; i++) 
            { 
                if (!openList.Contains(Edges[i].S)) openList.Add(Edges[i].S);
                if (!openList.Contains(Edges[i].E)) openList.Add(Edges[i].E); 
            }

            List<Vertex> stolenList = new List<Vertex>();
            stolenList.Add(start);

            List<Edge> results = new List<Edge>();

            while (openList.Count > 0)
            {
                bool done = false;
                float nearestV = float.MaxValue;
                Edge nearest = null;

                foreach (Edge edge in Edges)
                {
                    int closedVertices = 0;

                    if (!stolenList.Contains(edge.S)) closedVertices += 1;
                    if (!stolenList.Contains(edge.E)) closedVertices += 1;

                    if (closedVertices != 1) continue;

                    if (checkIfAllowed != null)
                    {
                        if (checkIfAllowed.Invoke(edge.S, edge.E) == false) continue;
                    }

                    if (edge.Length < nearestV)
                    {
                        nearest = edge;
                        done = true;
                        nearestV = edge.Length;
                    }
                }

                if (!done) break;

                results.Add(nearest);
                openList.Remove(nearest.S);
                openList.Remove(nearest.E);
                stolenList.Add(nearest.S);
                stolenList.Add(nearest.E);
            }

            return results;
        }

        #endregion


    }
}