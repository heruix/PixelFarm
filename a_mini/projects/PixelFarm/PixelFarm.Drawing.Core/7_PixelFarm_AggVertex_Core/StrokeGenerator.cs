//BSD, 2014-2017, WinterDev
//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using PixelFarm.VectorMath;

namespace PixelFarm.Agg
{
    static class MyMath
    {

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="degrees">An angle in degrees</param>
        /// <returns>The angle expressed in radians</returns>
        public static double DegreesToRadians(double degrees)
        {

            return degrees * System.Math.PI / 180.0f;
        }
        public static double RadToDegrees(double rad)
        {

            return rad * (180.0f / System.Math.PI);
        }
        public static bool MinDistanceFirst(Vector2 baseVec, Vector2 compare0, Vector2 compare1)
        {
            return (SquareDistance(baseVec, compare0) < SquareDistance(baseVec, compare1)) ? true : false;
        }

        public static double SquareDistance(Vector2 v0, Vector2 v1)
        {
            double xdiff = v1.X - v0.X;
            double ydiff = v1.Y - v0.Y;
            return (xdiff * xdiff) + (ydiff * ydiff);
        }
        public static int Min(double v0, double v1, double v2)
        {
            //find min of 3
            unsafe
            {
                double* doubleArr = stackalloc double[3];
                doubleArr[0] = v0;
                doubleArr[1] = v1;
                doubleArr[2] = v2;

                double min = double.MaxValue;
                int foundAt = 0;
                for (int i = 0; i < 3; ++i)
                {
                    if (doubleArr[i] < min)
                    {
                        foundAt = i;
                        min = doubleArr[i];
                    }
                }
                return foundAt;
            }

        }

        /// <summary>
        /// find parameter A,B,C from Ax + By = C, with given 2 points
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        static void FindABC(Vector2 p0, Vector2 p1, out double a, out double b, out double c)
        {
            //line is in the form
            //Ax + By = C 
            //from http://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
            //and https://www.topcoder.com/community/data-science/data-science-tutorials/geometry-concepts-line-intersection-and-its-applications/
            a = p1.Y - p0.Y;
            b = p0.X - p1.X;
            c = a * p0.X + b * p0.Y;
        }
        public static bool FindCutPoint(
              Vector2 p0, Vector2 p1,
              Vector2 p2, Vector2 p3, out Vector2 result)
        {
            //TODO: review here
            //from http://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
            //and https://www.topcoder.com/community/data-science/data-science-tutorials/geometry-concepts-line-intersection-and-its-applications/

            //------------------------------------------
            //use matrix style ***
            //------------------------------------------
            //line is in the form
            //Ax + By = C
            //so   A1x +B1y= C1 ... line1
            //     A2x +B2y=C2  ... line2
            //------------------------------------------
            //
            //from Ax+By=C ... (1)
            //By = C- Ax;

            double a1, b1, c1;
            FindABC(p0, p1, out a1, out b1, out c1);

            double a2, b2, c2;
            FindABC(p2, p3, out a2, out b2, out c2);

            double delta = a1 * b2 - a2 * b1; //delta is the determinant in math parlance
            if (delta == 0)
            {
                //"Lines are parallel"
                result = Vector2.Zero;
                return false; //
                throw new System.ArgumentException("Lines are parallel");
            }
            double x = (b2 * c1 - b1 * c2) / delta;
            double y = (a1 * c2 - a2 * c1) / delta;
            result = new Vector2((float)x, (float)y);
            return true; //has cutpoint
        }


        static double FindB(Vector2 p0, Vector2 p1)
        {

            double m1 = (p1.Y - p0.Y) / (p1.X - p0.X);
            //y = mx + b ...(1)
            //b = y- mx

            //substitue with known value to gett b 
            //double b0 = p0.Y - (slope_m) * p0.X;
            //double b1 = p1.Y - (slope_m) * p1.X;
            //return b0;

            return p0.Y - (m1) * p0.X;
        }


    }


