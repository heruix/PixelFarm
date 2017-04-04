﻿//MIT, 2017, WinterDev
using System;
namespace Typography.Rendering
{
    public enum BoneDirection : byte
    {
        /// <summary>
        /// 0 degree direction (horizontal left to right)
        /// </summary>
        D0,
        D45,
        D90,
        D135,
        D180,
        D225,
        D270,
        D315
    }
    /// <summary>
    /// a line that connects between centroid of 2 GlyphTriangle(p => q)
    /// </summary>
    public class GlyphBone
    {
        public readonly GlyphTriangle p, q;
        public readonly double boneLength;

        public GlyphBone(GlyphTriangle p, GlyphTriangle q)
        {
            this.p = p;
            this.q = q;

            double dy = q.CentroidY - p.CentroidY;
            double dx = q.CentroidX - p.CentroidX;
            this.boneLength = Math.Sqrt(
                (dy * dy) + (dx * dx)
                );
        }
        public double SlopAngle { get; private set; }
        public bool IsLongBone { get; set; }

        public LineSlopeKind SlopKind { get; private set; }

        static void CalculateMidPoint(EdgeLine e, out double midX, out double midY)
        {
            midX = (e.x0 + e.x1) / 2;
            midY = (e.y0 + e.y1) / 2;
        }

        public void Analyze()
        {

            //
            //p => (x0,y0)
            //q => (x1,y1)
            //line move from p to q 
            //...
            //tasks:
            //1. find slop angle
            //2. find slope kind



            //check if q is upper or lower when compare with p
            //check if q is on left side or right side of p
            //then we know the direction
            //....
            //p
            double x0 = p.CentroidX;
            double y0 = p.CentroidY;
            //q
            double x1 = q.CentroidX;
            double y1 = q.CentroidY;

            if (x1 == x0)
            {
                this.SlopKind = LineSlopeKind.Vertical;
                SlopAngle = 1;
            }
            else
            {
                SlopAngle = Math.Abs(Math.Atan2(Math.Abs(y1 - y0), Math.Abs(x1 - x0)));
                if (SlopAngle > _85degreeToRad)
                {
                    SlopKind = LineSlopeKind.Vertical;
                }
                else if (SlopAngle < _8degreeToRad) //_15degreeToRad
                {
                    SlopKind = LineSlopeKind.Horizontal;
                }
                else
                {
                    SlopKind = LineSlopeKind.Other;
                }
            }
            //--------------------------------------
            //for p and q, count number of outside edge
            //if outsideEdgeCount of triangle >=2 -> this triangle is tip part

            int p_outsideEdgeCount = OutSideEdgeCount(p);
            int q_outsideEdgeCount = OutSideEdgeCount(q);
            bool p_isTip = false;
            bool q_isTip = false;

            if (p_outsideEdgeCount >= 2)
            {
                //tip bone
                p_isTip = true;
            }
            if (q_outsideEdgeCount >= 2)
            {
                //tipbone
                q_isTip = true;
            }
            //-------------------------------------- 
            //p_isTip && q_isTip is possible eg. dot or dot of  i etc.
            //-------------------------------------- 
            //find matching side:
            //the bone connects between triangle p and q (via centroid)
            //

            MarkEdgeSide(p.e0, q);
            MarkEdgeSide(p.e1, q);
            MarkEdgeSide(p.e2, q);
            //
            MarkEdgeSide(q.e0, p);
            MarkEdgeSide(q.e1, p);
            MarkEdgeSide(q.e2, p);


            //if (p.e0.IsOutside)
            //{
            //    //find matching side on q
            //    MarkMatchingOutsideEdge(p.e0, q);
            //}
            //else
            //{
            //    //e0 is inside
            //    foundMatchingInside = MarkMatchingInsideEdge(p.e0, q);
            //}

            //if (p.e1.IsOutside)
            //{
            //    //find matching side on q   
            //    MarkMatchingOutsideEdge(p.e1, q);
            //}
            //if (p.e2.IsOutside)
            //{
            //    //find matching side on q
            //    MarkMatchingOutsideEdge(p.e2, q);
            //}
            ////-------------------------------------- 

            //if (q.e0.IsOutside)
            //{
            //    //find matching side on q
            //    MarkMatchingOutsideEdge(q.e0, p);
            //}
            //if (q.e1.IsOutside)
            //{
            //    //find matching side on q   
            //    MarkMatchingOutsideEdge(q.e1, p);
            //}
            //if (q.e2.IsOutside)
            //{
            //    //find matching side on q
            //    MarkMatchingOutsideEdge(q.e2, p);
            //}
            ////--------------------------------------  
        }
        static void MarkEdgeSide(EdgeLine edgeLine, GlyphTriangle another)
        {
            if (edgeLine.IsOutside)
            {
                MarkMatchingOutsideEdge(edgeLine, another);
            }
            else
            {
                //inside
                MarkMatchingInsideEdge(edgeLine, another);
            }
        }
        static bool MarkMatchingInsideEdge(EdgeLine insideEdge, GlyphTriangle another)
        {
             
            //side-by-side 

            if (another.e0.IsInside && MarkMatchingInsideEdge(insideEdge, another.e0))
            {
                //inside                 
                return true;
            }
            //
            if (another.e1.IsInside && MarkMatchingInsideEdge(insideEdge, another.e1))
            {
                return true;
            }
            //
            if (another.e2.IsInside && MarkMatchingInsideEdge(insideEdge, another.e2))
            {
                //check matching slope and coord?
                return true;
            }
            return false;
        }
        static bool MarkMatchingInsideEdge(EdgeLine p_edge, EdgeLine q_edge)
        {

            bool x_axis = false;
            if (p_edge.x0 == q_edge.x0 && p_edge.x1 == q_edge.x1)
            {
                x_axis = true;
            }
            else if (p_edge.x0 == q_edge.x1 && p_edge.x1 == q_edge.x0)
            {
                x_axis = true;
            }
            else
            {
                return false; //no match in x-axis
            }
            //--------------------------------
            //y_axis
            if (p_edge.y0 == q_edge.y0 && p_edge.y1 == q_edge.y1)
            {
                p_edge.contactToEdge = q_edge;
            }
            else if (p_edge.y0 == q_edge.y1 && p_edge.y1 == q_edge.y0)
            {
                p_edge.contactToEdge = q_edge;
            }
            else
            {
                return false; //no match in y-axis
            }
            //--------------------------------
            return true;
        }
        static void MarkMatchingOutsideEdge(EdgeLine targetEdge, GlyphTriangle q)
        {

            EdgeLine matchingEdgeLine;
            int matchingEdgeSideNo;
            if (FindMatchingOuterSide(targetEdge, q, out matchingEdgeLine, out matchingEdgeSideNo))
            {
                //assign matching edge line   
                //mid point of each edge
                //p-triangle's edge midX,midY
                double pe_midX, pe_midY;
                CalculateMidPoint(targetEdge, out pe_midX, out pe_midY);
                //q-triangle's edge midX,midY
                double qe_midX, qe_midY;
                CalculateMidPoint(matchingEdgeLine, out qe_midX, out qe_midY);

                if (targetEdge.SlopKind == LineSlopeKind.Vertical)
                {
                    //TODO: review same side edge (Fan shape)
                    if (pe_midX < qe_midX)
                    {
                        targetEdge.IsLeftSide = true;
                        if (matchingEdgeLine.IsOutside && matchingEdgeLine.SlopKind == LineSlopeKind.Vertical)
                        {
                            targetEdge.AddMatchingOutsideEdge(matchingEdgeLine);
                        }
                    }
                    else
                    {
                        //matchingEdgeLine.IsLeftSide = true;
                        if (matchingEdgeLine.IsOutside && matchingEdgeLine.SlopKind == LineSlopeKind.Vertical)
                        {
                            targetEdge.AddMatchingOutsideEdge(matchingEdgeLine);
                        }
                    }
                }
                else if (targetEdge.SlopKind == LineSlopeKind.Horizontal)
                {
                    //TODO: review same side edge (Fan shape)

                    if (pe_midY > qe_midY)
                    {
                        //p side is upper , q side is lower
                        if (targetEdge.SlopKind == LineSlopeKind.Horizontal)
                        {
                            targetEdge.IsUpper = true;
                            if (matchingEdgeLine.IsOutside && matchingEdgeLine.SlopKind == LineSlopeKind.Horizontal)
                            {
                                targetEdge.AddMatchingOutsideEdge(matchingEdgeLine);
                            }
                        }
                    }
                    else
                    {
                        if (matchingEdgeLine.SlopKind == LineSlopeKind.Horizontal)
                        {
                            // matchingEdgeLine.IsUpper = true;
                            if (matchingEdgeLine.IsOutside && matchingEdgeLine.SlopKind == LineSlopeKind.Horizontal)
                            {
                                targetEdge.AddMatchingOutsideEdge(matchingEdgeLine);
                            }
                        }
                    }
                }
            }
        }




