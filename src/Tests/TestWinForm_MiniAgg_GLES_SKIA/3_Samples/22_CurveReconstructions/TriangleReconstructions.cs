﻿//MIT, 2018-present, WinterDev
//from http://www.antigrain.com/research/bezier_interpolation/index.html#PAGE_BEZIER_INTERPOLATION

using System;
using System.Collections.Generic;
using PaintLab.Svg;
using PixelFarm.Drawing;
using PixelFarm.VectorMath;
using PixelFarm.CpuBlit.VertexProcessing;
using Mini;


namespace PixelFarm.CpuBlit.Samples
{

    class BezierControllerArmPair
    {
        public Vector2 left;
        public Vector2 mid;
        public Vector2 right;

        Vector2 _left_bk; //backup
        Vector2 _mid_bk; //backup
        Vector2 _right_bk; //backup


        float _smooth_coeff;//0-1 coefficiency value
        public BezierControllerArmPair(Vector2 left, Vector2 mid, Vector2 right)
        {
            _smooth_coeff = 1;
            this.left = _left_bk = left;
            this.mid = _mid_bk = mid;
            this.right = _right_bk = right;
        }

        public float UniformSmoothCoefficient
        {
            get => _smooth_coeff;
            set
            {
                _smooth_coeff = value;
                if (_smooth_coeff == 1)
                {
                    left = _left_bk;
                    mid = _mid_bk;
                    right = _right_bk;
                }
                else
                {
                    Vector2 newToRight = (_right_bk - _mid_bk) * _smooth_coeff;
                    Vector2 newToLeft = (_left_bk - _mid_bk) * _smooth_coeff;

                    left = _mid_bk + newToLeft;
                    right = _mid_bk + newToRight;
                }
            }
        }
        public void Offset(double dx, double dy)
        {
            Vector2 diff = new Vector2(dx, dy);
            left += diff;
            mid += diff;
            right += diff;

            _left_bk += diff;
            _mid_bk += diff;
            _right_bk += diff;
        }

        static double Len(Vector2 v0, Vector2 v1)
        {
            return System.Math.Sqrt(
                  ((v1.Y - v0.Y) * (v1.Y - v0.Y)) +
                  ((v1.X - v0.X) * (v1.X - v0.x)));
        }

        public static BezierControllerArmPair ReconstructControllerArms(Vector2 left, Vector2 middle, Vector2 right)
        {
            Vector2 a_left = (left + middle) / 2;
            Vector2 a_right = (right + middle) / 2;


            double len_1 = Len(left, middle);
            double len_2 = Len(right, middle);
            //
            //double a_left_right_len = Len(a_left, a_right);

            if ((len_1 + len_2) == 0)
            {
                return null;
            }

            double d1_ratio = (len_1 / (len_1 + len_2));

            Vector2 b = new Vector2(
                a_left.x + (d1_ratio * (a_right.x - a_left.x)),
                a_left.y + (d1_ratio * (a_right.y - a_left.y)));

            var controllerPair = new BezierControllerArmPair(a_left, b, a_right);

            Vector2 diff = b - middle;
            controllerPair.Offset(-diff.x, -diff.y);

            return controllerPair;
        }


    }

    class ReconstructedFigure
    {
        internal List<BezierControllerArmPair> _arms = new List<BezierControllerArmPair>();
        public int Count => _arms.Count;
    }

    class BezireControllerArmBuilder
    {
        LimitedQueue<Vector2> _reusableQueue = new LimitedQueue<Vector2>(3);

        class LimitedQueue<T>
        {
            T[] _que;

            int _readIndex;
            int _writeIndex;
            int _count;