    class LineJoiner
    {
        //create a joint between 2 line 
        public LineJoiner()
        {

        }
        double x0, y0, x1, y1, x2, y2;

        public LineJoin LineJoinKind { get; set; }
        public double HalfWidth { get; set; }

        /// <summary>
        /// set input line (x0,y0)-> (x1,y1) and output line (x1,y1)-> (x2,y2)
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        public void SetControlVectors(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            this.x0 = x0;
            this.y0 = y0;
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }


        public void BuildJointVertex(
            List<Vector> positiveSideVectors,
            List<Vector> negativeSideVectors)
        {
            if (LineJoinKind == LineJoin.Bevel) return;

            //--------------------------------------------------------------
            Vector2 v0v1 = new Vector2(x1 - x0, y1 - y0);
            Vector2 v1v2 = new Vector2(x2 - x1, y2 - y1);

            Vector2 delta_v0v1 = v0v1.RotateInDegree(90).NewLength(HalfWidth);
            Vector2 delta_v1v2 = v1v2.RotateInDegree(90).NewLength(HalfWidth);
            //check inner or outter joint: by check angle positive or negative

            double rad_v0v1 = Math.Atan2(v0v1.y, v0v1.x);
            double rad_v1v2 = Math.Atan2(v1v2.y, v1v2.x);
            double angle_rad_diff = rad_v1v2 - rad_v0v1;

            if (positiveSideVectors != null)
            {

                Vector2 cutPoint;
                if (MyMath.FindCutPoint(
                       new Vector2(x0 + delta_v0v1.x, y0 + delta_v0v1.y),
                       new Vector2(x1 + delta_v0v1.x, y1 + delta_v0v1.y),
                       new Vector2(x1 + delta_v1v2.x, y1 + delta_v1v2.y),
                       new Vector2(x2 + delta_v1v2.x, y2 + delta_v1v2.y),
                   out cutPoint))
                {

                    if (angle_rad_diff > 0)
                    {
                        //'ACUTE' angle side 
                        //------------
                        //inner join
                        //v0v1 => v1v2 is inner angle for positive side
                        //and is outter angle of negative side 
                        //inside joint share the same cutpoint
                        positiveSideVectors.Add(new Vector(cutPoint.x, cutPoint.y));
                    }
                    else if (angle_rad_diff < 0)
                    {
                        //'OBTUSE' angle side
                        //-------------------     
                        switch (LineJoinKind)
                        {
                            default: throw new NotSupportedException();
                            case LineJoin.Round:

                                ArcGenerator.GenerateArcNew(positiveSideVectors,
                                    x1, y1,
                                    delta_v0v1,
                                    angle_rad_diff);

                                break;
                            case LineJoin.Miter:

                                positiveSideVectors.Add(new Vector(cutPoint.x, cutPoint.y));

                                break;
                        }

                    }
                    else
                    {
                        //angle =0 , same line

                    }
                }
                else
                {
                    //the 2 not cut
                }
            }
            //----------------------------------------------------------------
            if (negativeSideVectors != null)
            {
                //input point

                delta_v0v1 = -delta_v0v1; //change vector direction
                delta_v1v2 = -delta_v1v2;

                //-------------
                Vector2 cutPoint;
                if (MyMath.FindCutPoint(
                        new Vector2(x0 + delta_v0v1.x, y0 + delta_v0v1.y),
                        new Vector2(x1 + delta_v0v1.x, y1 + delta_v0v1.y),
                        new Vector2(x1 + delta_v1v2.x, y1 + delta_v1v2.y),
                        new Vector2(x2 + delta_v1v2.x, y2 + delta_v1v2.y),
                    out cutPoint))
                {

                    if (angle_rad_diff > 0)
                    {
                        //'ACUTE' angle side 
                        //for negative side, this is outter join
                        //-------------------     
                        switch (LineJoinKind)
                        {
                            case LineJoin.Round:
                                ArcGenerator.GenerateArcNew(negativeSideVectors,
                                    x1, y1,
                                    delta_v0v1,
                                    angle_rad_diff);
                                break;
                            case LineJoin.Miter:
                                negativeSideVectors.Add(new Vector(cutPoint.x, cutPoint.y));
                                break;
                        }
                    }
                    else if (angle_rad_diff < 0)
                    {
                        //'OBTUSE' angle side
                        //------------ 
                        //for negative side, this is outter join
                        //inner join share the same cutpoint
                        negativeSideVectors.Add(new Vector(cutPoint.x, cutPoint.y));
                    }
                    else
                    {

                    }
                }
                else
                {
                    //the 2 not cut
                }

            }
        }
    }


