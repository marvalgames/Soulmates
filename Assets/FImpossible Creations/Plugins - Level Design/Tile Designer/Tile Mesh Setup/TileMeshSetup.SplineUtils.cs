using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public partial class TileMeshSetup
    {

        public List<MeshShapePoint> GenerateMeshShape(List<CurvePoint> points, float splineLength, float qualityFactor, List<MeshShapePoint> buffer = null, ESubdivideCompute compute = ESubdivideCompute.AngleLimit)
        {
            if (buffer == null) buffer = new List<MeshShapePoint>(); else buffer.Clear();

            float maxDivs = 256f;
            float stepProgr = 1f / maxDivs;

            if (qualityFactor >= 30f) // just curve points
            {
                for (int i = 0; i < points.Count; i++)
                {
                    MeshShapePoint sp = new MeshShapePoint();
                    sp.p = points[i].localPos;
                    sp.c = points[i].VertexColor;
                    Vector2 p = sp.p;

                    sp = CurvePointNormalCompute(points, i, sp, p);
                    sp.p.z = points[i]._extra_z;

                    buffer.Add(sp);
                }
            }
            else if (compute == ESubdivideCompute.LengthLimit)
            {
                float divsCount = (32.0f - qualityFactor) * 1.75f;
                float stepLimitLength = splineLength / divsCount;
                float elapsed = 0f;
                CurvePoint prePoint = points[0];
                int nextCPoint = 1;

                for (int i = 0; i < maxDivs; i++)
                {
                    float progr = i * stepProgr;
                    MeshShapePoint sp = new MeshShapePoint();
                    sp.c = Color.white;
                    sp.p = CurvePoint.GetPosAt(points, progr, splineLength);

                    CurvePoint sampleFocus = CurvePoint._GetPosAt_Origin;
                    CurvePoint sampleOther = CurvePoint._GetPosAt_Other;

                    bool quickComp = sp.QuickNormalCompute(sampleFocus);

                    #region Vertex Color

                    if (sampleFocus.VertexColor != Color.white || sampleOther.VertexColor != Color.white)
                    {
                        float distA = Vector2.Distance(sp.p, sampleFocus.localPos) * sampleOther.VertexColorFalloff;
                        float distB = Vector2.Distance(sp.p, sampleOther.localPos) * sampleFocus.VertexColorFalloff;

                        if (distA < distB) // A nearer
                        {
                            sp.c = sampleFocus.VertexColor; //Color.Lerp(sampleFocus.VertexColor, sampleOther.VertexColor, 1f - (distA * sampleFocus.VertexColorFalloff));
                        }
                        else // B nearer
                        {
                            sp.c = sampleOther.VertexColor;//Color.Lerp(sampleOther.VertexColor, sampleFocus.VertexColor, 1f - (distB * sampleOther.VertexColorFalloff));
                        }
                    }

                    #endregion

                    sp.p.z = CurvePoint.GetZAt(points, progr, splineLength);

                    if (i == 0 || i == maxDivs - 1)
                    {
                        elapsed = 0f;
                        if (!quickComp) sp.ComputeNormal(i, maxDivs, sampleFocus, sampleOther, progr, points, stepProgr, splineLength);
                        buffer.Add(sp);
                    }
                    else
                    {
                        // Control points insert
                        if (nextCPoint < points.Count - 1)
                        {
                            if (progr > points[nextCPoint].distanceInSpline / splineLength)
                            {
                                // Add control point
                                MeshShapePoint spc = new MeshShapePoint();
                                spc.p = points[nextCPoint].localPos;
                                spc.c = points[nextCPoint].VertexColor;
                                spc.p.z = points[nextCPoint]._extra_z;
                                spc = CurvePointNormalCompute(points, nextCPoint, spc, spc.p);
                                buffer.Add(spc);
                                nextCPoint += 1;
                            }
                        }


                        if (elapsed > stepLimitLength)
                        {
                            elapsed = 0f;
                            if (!quickComp) sp.ComputeNormal(i, maxDivs, sampleFocus, sampleOther, progr, points, stepProgr, splineLength);
                            buffer.Add(sp);
                        }
                    }

                    elapsed += stepProgr * splineLength;
                }
            }
            else if (compute == ESubdivideCompute.AngleLimit)
            {
                Vector2 latestUsedNorm = Vector2.zero;
                float radianLim = qualityFactor * Mathf.Deg2Rad;

                float elapsed = 0f;
                float minStepLen = splineLength * Mathf.Lerp(0.075f, 0.025f,
                    Mathf.InverseLerp(30f, 1f, qualityFactor)); // To compute minimum distance between subdivs

                int nextCPoint = 1;

                for (int i = 0; i < maxDivs; i++)
                {
                    float progr = i * stepProgr;

                    MeshShapePoint sp = new MeshShapePoint();
                    sp.c = Color.white;

                    sp.p = CurvePoint.GetPosAt(points, progr, splineLength);

                    CurvePoint sampleFocus = CurvePoint._GetPosAt_Origin;
                    CurvePoint sampleOther = CurvePoint._GetPosAt_Other;

                    bool quickComp = sp.QuickNormalCompute(sampleFocus);

                    #region Vertex Color

                    if (sampleFocus.VertexColor != Color.white || sampleOther.VertexColor != Color.white)
                    {
                        float distA = Vector2.Distance(sp.p, sampleFocus.localPos) * sampleOther.VertexColorFalloff;
                        float distB = Vector2.Distance(sp.p, sampleOther.localPos) * sampleFocus.VertexColorFalloff;

                        if (distA < distB) // A nearer
                        {
                            sp.c = sampleFocus.VertexColor; //Color.Lerp(sampleFocus.VertexColor, sampleOther.VertexColor, 1f - (distA * sampleFocus.VertexColorFalloff));
                        }
                        else // B nearer
                        {
                            sp.c = sampleOther.VertexColor;//Color.Lerp(sampleOther.VertexColor, sampleFocus.VertexColor, 1f - (distB * sampleOther.VertexColorFalloff));
                        }
                    }

                    #endregion

                    sp.p.z = CurvePoint.GetZAt(points, progr, splineLength);

                    if (!quickComp) sp.ComputeNormal(i, maxDivs, sampleFocus, sampleOther, progr, points, stepProgr, splineLength);

                    float dot = Vector2.Dot(latestUsedNorm, Vector2.Perpendicular(sp.normal));

                    if (i == 0 || i == maxDivs - 1)
                    {
                        latestUsedNorm = sp.normal;
                        elapsed = 0f;
                        buffer.Add(sp);
                    }
                    else
                    {
                        // Control points insert
                        if (nextCPoint < points.Count-1 )
                        {
                            if (progr > points[nextCPoint].distanceInSpline / splineLength)
                            {
                                // Add control point
                                MeshShapePoint spc = new MeshShapePoint();
                                spc.p = points[nextCPoint].localPos;
                                spc.c = points[nextCPoint].VertexColor;
                                spc.p.z = points[nextCPoint]._extra_z;
                                spc = CurvePointNormalCompute(points, nextCPoint, spc, spc.p);
                                buffer.Add( spc);
                                nextCPoint += 1;
                            }
                        }

                        if (Mathf.Abs(dot) > radianLim)
                        {
                            if (elapsed > minStepLen)
                            {
                                elapsed = 0f;
                                latestUsedNorm = sp.normal;
                                buffer.Add(sp);
                            }
                        }
                    }

                    elapsed += stepProgr * splineLength;

                }

                // Fill with control points

                //for (int i = 1; i < points.Count - 1; i++)
                //{
                //    float targetDist = points[i].distanceInSpline;
                //    float belapsed = 0f;

                //    for (int b = 1; b < buffer.Count; b++)
                //    {
                //        belapsed += Vector2.Distance(buffer[b - 1].p, buffer[b].p);
                //        if (belapsed > targetDist)
                //        {
                //            // Add control point
                //            MeshShapePoint spc = new MeshShapePoint();
                //            spc.p = points[i].localPos;
                //            spc.c = points[i].VertexColor;
                //            spc.p.z = points[i]._extra_z;
                //            //spc.ComputeNormal(i, maxDivs, sampleFocus, sampleOther, progr, points, stepProgr, splineLength);
                //            buffer.Insert(b, spc);

                //            break;
                //        }
                //    }
                //}

            }

            return buffer;
        }


        MeshShapePoint CurvePointNormalCompute(List<CurvePoint> points, int i, MeshShapePoint sp, Vector2 p)
        {
            if (points[i].overrideNormal != Vector2.zero)
            {
                sp.QuickNormalCompute(points[i]);
                //sp.normal = points[i].overrideNormal.normalized;
                //sp.normal.y = -sp.normal.y;
            }
            else
            {
                if (points.Count > 1)
                {
                    if (i == 0)
                    {
                        Vector2 dir = p - points[i + 1].localPos;
                        dir = Vector2.Perpendicular(dir.normalized);
                        sp.normal = dir;
                    }
                    else if (i == points.Count - 1)
                    {
                        Vector2 dir = p - points[i - 1].localPos;
                        dir = Vector2.Perpendicular(-dir.normalized);
                        sp.normal = dir;
                    }
                    else
                    {
                        Vector2 dir = points[i - 1].localPos - points[i + 1].localPos;
                        sp.normal = Vector2.Perpendicular(dir.normalized);
                    }
                }
            }

            return sp;
        }


    }
}