            public LimitedQueue(int limitCapacity)
            {
                _que = new T[limitCapacity];
            }
            public int Count => _count;
            public void Enqueue(T input)
            {
                if (_count == _que.Length)
                {
                    throw new Exception("queue is fulled");
                }

                //
                _que[_writeIndex] = input;
                _writeIndex++;
                _count++;

                if (_writeIndex == _que.Length)
                {
                    //we are at the end of the arr
                    //turn back to 0
                    _writeIndex = 0;
                }
            }
            public T Dequeue()
            {
                if (_count == 0)
                {
                    //no obj in queue
                    throw new Exception("no obj in queue");
                }

                //-----------
                T obj = _que[_readIndex];
                _count--;
                _readIndex++;

                if (_readIndex == _que.Length)
                {
                    //we are on the end of the array
                    //turn to 0
                    _readIndex = 0;
                }
                return obj;
            }
            public T NextQueue(int offset)
            {
                if (offset > _count - 1)
                {
                    throw new Exception("no obj in queue");
                }
                if (_readIndex + offset >= _que.Length)
                {
                    return _que[(_readIndex + offset) - _que.Length];
                }
                else
                {
                    return _que[_readIndex + offset];
                }
            }
            public void Clear()
            {
                _writeIndex = _readIndex = _count = 0;
                //clear arr?

            }
        }

        public void ReconstructionControllerArms(VertexStore inputVxs, List<ReconstructedFigure> figures)
        {
            _reusableQueue.Clear();
            int count = inputVxs.Count;
            if (count < 2)
            {
                return;
            }
            if (count == 2)
            {
                //simulate right

            }

            //
            ReconstructedFigure currentFig = new ReconstructedFigure();

            //
            int limit = count - 1;
            double lastest_moveToX = 0, latest_moveToY = 0;

            for (int i = 0; i < limit; ++i)
            {

                if (_reusableQueue.Count == 3)
                {
                    Vector2 v0 = _reusableQueue.Dequeue();
                    Vector2 v1 = _reusableQueue.NextQueue(0);
                    Vector2 v2 = _reusableQueue.NextQueue(1);
                    BezierControllerArmPair arm = BezierControllerArmPair.ReconstructControllerArms(v0, v1, v2);
                    if (arm != null)
                    {
                        currentFig._arms.Add(arm);
                    }
                }

                switch (inputVxs.GetVertex(i, out double x, out double y))
                {
                    case VertexCmd.MoveTo:
                        _reusableQueue.Enqueue(new Vector2(lastest_moveToX = x, latest_moveToY = y));
                        break;
                    case VertexCmd.P2c:
                    case VertexCmd.P3c:
                    case VertexCmd.LineTo:
                        _reusableQueue.Enqueue(new Vector2(x, y));
                        break;
                    case VertexCmd.Close:
                        _reusableQueue.Enqueue(new Vector2(lastest_moveToX, latest_moveToY));
                        //
                        if (currentFig.Count > 0)
                        {
                            figures.Add(currentFig);
                        }
                        currentFig = new ReconstructedFigure();
                        break;
                    case VertexCmd.NoMore:
                        goto EXIT;
                }
            }

            switch (_reusableQueue.Count)
            {
                default:
                    throw new NotSupportedException();//? 
                case 1:
                    {

                    }
                    break;
                case 2:
                    {

                    }
                    break;
                case 3:
                    {
                        Vector2 v0 = _reusableQueue.Dequeue();
                        Vector2 v1 = _reusableQueue.NextQueue(0);
                        Vector2 v2 = _reusableQueue.NextQueue(1);
                        BezierControllerArmPair arm = BezierControllerArmPair.ReconstructControllerArms(v0, v1, v2);
                        if (arm != null)
                        {
                            currentFig._arms.Add(arm);
                        }
                        if (currentFig.Count > 0)
                        {
                            figures.Add(currentFig);
                        }
                    }
                    break;
            }


            EXIT:
            return;
        }
    }

    [Info(OrderCode = "03")]
    public class TriangleCurveReconstruction1 : DemoBase
    {
        VertexStore _triangleVxs;
        CurveFlattener _curveflattener = new CurveFlattener();

        Vector2 v0;
        Vector2 v1;
        Vector2 v2;

        Vector2 a01;
        Vector2 a12;
        Vector2 a20;