    class EdgeLine
    {

        LineJoiner _lineJoiner;

        double latest_moveto_x;
        double latest_moveto_y;
        double positiveSide, negativeSide;
        //line core (x0,y0) ->  (x1,y1) -> (x2,y2)
        double x0, y0, x1, y1;
        Vector delta0, delta1;
        Vector e1_positive;
        Vector e1_negative;
        Vector line_vector; //latest line vector
        int coordCount = 0;
        double first_lineto_x, first_lineto_y;

        public EdgeLine()
        {
            _lineJoiner = new LineJoiner();
            _lineJoiner.LineJoinKind = LineJoin.Bevel;
        }

        public void SetEdgeWidths(double positiveSide, double negativeSide)
        {
            this.positiveSide = positiveSide;
            this.negativeSide = negativeSide;
            _lineJoiner.HalfWidth = positiveSide;
        }
        void AcceptLatest()
        {
            //TODO: rename this method
            this.x0 = x1;
            this.y0 = y1;
        }
        public void MoveTo(double x0, double y0)
        {
            //reset data
            coordCount = 0;
            latest_moveto_x = x0;
            latest_moveto_y = y0;
            this.x0 = x0;
            this.y0 = y0;

        }

        public void LineTo(double x1, double y1,
            List<Vector> outputPositiveSideList,
            List<Vector> outputNegativeSideList)
        {
            double ex0, ey0, ex0_n, ey0_n;
            if (coordCount > 0)
            {
                CreateLineJoin(x1, y1, outputPositiveSideList, outputNegativeSideList);
                AcceptLatest();
                ExactLineTo(x1, y1);
                //--------------------------------------------------

                //consider create joint here
                GetEdge0(out ex0, out ey0, out ex0_n, out ey0_n);
                //add to vectors
                outputPositiveSideList.Add(new Vector(ex0, ey0));
                outputPositiveSideList.Add(e1_positive);
                //
                outputNegativeSideList.Add(new Vector(ex0_n, ey0_n));
                outputNegativeSideList.Add(e1_negative);
            }
            else
            {
                ExactLineTo(x1, y1);
                GetEdge0(out ex0, out ey0, out ex0_n, out ey0_n);
                //add to vectors
                outputPositiveSideList.Add(new Vector(ex0, ey0));
                outputNegativeSideList.Add(new Vector(ex0_n, ey0_n));
            }
        }
        public void Close(
            double first_lineto_x, double first_lineto_y,
            List<Vector> outputPositiveSideList,
            List<Vector> outputNegativeSideList)
        {
            double ex0, ey0, ex0_n, ey0_n;
            CreateLineJoin(latest_moveto_x, latest_moveto_y, outputPositiveSideList, outputNegativeSideList);
            //
            ExactLineTo(latest_moveto_x, latest_moveto_y);
            if (coordCount > 1)
            {

                //consider create joint here
                GetEdge0(out ex0, out ey0, out ex0_n, out ey0_n);
                //add to vectors
                outputPositiveSideList.Add(new Vector(ex0, ey0));
                outputPositiveSideList.Add(e1_positive);
                //
                outputNegativeSideList.Add(new Vector(ex0_n, ey0_n));
                outputNegativeSideList.Add(e1_negative);
            }
            else
            {
                GetEdge0(out ex0, out ey0, out ex0_n, out ey0_n);
                //add to vectors
                outputPositiveSideList.Add(new Vector(ex0, ey0));
                outputPositiveSideList.Add(e1_positive);
                //
                outputNegativeSideList.Add(new Vector(ex0_n, ey0_n));
                outputNegativeSideList.Add(e1_negative);
            }

            //------------------------------------------
            CreateLineJoin(first_lineto_x, first_lineto_y, outputPositiveSideList, outputNegativeSideList);
            AcceptLatest();
            //------------------------------------------
        }
        void GetEdge0(out double ex0, out double ey0, out double ex0_n, out double ey0_n)
        {
            ex0 = x0 + delta0.X;
            ey0 = y0 + delta0.Y;
            ex0_n = x0 - delta0.X;
            ey0_n = y0 - delta0.Y;
        }

