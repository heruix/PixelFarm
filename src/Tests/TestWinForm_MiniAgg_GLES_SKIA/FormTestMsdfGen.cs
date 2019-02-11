﻿//MIT, 2019-present, WinterDev
using System;
using System.Collections.Generic;
using System.Drawing;

using System.IO;
using System.Windows.Forms;

using Typography.OpenFont;
using Typography.Rendering;
using Typography.Contours;

using PixelFarm.Drawing;
using PixelFarm.CpuBlit;
using PixelFarm.CpuBlit.VertexProcessing;

namespace Mini
{
    public partial class FormTestMsdfGen : Form
    {
        public FormTestMsdfGen()
        {
            InitializeComponent();
        }


        static void CreateSampleMsdfTextureFont(string fontfile, float sizeInPoint, ushort startGlyphIndex, ushort endGlyphIndex, string outputFile)
        {
            //sample
            var reader = new OpenFontReader();

            using (var fs = new FileStream(fontfile, FileMode.Open))
            {
                //1. read typeface from font file
                Typeface typeface = reader.Read(fs);
                //sample: create sample msdf texture 
                //-------------------------------------------------------------
                var builder = new GlyphPathBuilder(typeface);
                //builder.UseTrueTypeInterpreter = this.chkTrueTypeHint.Checked;
                //builder.UseVerticalHinting = this.chkVerticalHinting.Checked;
                //-------------------------------------------------------------
                var atlasBuilder = new SimpleFontAtlasBuilder();


                for (ushort gindex = startGlyphIndex; gindex <= endGlyphIndex; ++gindex)
                {
                    //build glyph
                    builder.BuildFromGlyphIndex(gindex, sizeInPoint);

                    var glyphContourBuilder = new GlyphContourBuilder();
                    //glyphToContour.Read(builder.GetOutputPoints(), builder.GetOutputContours());
                    var genParams = new MsdfGenParams();
                    builder.ReadShapes(glyphContourBuilder);
                    //genParams.shapeScale = 1f / 64; //we scale later (as original C++ code use 1/64)
                    GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(glyphContourBuilder, genParams);
                    atlasBuilder.AddGlyph(gindex, glyphImg);

                    using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        int[] buffer = glyphImg.GetImageBuffer();

                        var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg.Width, glyphImg.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
                        bmp.UnlockBits(bmpdata);
                        bmp.Save("d:\\WImageTest\\a001_xn2_" + gindex + ".png");
                    }
                }

                GlyphImage glyphImg2 = atlasBuilder.BuildSingleImage();
                using (Bitmap bmp = new Bitmap(glyphImg2.Width, glyphImg2.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg2.Width, glyphImg2.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int[] intBuffer = glyphImg2.GetImageBuffer();

                    System.Runtime.InteropServices.Marshal.Copy(intBuffer, 0, bmpdata.Scan0, intBuffer.Length);
                    bmp.UnlockBits(bmpdata);
                    bmp.Save(outputFile);
                }
                atlasBuilder.SaveFontInfo("d:\\WImageTest\\a_info.bin");
                //
                //-----------
                //test read texture info back
                var atlasBuilder2 = new SimpleFontAtlasBuilder();
                var readbackFontAtlas = atlasBuilder2.LoadFontInfo("d:\\WImageTest\\a_info.bin");
            }
        }

        static void CreateSampleMsdfImg(GlyphContourBuilder tx, string outputFile)
        {
            //sample

            MsdfGenParams msdfGenParams = new MsdfGenParams();
            GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(tx, msdfGenParams);
            int w = glyphImg.Width;
            int h = glyphImg.Height;
            using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                int[] imgBuffer = glyphImg.GetImageBuffer();
                System.Runtime.InteropServices.Marshal.Copy(imgBuffer, 0, bmpdata.Scan0, imgBuffer.Length);
                bmp.UnlockBits(bmpdata);
                bmp.Save(outputFile);
            }
        }