        public override void Init()
        {
            ShowReconstructionCurve = true;
            SmoothCoefficientValue = 1;
            using (VxsTemp.Borrow(out var vxs))
            {
                int xoffset = 50;
                int yoffset = 50;

                v0 = new Vector2(0 + xoffset, 0 + yoffset);
                v1 = new Vector2(300 + xoffset, 100 + yoffset);
                v2 = new Vector2(100 + xoffset, 120 + yoffset);

                vxs.AddMoveTo(v0.x, v0.y);
                vxs.AddLineTo(v1.x, v1.y);
                vxs.AddLineTo(v2.x, v2.y);
                vxs.AddCloseFigure();
                _triangleVxs = vxs.CreateTrim();
                //
                //mid point of each edge

                a01 = (v0 + v1) / 2;
                a12 = (v1 + v2) / 2;
                a20 = (v2 + v0) / 2;
                //----- 
            }
        }
        void DrawPoint(PixelFarm.Drawing.Painter p, Vector2 v)
        {
            Color prev = p.FillColor;
            p.FillColor = Color.Red;

            p.FillRect(v.X, v.Y, 4, 4);

            p.FillColor = prev;
        }
        void DrawLine(PixelFarm.Drawing.Painter p, Vector2 v0, Vector2 v1)
        {
            p.Line(v0.x, v0.y, v1.x, v1.Y, p.StrokeColor);
        }
        void DrawControllerPair(PixelFarm.Drawing.Painter p, BezierControllerArmPair c)
        {
            DrawLine(p, c.left, c.mid);
            DrawLine(p, c.mid, c.right);
            DrawPoint(p, c.mid);
        }

        [DemoConfig(MinValue = -10, MaxValue = 20)] //just a sample!, -10=> 20
        public float SmoothCoefficientValue
        {
            get;
            set;
        }

        [DemoConfig]
        public bool ShowReconstructionCurve
        {
            get;
            set;
        }
        public override void Draw(PixelFarm.Drawing.Painter p)
        {
            p.Clear(Drawing.Color.White);
            p.StrokeColor = Color.Black;
            p.StrokeWidth = 2;
            p.StrokeColor = Color.Green;

            p.Draw(_triangleVxs);


            if (!ShowReconstructionCurve) return;

            //draw Ci line
            p.StrokeColor = Color.OrangeRed;
            DrawLine(p, a01, a12);
            DrawLine(p, a12, a20);
            DrawLine(p, a20, a01);
            //find B

            //DrawPoint(p, a01);
            //DrawPoint(p, a12);
            //DrawPoint(p, a20);

            BezierControllerArmPair c1 = BezierControllerArmPair.ReconstructControllerArms(v0, v1, v2);
            BezierControllerArmPair c2 = BezierControllerArmPair.ReconstructControllerArms(v1, v2, v0);
            BezierControllerArmPair c0 = BezierControllerArmPair.ReconstructControllerArms(v2, v0, v1);



            c0.UniformSmoothCoefficient = SmoothCoefficientValue;
            c1.UniformSmoothCoefficient = SmoothCoefficientValue;
            c2.UniformSmoothCoefficient = SmoothCoefficientValue;

            //DrawPoint(p, c0 = FindB(v0, v1, v2, out c1));
            //DrawPoint(p, b2 = FindB(v1, v2, v0, out c2));
            //DrawPoint(p, b0 = FindB(v2, v0, v1, out c0));

            p.StrokeColor = Color.Red;
            DrawControllerPair(p, c0);
            DrawControllerPair(p, c1);
            DrawControllerPair(p, c2);


            p.StrokeColor = Color.Blue;
            using (VxsTemp.Borrow(out var tmpVxs1, out var tmpVxs2))
            using (VectorToolBox.Borrow(tmpVxs1, out PathWriter pw))
            {
                pw.MoveTo(c0.mid.X, c0.mid.y);

                pw.Curve4(c0.right.x, c0.right.y, c1.left.x, c1.left.y, c1.mid.x, c1.mid.y); //1st curve

                pw.Curve4(c1.right.x, c1.right.y, c2.left.x, c2.left.y, c2.mid.x, c2.mid.y); //2nd curve

                pw.Curve4(c2.right.x, c2.right.y, c0.left.x, c0.left.y, c0.mid.x, c0.mid.y); //3rd curve

                _curveflattener.MakeVxs(tmpVxs1, tmpVxs2);
                p.Draw(tmpVxs2);
            }
        }

    }

}