        void ExactLineTo(double x1, double y1)
        {

            //perpendicular line
            //create line vector
            line_vector = delta0 = new Vector(x1 - x0, y1 - y0);
            delta1 = delta0 = delta0.Rotate(90).NewLength(positiveSide);

            this.x1 = x1;
            this.y1 = y1;
            //------------------------------------------------------
            e1_positive = new Vector(x1 + delta1.X, y1 + delta1.Y);
            e1_negative = new Vector(x1 - delta1.X, y1 - delta1.Y);
            //------------------------------------------------------
            //create both positive and negative edge 
            coordCount++;
        }
        void CreateLineJoin(
           double previewX1,
           double previewY1,
           List<Vector> outputPositiveSideList,
           List<Vector> outputNegativeSideList)
        {

            if (_lineJoiner.LineJoinKind == LineJoin.Bevel)
            {
                Vector p = new Vector(x1, y1);
                outputPositiveSideList.Add(p + delta0);
                outputNegativeSideList.Add(p - delta0);

                return;
            }
            //------------------------------------------ 
            _lineJoiner.SetControlVectors(x0, y0, x1, y1, previewX1, previewY1);
            _lineJoiner.BuildJointVertex(outputPositiveSideList, outputNegativeSideList);
            //------------------------------------------ 
        }

    }


    public class StrokeGen2
    {

        EdgeLine currentEdgeLine = new EdgeLine();
        List<Vector> positiveSideVectors = new List<Vector>();
        List<Vector> negativeSideVectors = new List<Vector>();
        List<Vector> capVectors = new List<Vector>(); //temporary 


        float positiveSide, negativeSide;
        public StrokeGen2()
        {

            //
            //use 2 vertext list to store perpendicular outline 
            this.LineCapKind = LineCap.Square;
            SetEdgeWidth(0.5f, 0.5f);//default
        }