        static void GetPoints(
           ExtMsdfgen.EdgeSegment edge_A,
           ExtMsdfgen.EdgeSegment edge_B,
           List<ExtMsdfgen.Vec2Info> points)
        {

            switch (edge_A.SegmentKind)
            {
                default: throw new NotSupportedException();
                case ExtMsdfgen.EdgeSegmentKind.LineSegment:
                    {
                        ExtMsdfgen.LinearSegment seg = (ExtMsdfgen.LinearSegment)edge_A;
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P1.x, y = seg.P1.y });
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.QuadraticSegment:
                    {
                        ExtMsdfgen.QuadraticSegment seg = (ExtMsdfgen.QuadraticSegment)edge_A;
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.C2, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P2.x, y = seg.P2.y });
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.CubicSegment:
                    {
                        ExtMsdfgen.CubicSegment seg = (ExtMsdfgen.CubicSegment)edge_A;
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P2.x, y = seg.P2.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_A) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P3.x, y = seg.P3.y });
                    }
                    break;
            }

            switch (edge_B.SegmentKind)
            {
                default: throw new NotSupportedException();
                case ExtMsdfgen.EdgeSegmentKind.LineSegment:
                    {
                        ExtMsdfgen.LinearSegment seg = (ExtMsdfgen.LinearSegment)edge_B;
                        points.Add(new ExtMsdfgen.Vec2Info(edge_B) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P1.x, y = seg.P1.y });
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.QuadraticSegment:
                    {
                        ExtMsdfgen.QuadraticSegment seg = (ExtMsdfgen.QuadraticSegment)edge_B;
                        points.Add(new ExtMsdfgen.Vec2Info(edge_B) { Kind = ExtMsdfgen.Vec2PointKind.C2, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_B) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P2.x, y = seg.P2.y });
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.CubicSegment:
                    {
                        ExtMsdfgen.CubicSegment seg = (ExtMsdfgen.CubicSegment)edge_B;
                        points.Add(new ExtMsdfgen.Vec2Info(edge_B) { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_B) { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P2.x, y = seg.P2.y });
                        points.Add(new ExtMsdfgen.Vec2Info(edge_B) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P3.x, y = seg.P3.y });
                    }
                    break;
            }
        }


        static void FlattenPoints(ExtMsdfgen.EdgeSegment segment, bool isLastSeg, List<ExtMsdfgen.Vec2Info> points)
        {
            switch (segment.SegmentKind)
            {
                default: throw new NotSupportedException();
                case ExtMsdfgen.EdgeSegmentKind.LineSegment:
                    {
                        ExtMsdfgen.LinearSegment seg = (ExtMsdfgen.LinearSegment)segment;
                        points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        //if (isLastSeg)
                        //{
                        //    points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P1.x, y = seg.P1.y });
                        //}

                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.QuadraticSegment:
                    {
                        ExtMsdfgen.QuadraticSegment seg = (ExtMsdfgen.QuadraticSegment)segment;
                        points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.C2, x = seg.P1.x, y = seg.P1.y });
                        //if (isLastSeg)
                        //{
                        //    points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P2.x, y = seg.P2.y });
                        //}
                    }
                    break;
                case ExtMsdfgen.EdgeSegmentKind.CubicSegment:
                    {
                        ExtMsdfgen.CubicSegment seg = (ExtMsdfgen.CubicSegment)segment;
                        points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.Touch1, x = seg.P0.x, y = seg.P0.y });
                        points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P1.x, y = seg.P1.y });
                        points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.C3, x = seg.P2.x, y = seg.P2.y });
                        //if (isLastSeg)
                        //{
                        //    points.Add(new ExtMsdfgen.Vec2Info(segment) { Kind = ExtMsdfgen.Vec2PointKind.Touch2, x = seg.P2.x, y = seg.P2.y });
                        //}
                    }
                    break;
            }

        }
        static void CreateCornerAndArmList(List<ExtMsdfgen.Vec2Info> points, List<ExtMsdfgen.ShapeCornerArms> cornerAndArms)
        {

            int j = points.Count;
            int beginAt = cornerAndArms.Count;
            for (int i = 1; i < j - 1; ++i)
            {
                ExtMsdfgen.Vec2Info p0 = points[i - 1];
                ExtMsdfgen.Vec2Info p1 = points[i];
                ExtMsdfgen.Vec2Info p2 = points[i + 1];
                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms(p0, p1, p2);

                cornerArm.dbugLeftIndex = beginAt + i - 1;
                cornerArm.dbugMiddleIndex = beginAt + i;
                cornerArm.dbugRightIndex = beginAt + i + 1;

                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }

            {

                ExtMsdfgen.Vec2Info p0 = points[j - 2];
                ExtMsdfgen.Vec2Info p1 = points[j - 1];
                ExtMsdfgen.Vec2Info p2 = points[0];
                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms(p0, p1, p2);

#if DEBUG
                cornerArm.dbugLeftIndex = beginAt + j - 2;
                cornerArm.dbugMiddleIndex = beginAt + j - 1;
                cornerArm.dbugRightIndex = beginAt + 0;
#endif
                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }

            {
                //
                //PixelFarm.Drawing.PointF p0 = points[j - 1];
                //PixelFarm.Drawing.PointF p1 = points[0];
                //PixelFarm.Drawing.PointF p2 = points[1];

                ExtMsdfgen.Vec2Info p0 = points[j - 1];
                ExtMsdfgen.Vec2Info p1 = points[0];
                ExtMsdfgen.Vec2Info p2 = points[1];

                ExtMsdfgen.ShapeCornerArms cornerArm = new ExtMsdfgen.ShapeCornerArms(p0, p1, p2);

#if DEBUG
                cornerArm.dbugLeftIndex = beginAt + j - 1;
                cornerArm.dbugMiddleIndex = beginAt + 0;
                cornerArm.dbugRightIndex = beginAt + 1;
#endif

                cornerArm.CornerNo = cornerAndArms.Count; //**
                cornerAndArms.Add(cornerArm);
            }

        }
        static void CreateCornerArms(ExtMsdfgen.Contour contour, List<ExtMsdfgen.ShapeCornerArms> output)
        {


            //create corner-arm relation for a given contour
            List<ExtMsdfgen.EdgeHolder> edges = contour.edges;
            int j = edges.Count;

            List<ExtMsdfgen.Vec2Info> flattenPoints = new List<ExtMsdfgen.Vec2Info>();
            for (int i = 0; i < j; ++i)
            {
                ExtMsdfgen.EdgeSegment edge_A = edges[i].edgeSegment;
                FlattenPoints(edge_A, i == j - 1, flattenPoints);
            }
            CreateCornerAndArmList(flattenPoints, output);
        }
        static ExtMsdfgen.Shape CreateShape(VertexStore vxs, out ExtMsdfgen.BmpEdgeLut bmpLut)
        {
            List<ExtMsdfgen.EdgeSegment> flattenEdges = new List<ExtMsdfgen.EdgeSegment>();
            ExtMsdfgen.Shape shape1 = new ExtMsdfgen.Shape();

            int i = 0;
            double x, y;
            VertexCmd cmd;
            ExtMsdfgen.Contour cnt = null;
            double latestMoveToX = 0;
            double latestMoveToY = 0;
            double latestX = 0;
            double latestY = 0;


            List<ExtMsdfgen.ShapeCornerArms> cornerAndArms = new List<ExtMsdfgen.ShapeCornerArms>();
            List<int> edgeOfNextContours = new List<int>();//
            List<int> cornerOfNextContours = new List<int>();//

            while ((cmd = vxs.GetVertex(i, out x, out y)) != VertexCmd.NoMore)
            {
                switch (cmd)
                {
                    case VertexCmd.Close:
                        {
                            //close current cnt

                            if ((latestMoveToX != latestX) ||
                                (latestMoveToY != latestY))
                            {
                                //add line to close the shape
                                if (cnt != null)
                                {
                                    flattenEdges.Add(cnt.AddLine(latestX, latestY, latestMoveToX, latestMoveToY));
                                }
                            }
                            if (cnt != null)
                            {
                                //***                                
                                CreateCornerArms(cnt, cornerAndArms);
                                edgeOfNextContours.Add(flattenEdges.Count);
                                cornerOfNextContours.Add(cornerAndArms.Count);
                                shape1.contours.Add(cnt);
                                //***
                                cnt = null;
                            }
                        }
                        break;
                    case VertexCmd.C3:
                        {

                            //C3 curve (Quadratic)                            
                            if (cnt == null)
                            {
                                cnt = new ExtMsdfgen.Contour();
                            }
                            VertexCmd cmd1 = vxs.GetVertex(i + 1, out double x1, out double y1);
                            i++;
                            if (cmd1 != VertexCmd.LineTo)
                            {
                                throw new NotSupportedException();
                            }

                            //in this version, 
                            //we convert Quadratic to Cubic (https://stackoverflow.com/questions/9485788/convert-quadratic-curve-to-cubic-curve)

                            //Control1X = StartX + ((2f/3) * (ControlX - StartX))
                            //Control2X = EndX + ((2f/3) * (ControlX - EndX))


                            //flattenEdges.Add(cnt.AddCubicSegment(
                            //    latestX, latestY,
                            //    ((2f / 3) * (x - latestX)) + latestX, ((2f / 3) * (y - latestY)) + latestY,
                            //    ((2f / 3) * (x - x1)) + x1, ((2f / 3) * (y - y1)) + y1,
                            //    x1, y1));

                            flattenEdges.Add(cnt.AddQuadraticSegment(latestX, latestY, x, y, x1, y1));

                            latestX = x1;
                            latestY = y1;

                        }
                        break;
                    case VertexCmd.C4:
                        {
                            //C4 curve (Cubic)
                            if (cnt == null)
                            {
                                cnt = new ExtMsdfgen.Contour();
                            }

                            VertexCmd cmd1 = vxs.GetVertex(i + 1, out double x2, out double y2);
                            VertexCmd cmd2 = vxs.GetVertex(i + 2, out double x3, out double y3);
                            i += 2;

                            if (cmd1 != VertexCmd.C4 || cmd2 != VertexCmd.LineTo)
                            {
                                throw new NotSupportedException();
                            }

                            flattenEdges.Add(cnt.AddCubicSegment(latestX, latestY, x, y, x2, y2, x3, y3));

                            latestX = x3;
                            latestY = y3;

                        }
                        break;
                    case VertexCmd.LineTo:
                        {
                            if (cnt == null)
                            {
                                cnt = new ExtMsdfgen.Contour();
                            }
                            ExtMsdfgen.LinearSegment lineseg = cnt.AddLine(latestX, latestY, x, y);
                            flattenEdges.Add(lineseg);

                            latestX = x;
                            latestY = y;
                        }
                        break;
                    case VertexCmd.MoveTo:
                        {
                            latestX = latestMoveToX = x;
                            latestY = latestMoveToY = y;
                            if (cnt != null)
                            {
                                shape1.contours.Add(cnt);
                                cnt = null;
                            }
                        }
                        break;
                }
                i++;
            }

            if (cnt != null)
            {
                shape1.contours.Add(cnt);
                CreateCornerArms(cnt, cornerAndArms);
                edgeOfNextContours.Add(flattenEdges.Count);
                cornerOfNextContours.Add(cornerAndArms.Count);
                cnt = null;
            }

            //from a given shape we create a corner-arm for each corner  
            bmpLut = new ExtMsdfgen.BmpEdgeLut(cornerAndArms, flattenEdges, edgeOfNextContours, cornerOfNextContours);

            return shape1;
        }

        private void cmdTestMsdfGen_Click(object sender, EventArgs e)
        {
            List<PixelFarm.Drawing.PointF> points = new List<PixelFarm.Drawing.PointF>();
            points.AddRange(new PixelFarm.Drawing.PointF[]{
                    new PixelFarm.Drawing.PointF(10, 20),
                    new PixelFarm.Drawing.PointF(50, 60),
                    new PixelFarm.Drawing.PointF(80, 20),
                    new PixelFarm.Drawing.PointF(50, 10),
                    //new PixelFarm.Drawing.PointF(10, 20)
            });
            //1. 
            ExtMsdfgen.Shape shape1 = null;
            RectD bounds = RectD.ZeroIntersection;
            using (VxsTemp.Borrow(out var v1))
            using (VectorToolBox.Borrow(v1, out PathWriter w))
            {
                int count = points.Count;
                PixelFarm.Drawing.PointF pp = points[0];
                w.MoveTo(pp.X, pp.Y);
                for (int i = 1; i < count; ++i)
                {
                    pp = points[i];
                    w.LineTo(pp.X, pp.Y);
                }
                w.CloseFigure();

                bounds = v1.GetBoundingRect();
                shape1 = CreateShape(v1, out var bmpLut);
            }

            //using (VxsTemp.Borrow(out var v1))
            //using (VectorToolBox.Borrow(v1, out PathWriter w))
            //{


            //    w.MoveTo(15, 20);
            //    w.LineTo(50, 60);
            //    w.LineTo(60, 20);
            //    w.LineTo(50, 10);
            //    w.CloseFigure();

            //    bounds = v1.GetBoundingRect();
            //    shape1 = CreateShape(v1);
            //}

            //2.
            ExtMsdfgen.MsdfGenParams msdfGenParams = new ExtMsdfgen.MsdfGenParams();
            ExtMsdfgen.GlyphImage glyphImg = ExtMsdfgen.MsdfGlyphGen.CreateMsdfImage(shape1, msdfGenParams);
            using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                int[] buffer = glyphImg.GetImageBuffer();

                var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg.Width, glyphImg.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
                bmp.UnlockBits(bmpdata);
                bmp.Save("d:\\WImageTest\\msdf_shape.png");
                //
            }
        }

        void TranslateArms(List<ExtMsdfgen.ShapeCornerArms> cornerArms, double dx, double dy)
        {
            //test 2 if each edge has unique color
            int j = cornerArms.Count;
            for (int i = 0; i < j; ++i)
            {
                ExtMsdfgen.ShapeCornerArms arm = cornerArms[i];
                arm.Offset((float)dx, (float)dy);
            }
        }



        void DrawTessTriangles(Poly2Tri.Polygon polygon, AggPainter painter)
        {
            return;
            foreach (var triangle in polygon.Triangles)
            {
                Poly2Tri.TriangulationPoint p0 = triangle.P0;
                Poly2Tri.TriangulationPoint p1 = triangle.P1;
                Poly2Tri.TriangulationPoint p2 = triangle.P2;


                ////we do not store triangulation points (p0,p1,02)
                ////an EdgeLine is created after we create GlyphTriangles.

                ////triangulate point p0->p1->p2 is CCW ***             
                //e0 = NewEdgeLine(p0, p1, tri.EdgeIsConstrained(tri.FindEdgeIndex(p0, p1)));
                //e1 = NewEdgeLine(p1, p2, tri.EdgeIsConstrained(tri.FindEdgeIndex(p1, p2)));
                //e2 = NewEdgeLine(p2, p0, tri.EdgeIsConstrained(tri.FindEdgeIndex(p2, p0)));

                painter.RenderQuality = RenderQuality.HighQuality;
                painter.StrokeColor = PixelFarm.Drawing.Color.Green;
                painter.StrokeWidth = 1.5f;
                painter.DrawLine(p0.X, p0.Y, p1.X, p1.Y);
                painter.DrawLine(p1.X, p1.Y, p2.X, p2.Y);
                painter.DrawLine(p2.X, p2.Y, p0.X, p0.Y);
            }
        }



        /// <summary>
        /// create polygon from GlyphContour
        /// </summary>
        /// <param name="cnt"></param>
        /// <returns></returns>
        static Poly2Tri.Polygon CreatePolygon(List<PixelFarm.Drawing.PointF> flattenPoints, double dx, double dy)
        {
            List<Poly2Tri.TriangulationPoint> points = new List<Poly2Tri.TriangulationPoint>();

            //limitation: poly tri not accept duplicated points! *** 
            double prevX = 0;
            double prevY = 0;

            int j = flattenPoints.Count;
            //pass
            for (int i = 0; i < j; ++i)
            {
                PixelFarm.Drawing.PointF pp = flattenPoints[i];

                double x = pp.X + dx; //start from original X***
                double y = pp.Y + dy; //start from original Y***

                if (x == prevX && y == prevY)
                {
                    if (i > 0)
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    var triPoint = new Poly2Tri.TriangulationPoint(prevX = x, prevY = y) { userData = pp };
                    //#if DEBUG
                    //                    p.dbugTriangulationPoint = triPoint;
                    //#endif
                    points.Add(triPoint);

                }
            }

            return new Poly2Tri.Polygon(points.ToArray());

        }

        static Poly2Tri.Polygon CreateInvertedPolygon(List<PixelFarm.Drawing.PointF> flattenPoints, RectD bounds)
        {

            Poly2Tri.Polygon mainPolygon = new Poly2Tri.Polygon(new Poly2Tri.TriangulationPoint[]
            {
                new Poly2Tri.TriangulationPoint( bounds.Left,   bounds.Bottom),
                new Poly2Tri.TriangulationPoint( bounds.Right,  bounds.Bottom),
                new Poly2Tri.TriangulationPoint( bounds.Right,  bounds.Top),
                new Poly2Tri.TriangulationPoint( bounds.Left,   bounds.Top)
            });

            //find bounds

            List<Poly2Tri.TriangulationPoint> points = new List<Poly2Tri.TriangulationPoint>();

            //limitation: poly tri not accept duplicated points! *** 
            double prevX = 0;
            double prevY = 0;

            int j = flattenPoints.Count;
            //pass
            for (int i = 0; i < j; ++i)
            {
                PixelFarm.Drawing.PointF pp = flattenPoints[i];

                double x = pp.X; //start from original X***
                double y = pp.Y; //start from original Y***

                if (x == prevX && y == prevY)
                {
                    if (i > 0)
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    var triPoint = new Poly2Tri.TriangulationPoint(prevX = x, prevY = y) { userData = pp };
                    //#if DEBUG
                    //                    p.dbugTriangulationPoint = triPoint;
                    //#endif
                    points.Add(triPoint);

                }
            }

            Poly2Tri.Polygon p2 = new Poly2Tri.Polygon(points.ToArray());

            mainPolygon.AddHole(p2);
            return mainPolygon;
        }


        void GetExampleVxs(VertexStore outputVxs)
        {
            //counter-clockwise 
            //a triangle
            //outputVxs.AddMoveTo(10, 20);
            //outputVxs.AddLineTo(50, 60);
            //outputVxs.AddLineTo(70, 20);
            //outputVxs.AddCloseFigure();

            //a quad
            //outputVxs.AddMoveTo(10, 20);
            //outputVxs.AddLineTo(50, 60);
            //outputVxs.AddLineTo(70, 20);
            //outputVxs.AddLineTo(50, 10);
            //outputVxs.AddCloseFigure();



            //curve4
            //outputVxs.AddMoveTo(5, 5);
            //outputVxs.AddLineTo(50, 60);
            //outputVxs.AddCurve4To(70, 20, 50, 10, 10, 5);
            //outputVxs.AddCloseFigure();

            //curve3
            outputVxs.AddMoveTo(5, 5);
            outputVxs.AddLineTo(50, 60);
            outputVxs.AddCurve3To(70, 20, 10, 5);
            outputVxs.AddCloseFigure();


            //a quad with hole
            //outputVxs.AddMoveTo(10, 20);
            //outputVxs.AddLineTo(50, 60);
            //outputVxs.AddLineTo(70, 20);
            //outputVxs.AddLineTo(50, 10);
            //outputVxs.AddCloseFigure();

            //outputVxs.AddMoveTo(30, 30);
            //outputVxs.AddLineTo(40, 30);
            //outputVxs.AddLineTo(40, 35);
            //outputVxs.AddLineTo(30, 35);
            //outputVxs.AddCloseFigure();



        }
       

        class CustomBlendOp1 : BitmapBufferEx.CustomBlendOp
        {
            const int WHITE = (255 << 24) | (255 << 16) | (255 << 8) | 255;
            const int BLACK = (255 << 24);
            const int GREEN = (255 << 24) | (255 << 8);
            const int RED = (255 << 24) | (255 << 16);

            public override int Blend(int currentExistingColor, int inputColor)
            {


                //this is our custom blending 
                if (currentExistingColor != WHITE && currentExistingColor != BLACK)
                {
                    //return RED;
                    //WINDOWS: ABGR
                    int existing_R = currentExistingColor & 0xFF;
                    int existing_G = (currentExistingColor >> 8) & 0xFF;
                    int existing_B = (currentExistingColor >> 16) & 0xFF;

                    int new_R = inputColor & 0xFF;
                    int new_G = (inputColor >> 8) & 0xFF;
                    int new_B = (inputColor >> 16) & 0xFF;

                    if (new_R == existing_R && new_B == existing_B)
                    {
                        return inputColor;
                    }

                    //***
                    //Bitmap extension arrange this to ARGB?
                    return RED;
                    //return base.Blend(currentExistingColor, inputColor);
                }
                else
                {
                    return inputColor;
                }
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            //test fake msdf (this is not real msdf gen)
            //--------------------  
            using (VxsTemp.Borrow(out var v1))
            using (VectorToolBox.Borrow(v1, out PathWriter w))
            {
                //--------
                GetExampleVxs(v1);
                //--------

                ExtMsdfgen.Shape shape1 = CreateShape(v1, out ExtMsdfgen.BmpEdgeLut bmpLut7);
                ExtMsdfgen.MsdfGenParams msdfGenParams = new ExtMsdfgen.MsdfGenParams();
                ExtMsdfgen.MsdfGlyphGen.PreviewSizeAndLocation(
                   shape1,
                   msdfGenParams,
                   out int imgW, out int imgH,
                   out ExtMsdfgen.Vector2 translateVec);

                //---------
                List<ExtMsdfgen.ShapeCornerArms> cornerAndArms = bmpLut7.CornerArms;
                TranslateArms(cornerAndArms, translateVec.x, translateVec.y);
                //---------


                //Poly2Tri.Polygon polygon1 = CreatePolygon(points, translateVec.x, translateVec.y);
                //Poly2Tri.P2T.Triangulate(polygon1);
                //---------

                using (MemBitmap bmpLut = new MemBitmap(imgW, imgH))
                using (VxsTemp.Borrow(out var v5, out var v6))
                using (VectorToolBox.Borrow(out CurveFlattener flattener))
                using (AggPainterPool.Borrow(bmpLut, out AggPainter painter))
                {
                    painter.RenderQuality = RenderQuality.Fast;
                    painter.Clear(PixelFarm.Drawing.Color.Black);

                    v1.TranslateToNewVxs(translateVec.x, translateVec.y, v5);
                    flattener.MakeVxs(v5, v6);
                    painter.Fill(v6, PixelFarm.Drawing.Color.White);

                    painter.StrokeColor = PixelFarm.Drawing.Color.Red;
                    painter.StrokeWidth = 1;

                    CustomBlendOp1 customBlendOp1 = new CustomBlendOp1();

                    int cornerArmCount = cornerAndArms.Count;
                    List<int> cornerOfNextContours = bmpLut7.CornerOfNextContours;
                    int n = 1;
                    int startAt = 0;
                    for (int cc = 0; cc < cornerOfNextContours.Count; ++cc)
                    {
                        int nextStartAt = cornerOfNextContours[cc];
                        for (; n <= nextStartAt - 1; ++n)
                        {

                            ExtMsdfgen.ShapeCornerArms c0 = cornerAndArms[n - 1];
                            ExtMsdfgen.ShapeCornerArms c1 = cornerAndArms[n];

                            using (VxsTemp.Borrow(out var v2))
                            using (VectorToolBox.Borrow(v2, out PathWriter writer))
                            {
                                painter.CurrentBxtBlendOp = customBlendOp1; //**

                                //counter-clockwise
                                if (c0.MiddlePointKindIsTouchPoint)
                                {
                                    if (c0.RightPointKindIsTouchPoint)
                                    {
                                        //outer
                                        writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                                        writer.LineTo(c0.ExtPoint_LeftOuter.X, c0.ExtPoint_LeftOuter.Y);
                                        writer.LineTo(c0.ExtPoint_LeftOuterDest.X, c0.ExtPoint_LeftOuterDest.Y);
                                        writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                        writer.CloseFigure();
                                        // 
                                        painter.Fill(v2, c0.OuterColor);

                                        //inner
                                        v2.Clear();
                                        writer.MoveTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                        writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                                        writer.LineTo(c1.ExtPoint_RightInner.X, c1.ExtPoint_RightInner.Y);
                                        writer.LineTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                        writer.CloseFigure();
                                        ////
                                        painter.Fill(v2, c0.InnerColor);

                                        //gap
                                        v2.Clear();
                                        //large corner that cover gap
                                        writer.MoveTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                        writer.LineTo(c0.ExtPoint_RightOuter.X, c0.ExtPoint_RightOuter.Y);
                                        writer.LineTo(c0.ExtPoint_LeftOuter.X, c0.ExtPoint_LeftOuter.Y);
                                        writer.LineTo(c0.ExtPoint_RightInner.X, c0.ExtPoint_RightInner.Y);
                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                        writer.CloseFigure();
                                        painter.Fill(v2, PixelFarm.Drawing.Color.Red);

                                    }
                                    else
                                    {
                                        painter.CurrentBxtBlendOp = null;//**
                                                                         //right may be Curve2 or Curve3
                                        ExtMsdfgen.EdgeSegment ownerSeg = c1.CenterSegment;
                                        switch (ownerSeg.SegmentKind)
                                        {
                                            default: throw new NotSupportedException();
                                            case ExtMsdfgen.EdgeSegmentKind.CubicSegment:
                                                {
                                                    //approximate 
                                                    ExtMsdfgen.CubicSegment cs = (ExtMsdfgen.CubicSegment)ownerSeg;

                                                    double dx = translateVec.x;
                                                    double dy = translateVec.y;

                                                    using (VxsTemp.Borrow(out var v3, out var v4, out var v7))
                                                    using (VectorToolBox.Borrow(out Stroke s))
                                                    {
                                                        double rad0 = Math.Atan2(cs.P0.y - cs.P1.y, cs.P0.x - cs.P1.x);
                                                        v3.AddMoveTo(cs.P0.x + dx + Math.Cos(rad0) * 4, cs.P0.y + dy + Math.Sin(rad0) * 4);
                                                        v3.AddLineTo(cs.P0.x + dx, cs.P0.y + dy);
                                                        v3.AddCurve4To(cs.P1.x + dx, cs.P1.y + dy,
                                                                cs.P2.x + dx, cs.P2.y + dy,
                                                                cs.P3.x + dx, cs.P3.y + dy);

                                                        double rad1 = Math.Atan2(cs.P3.y - cs.P2.y, cs.P3.x - cs.P2.x);
                                                        v3.AddLineTo((cs.P3.x + dx) + Math.Cos(rad1) * 4, (cs.P3.y + dy) + Math.Sin(rad1) * 4);
                                                        v3.AddNoMore();//
                                                        //
                                                        flattener.MakeVxs(v3, v4);
                                                        s.Width = 4;
                                                        s.MakeVxs(v4, v7);

                                                        painter.RenderQuality = RenderQuality.HighQuality;
                                                        painter.Fill(v7, c0.OuterColor);
                                                        painter.RenderQuality = RenderQuality.Fast;


                                                        writer.MoveTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                                        writer.LineTo(c0.ExtPoint_RightOuter.X, c0.ExtPoint_RightOuter.Y);
                                                        v7.GetVertex(0, out double v7x, out double v7y);
                                                        //writer.LineTo(v7x - 2, v7y - 2);
                                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                                        writer.CloseFigure();
                                                        painter.Fill(v2, PixelFarm.Drawing.Color.Red);
                                                    }
                                                }
                                                break;
                                            case ExtMsdfgen.EdgeSegmentKind.QuadraticSegment:
                                                {
                                                    ExtMsdfgen.QuadraticSegment qs = (ExtMsdfgen.QuadraticSegment)ownerSeg;
                                                    double dx = translateVec.x;
                                                    double dy = translateVec.y;

                                                    using (VxsTemp.Borrow(out var v3, out var v4, out var v7))
                                                    using (VectorToolBox.Borrow(out Stroke s))
                                                    {
                                                        double rad0 = Math.Atan2(qs.P0.y - qs.P1.y, qs.P0.x - qs.P1.x);
                                                        v3.AddMoveTo(qs.P0.x + dx + Math.Cos(rad0) * 4, qs.P0.y + dy + Math.Sin(rad0) * 4);
                                                        v3.AddLineTo(qs.P0.x + dx, qs.P0.y + dy);
                                                        v3.AddCurve3To(qs.P1.x + dx, qs.P1.y + dy,
                                                                qs.P2.x + dx, qs.P2.y + dy);

                                                        double rad1 = Math.Atan2(qs.P2.y - qs.P1.y, qs.P2.x - qs.P1.x);
                                                        v3.AddLineTo((qs.P2.x + dx) + Math.Cos(rad1) * 4, (qs.P2.y + dy) + Math.Sin(rad1) * 4);
                                                        v3.AddNoMore();//
                                                        //
                                                        flattener.MakeVxs(v3, v4);
                                                        s.Width = 4;
                                                        s.MakeVxs(v4, v7);


                                                        painter.RenderQuality = RenderQuality.HighQuality;
                                                        painter.Fill(v7, c0.OuterColor);
                                                        painter.RenderQuality = RenderQuality.Fast;


                                                        writer.MoveTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                                        writer.LineTo(c0.ExtPoint_RightOuter.X, c0.ExtPoint_RightOuter.Y);
                                                        v7.GetVertex(0, out double v7x, out double v7y);
                                                        //writer.LineTo(v7x - 2, v7y - 2);
                                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                                        writer.CloseFigure();
                                                        painter.Fill(v2, PixelFarm.Drawing.Color.Red);
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        {
                            //the last one
                            ExtMsdfgen.ShapeCornerArms c0 = cornerAndArms[nextStartAt - 1];
                            ExtMsdfgen.ShapeCornerArms c1 = cornerAndArms[startAt];

                            using (VxsTemp.Borrow(out var v2))
                            using (VectorToolBox.Borrow(v2, out PathWriter writer))
                            {
                                painter.CurrentBxtBlendOp = customBlendOp1; //**
                                                                            //counter-clockwise

                                //counter-clockwise
                                if (c0.MiddlePointKindIsTouchPoint)
                                {
                                    if (c0.RightPointKindIsTouchPoint)
                                    {
                                        //outer
                                        writer.MoveTo(c0.middlePoint.X, c0.middlePoint.Y);
                                        writer.LineTo(c0.ExtPoint_LeftOuter.X, c0.ExtPoint_LeftOuter.Y);
                                        writer.LineTo(c0.ExtPoint_LeftOuterDest.X, c0.ExtPoint_LeftOuterDest.Y);
                                        writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                        writer.CloseFigure();
                                        // 
                                        painter.Fill(v2, c0.OuterColor);

                                        //inner
                                        v2.Clear();
                                        writer.MoveTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                        writer.LineTo(c1.middlePoint.X, c1.middlePoint.Y);
                                        writer.LineTo(c1.ExtPoint_RightInner.X, c1.ExtPoint_RightInner.Y);
                                        writer.LineTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                        writer.CloseFigure();
                                        ////
                                        painter.Fill(v2, c0.InnerColor);

                                        //gap
                                        v2.Clear();
                                        painter.CurrentBxtBlendOp = null;//**

                                        //large corner that cover gap
                                        writer.MoveTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                        writer.LineTo(c0.ExtPoint_RightOuter.X, c0.ExtPoint_RightOuter.Y);
                                        writer.LineTo(c0.ExtPoint_LeftOuter.X, c0.ExtPoint_LeftOuter.Y);
                                        writer.LineTo(c0.ExtPoint_RightInner.X, c0.ExtPoint_RightInner.Y);
                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                        writer.CloseFigure();
                                        painter.Fill(v2, PixelFarm.Drawing.Color.Red);

                                    }
                                    else
                                    {
                                        painter.CurrentBxtBlendOp = null;//**
                                                                         //right may be Curve2 or Curve3
                                        ExtMsdfgen.EdgeSegment ownerSeg = c1.CenterSegment;
                                        switch (ownerSeg.SegmentKind)
                                        {
                                            default: throw new NotSupportedException();
                                            case ExtMsdfgen.EdgeSegmentKind.CubicSegment:
                                                {
                                                    //approximate 
                                                    ExtMsdfgen.CubicSegment cs = (ExtMsdfgen.CubicSegment)ownerSeg;

                                                    double dx = translateVec.x;
                                                    double dy = translateVec.y;

                                                    using (VxsTemp.Borrow(out var v3, out var v4, out var v7))
                                                    using (VectorToolBox.Borrow(out Stroke s))
                                                    {
                                                        v3.AddMoveTo(cs.P0.x + dx, cs.P0.y + dy);
                                                        v3.AddCurve4To(cs.P1.x + dx, cs.P1.y + dy,
                                                                cs.P2.x + dx, cs.P2.y + dy,
                                                                cs.P3.x + dx, cs.P3.y + dy);

                                                        v3.AddNoMore();//

                                                        flattener.MakeVxs(v3, v4);
                                                        s.Width = 4;
                                                        s.MakeVxs(v4, v7);

                                                        painter.RenderQuality = RenderQuality.HighQuality;
                                                        painter.Fill(v7, PixelFarm.Drawing.Color.Red);
                                                        painter.RenderQuality = RenderQuality.Fast;


                                                        writer.MoveTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                                        writer.LineTo(c0.ExtPoint_RightOuter.X, c0.ExtPoint_RightOuter.Y);
                                                        v7.GetVertex(0, out double v7x, out double v7y);
                                                        writer.LineTo(v7x - 2, v7y - 2);
                                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                                        writer.CloseFigure();
                                                        painter.Fill(v2, PixelFarm.Drawing.Color.Red);
                                                    }
                                                }
                                                break;
                                            case ExtMsdfgen.EdgeSegmentKind.QuadraticSegment:
                                                {
                                                    ExtMsdfgen.QuadraticSegment qs = (ExtMsdfgen.QuadraticSegment)ownerSeg;
                                                    double dx = translateVec.x;
                                                    double dy = translateVec.y;

                                                    using (VxsTemp.Borrow(out var v3, out var v4, out var v7))
                                                    using (VectorToolBox.Borrow(out Stroke s))
                                                    {
                                                        double rad0 = Math.Atan2(qs.P0.y - qs.P1.y, qs.P0.x - qs.P1.x);
                                                        v3.AddMoveTo(qs.P0.x + dx + Math.Cos(rad0) * 4, qs.P0.y + dy + Math.Sin(rad0) * 4);
                                                        v3.AddLineTo(qs.P0.x + dx, qs.P0.y + dy);
                                                        v3.AddCurve3To(qs.P1.x + dx, qs.P1.y + dy,
                                                                qs.P2.x + dx, qs.P2.y + dy);

                                                        double rad1 = Math.Atan2(qs.P2.y - qs.P1.y, qs.P2.x - qs.P1.x);
                                                        v3.AddLineTo((qs.P2.x + dx) + Math.Cos(rad1) * 4, (qs.P2.y + dy) + Math.Sin(rad1) * 4);
                                                        v3.AddNoMore();//
                                                        //
                                                        flattener.MakeVxs(v3, v4);
                                                        s.Width = 4;
                                                        s.MakeVxs(v4, v7);

                                                        painter.RenderQuality = RenderQuality.HighQuality;
                                                        painter.Fill(v7, c0.OuterColor);
                                                        painter.RenderQuality = RenderQuality.Fast;


                                                        writer.MoveTo(c0.ExtPoint_LeftInner.X, c0.ExtPoint_LeftInner.Y);
                                                        writer.LineTo(c0.ExtPoint_RightOuter.X, c0.ExtPoint_RightOuter.Y);
                                                        v7.GetVertex(0, out double v7x, out double v7y);
                                                        //writer.LineTo(v7x - 2, v7y - 2);
                                                        writer.LineTo(c0.middlePoint.X, c0.middlePoint.Y);
                                                        writer.CloseFigure();
                                                        painter.Fill(v2, PixelFarm.Drawing.Color.Red);
                                                    }
                                                }
                                                break;
                                        }

                                    }
                                }
                            }
                        }

                        startAt = nextStartAt;
                        n++;
                    }
                    //DrawTessTriangles(polygon1, painter); 

                    bmpLut.SaveImage("d:\\WImageTest\\msdf_shape_lut2.png");
                    //
                    int[] lutBuffer = bmpLut.CopyImgBuffer(bmpLut.Width, bmpLut.Height);
                    //ExtMsdfgen.BmpEdgeLut bmpLut2 = new ExtMsdfgen.BmpEdgeLut(bmpLut.Width, bmpLut.Height, lutBuffer);


                    //bmpLut2 = null;
                    var bmp5 = MemBitmap.LoadBitmap("d:\\WImageTest\\msdf_shape_lut.png");

                    int[] lutBuffer5 = bmp5.CopyImgBuffer(bmpLut.Width, bmpLut.Height);
                    bmpLut7.SetBmpBuffer(bmpLut.Width, bmpLut.Height, lutBuffer5);

                    ExtMsdfgen.GlyphImage glyphImg = ExtMsdfgen.MsdfGlyphGen.CreateMsdfImage(shape1, msdfGenParams, bmpLut7);
                    //                     
                    using (Bitmap bmp3 = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        int[] buffer = glyphImg.GetImageBuffer();

                        var bmpdata = bmp3.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg.Width, glyphImg.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp3.PixelFormat);
                        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
                        bmp3.UnlockBits(bmpdata);
                        bmp3.Save("d:\\WImageTest\\msdf_shape.png");
                        //
                    }
                }
            }
        }
    }
}