        static bool FindMatchingOuterSide(EdgeLine compareEdge,
            GlyphTriangle another,
            out EdgeLine result,
            out int edgeIndex)
        {
            //compare by radian of edge line
            double compareSlope = Math.Abs(compareEdge.SlopAngle);
            double diff0 = double.MaxValue;
            double diff1 = double.MaxValue;
            double diff2 = double.MaxValue;

            diff0 = Math.Abs(Math.Abs(another.e0.SlopAngle) - compareSlope);

            diff1 = Math.Abs(Math.Abs(another.e1.SlopAngle) - compareSlope);

            diff2 = Math.Abs(Math.Abs(another.e2.SlopAngle) - compareSlope);

            //find min
            int minDiffSide = FindMinIndex(diff0, diff1, diff2);
            if (minDiffSide > -1)
            {
                edgeIndex = minDiffSide;
                switch (minDiffSide)
                {
                    default: throw new NotSupportedException();
                    case 0:
                        result = another.e0;
                        break;
                    case 1:
                        result = another.e1;
                        break;
                    case 2:
                        result = another.e2;
                        break;
                }
                return true;
            }
            else
            {
                edgeIndex = -1;
                result = null;
                return false;
            }
        }
        static int FindMinIndex(double d0, double d1, double d2)
        {
            unsafe
            {
                double* tmpArr = stackalloc double[3];
                tmpArr[0] = d0;
                tmpArr[1] = d1;
                tmpArr[2] = d2;

                int minAt = -1;
                double currentMin = double.MaxValue;
                for (int i = 0; i < 3; ++i)
                {
                    double d = tmpArr[i];
                    if (d < currentMin)
                    {
                        currentMin = d;
                        minAt = i;
                    }
                }
                return minAt;
            }
        }
        /// <summary>
        /// count number of outside edge
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        static int OutSideEdgeCount(GlyphTriangle t)
        {
            int n = 0;
            n += t.e0.IsOutside ? 1 : 0;
            n += t.e1.IsOutside ? 1 : 0;
            n += t.e2.IsOutside ? 1 : 0;
            return n;
        }
        static readonly double _85degreeToRad = MyMath.DegreesToRadians(85);
        static readonly double _15degreeToRad = MyMath.DegreesToRadians(15);
        static readonly double _8degreeToRad = MyMath.DegreesToRadians(8);
        static readonly double _90degreeToRad = MyMath.DegreesToRadians(90);
        public override string ToString()
        {
            return p + " -> " + q;
        }
    }
}