        public LineCap LineCapKind { get; set; }
        public void SetEdgeWidth(float positiveSide, float negativeSide)
        {
            this.positiveSide = positiveSide;
            this.negativeSide = negativeSide;
            //
            currentEdgeLine.SetEdgeWidths(positiveSide, negativeSide);
        }
        public void Generate(VertexStore srcVxs, VertexStore outputVxs)
        {
            //read data from src
            //generate stroke and 
            //write to output
            //-----------
            int cmdCount = srcVxs.Count;
            VertexCmd cmd;
            double x, y;

            positiveSideVectors.Clear();
            negativeSideVectors.Clear();

            double ex0, ey0, ex0_n, ey0_n;
            int current_coord_count = 0;
            double latest_moveto_x = 0;
            double latest_moveto_y = 0;
            double first_lineto_x = 0;
            double first_lineto_y = 0;
            for (int i = 0; i < cmdCount; ++i)
            {
                cmd = srcVxs.GetVertex(i, out x, out y);
                switch (cmd)
                {
                    case VertexCmd.LineTo:
                        {
                            if (current_coord_count > 1)
                            {
                                currentEdgeLine.LineTo(x, y, positiveSideVectors, negativeSideVectors);
                            }
                            else
                            {
                                currentEdgeLine.LineTo(first_lineto_x = x, first_lineto_y = y, positiveSideVectors, negativeSideVectors);

                            }

                            current_coord_count++;
                        }
                        break;
                    case VertexCmd.MoveTo:
                        //if we have current shape
                        //leave it and start the new shape
                        currentEdgeLine.MoveTo(latest_moveto_x = x, latest_moveto_y = y);
                        current_coord_count = 1;
                        break;
                    case VertexCmd.Close:
                    case VertexCmd.CloseAndEndFigure:
                        {
                            //------------------------------------------
                            currentEdgeLine.Close(first_lineto_x, first_lineto_y, positiveSideVectors, negativeSideVectors);



                            current_coord_count++;

                            WriteOutput(outputVxs, true);
                            current_coord_count = 0;
                        }//create line cap
                        break;
                    default:
                        break;
                }
            }
            //-------------
            if (current_coord_count > 0)
            {
                WriteOutput(outputVxs, false);
            }
        }
        void WriteOutput(VertexStore outputVxs, bool close)
        {

            //write output to 

            if (close)
            {
                int positive_edgeCount = positiveSideVectors.Count;
                int negative_edgeCount = negativeSideVectors.Count;

                int n = positive_edgeCount - 1;
                Vector v = positiveSideVectors[n];
                outputVxs.AddMoveTo(v.X, v.Y);
                for (; n >= 0; --n)
                {
                    v = positiveSideVectors[n];
                    outputVxs.AddLineTo(v.X, v.Y);
                }
                outputVxs.AddCloseFigure();
                //end ... create join to negative side
                //------------------------------------------ 
                //create line join from positive  to negative side
                v = negativeSideVectors[0];
                outputVxs.AddMoveTo(v.X, v.Y);
                n = 1;
                for (; n < negative_edgeCount; ++n)
                {
                    v = negativeSideVectors[n];
                    outputVxs.AddLineTo(v.X, v.Y);
                }
                //------------------------------------------
                //close
                outputVxs.AddCloseFigure();
            }
            else
            {

                int positive_edgeCount = positiveSideVectors.Count;
                int negative_edgeCount = negativeSideVectors.Count;

                //no a close shape stroke
                //create line cap for this
                //
                //positive
                Vector v = positiveSideVectors[0];
                //----------- 
                //1. moveto

                //2.
                CreateStartLineCap(outputVxs,
                    v,
                    negativeSideVectors[0], positiveSide);
                //-----------

                int n = 1;
                for (; n < positive_edgeCount; ++n)
                {
                    //increment n
                    v = positiveSideVectors[n];
                    outputVxs.AddLineTo(v.X, v.Y);
                }
                //negative 

                //---------------------------------- 
                CreateEndLineCap(outputVxs,
                    positiveSideVectors[positive_edgeCount - 1],
                    negativeSideVectors[negative_edgeCount - 1],
                    positiveSide);
                //----------------------------------
                for (n = negative_edgeCount - 2; n >= 0; --n)
                {
                    //decrement n
                    v = negativeSideVectors[n];
                    outputVxs.AddLineTo(v.X, v.Y);
                }

                outputVxs.AddCloseFigure();
            }
            //reset
            positiveSideVectors.Clear();
            negativeSideVectors.Clear();
        }


       
        void CreateStartLineCap(VertexStore outputVxs, Vector v0, Vector v1, double edgeWidth)
        {
            switch (this.LineCapKind)
            {
                default: throw new NotSupportedException();
                case LineCap.Butt:
                    outputVxs.AddMoveTo(v1.X, v1.Y);// moveto      
                    outputVxs.AddLineTo(v0.X, v0.Y);
                    break;
                case LineCap.Square:
                    {
                        Vector delta = (v0 - v1).Rotate(90).NewLength(edgeWidth);
                        //------------------------
                        outputVxs.AddMoveTo(v1.X + delta.X, v1.Y + delta.Y);
                        outputVxs.AddLineTo(v0.X + delta.X, v0.Y + delta.Y);
                    }
                    break;
                case LineCap.Round:
                    capVectors.Clear();
                    BuildBeginCap(v0.X, v0.Y, v1.X, v1.Y, capVectors);
                    //----------------------------------------------------
                    int j = capVectors.Count;
                    outputVxs.AddMoveTo(v1.X, v1.Y);
                    for (int i = j - 1; i >= 0; --i)
                    {
                        Vector v = capVectors[i];
                        outputVxs.AddLineTo(v.X, v.Y);
                    }
                    break;
            }
        }
        void CreateEndLineCap(VertexStore outputVxs, Vector v0, Vector v1, double edgeWidth)
        {
            switch (this.LineCapKind)
            {
                default: throw new NotSupportedException();
                case LineCap.Butt:
                    outputVxs.AddLineTo(v0.X, v0.Y);
                    outputVxs.AddLineTo(v1.X, v1.Y);

                    break;
                case LineCap.Square:
                    {
                        Vector delta = (v1 - v0).Rotate(90).NewLength(edgeWidth);
                        outputVxs.AddLineTo(v0.X + delta.X, v0.Y + delta.Y);
                        outputVxs.AddLineTo(v1.X + delta.X, v1.Y + delta.Y);
                    }
                    break;
                case LineCap.Round:
                    {
                        capVectors.Clear();
                        BuildEndCap(v0.X, v0.Y, v1.X, v1.Y, capVectors);
                        int j = capVectors.Count;
                        for (int i = j - 1; i >= 0; --i)
                        {
                            Vector v = capVectors[i];
                            outputVxs.AddLineTo(v.X, v.Y);
                        }
                    }
                    break;
            }
        }


        void BuildBeginCap(
          double x0, double y0,
          double x1, double y1,
          List<Vector> outputVectors)
        {
            switch (LineCapKind)
            {
                default: throw new NotSupportedException();
                case LineCap.Butt:
                    break;
                case LineCap.Square:
                    break;
                case LineCap.Round:

                    {
                        //------------------------
                        //x0,y0 -> begin of line 1
                        //x1,y1 -> begin of line 2
                        //------------------------ 
                        double c_x = (x0 + x1) / 2;
                        double c_y = (y0 + y1) / 2;
                        Vector2 delta = new Vector2(x0 - c_x, y0 - c_y);
                        ArcGenerator.GenerateArcNew(outputVectors,
                                     c_x, c_y, delta, MyMath.DegreesToRadians(180));
                    }
                    break;
            }

        }

        void BuildEndCap(
          double x0, double y0,
          double x1, double y1,
          List<Vector> outputVectors)
        {
            switch (LineCapKind)
            {
                default: throw new NotSupportedException();
                case LineCap.Butt:
                    break;
                case LineCap.Square:
                    break;
                case LineCap.Round:
                    {
                        //------------------------
                        //x0,y0 -> end of line 1 
                        //x1,y1 -> end of line 2
                        //------------------------ 
                        double c_x = (x0 + x1) / 2;
                        double c_y = (y0 + y1) / 2;
                        Vector2 delta = new Vector2(x1 - c_x, y1 - c_y);
                        ArcGenerator.GenerateArcNew(outputVectors,
                                     c_x, c_y, delta, MyMath.DegreesToRadians(180));
                    }
                    break;
            }
        }
    }


    static class ArcGenerator
    {
        //helper class for generate arc
        //
        public static void GenerateArcNew(List<Vector> output, double cx,
            double cy,
            Vector2 startDelta,
            double sweepAngleRad)
        {

            //TODO: review here ***
            int nsteps = 4;
            double eachStep = MyMath.RadToDegrees(sweepAngleRad) / nsteps;
            double angle = 0;
            for (int i = 0; i < nsteps; ++i)
            {

                Vector2 newPerpend = startDelta.RotateInDegree(angle);
                Vector2 newpos = new Vector2(cx + newPerpend.x, cy + newPerpend.y);
                output.Add(new Vector(newpos.x, newpos.y));
                angle += eachStep;
            }
        }
    }


}