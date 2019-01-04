﻿//BSD, 2014-present, WinterDev
//MatterHackers 

using System;
using System.Collections.Generic;

using PixelFarm.Drawing;
using PixelFarm.CpuBlit.Imaging;
using Mini;

namespace PixelFarm.CpuBlit.Sample_FloodFill
{
    [Info(OrderCode = "09")]
    [Info(DemoCategory.Bitmap, "Demonstration of a flood filling algorithm.")]
    public class FloodFillDemo : DemoBase
    {

        public enum ImageOption
        {
            Default,
            Lion,
            Stars,
        }
        ImageOption _imgOption;

        MemBitmap _bmpToFillOn;

        MemBitmap _lionPng;
        MemBitmap _defaultImg;
        MemBitmap _starsPng;

        int _imgOffsetX = 20;
        int _imgOffsetY = 60;
        int _tolerance = 0;

        FloodFill _floodFill;
        public FloodFillDemo()
        {
            //
            BackgroundColor = Color.White;

            _defaultImg = new MemBitmap(400, 300);

            AggPainter p = AggPainter.Create(_defaultImg);
            p.Clear(Color.White);
            p.FillColor = Color.Black;
            p.FillEllipse(20, 20, 30, 30);


            for (int i = 0; i < 20; i++)
            {
                p.StrokeColor = Color.Black;
                p.DrawEllipse(i * 10, i * 10, 20, 20);
            }

            //
            this.PixelSize = 32;
            this.Gamma = 1;

            _floodFill = new FloodFill(Color.Red, 30);

            //
            _lionPng = PixelFarm.Platforms.StorageService.Provider.ReadPngBitmap("../Data/lion1.png");
            _starsPng = PixelFarm.Platforms.StorageService.Provider.ReadPngBitmap("../Data/stars.png");


            _bmpToFillOn = _defaultImg;
        }
        [DemoConfig]
        public ImageOption SelectedImageOption
        {
            get => _imgOption;
            set
            {
                _imgOption = value;
                switch (value)
                {
                    default:
                        _bmpToFillOn = _defaultImg;
                        break;
                    case ImageOption.Stars:
                        _bmpToFillOn = _starsPng;
                        break;
                    case ImageOption.Lion:
                        _bmpToFillOn = _lionPng;
                        break;
                }
                this.InvalidateGraphics();
            }
        }
        [DemoConfig(MinValue = 8, MaxValue = 100)]
        public int PixelSize
        {
            get;
            set;
        }
        [DemoConfig(MaxValue = 3)]
        public double Gamma
        {
            get;
            set;
        }
        [DemoConfig(MinValue = 0, MaxValue = 255)]
        public int Tolerance
        {
            get => _tolerance;
            set
            {
                _tolerance = value;
                _floodFill.Update(_floodFill.FillColor, (byte)value);
                //
                InvalidateGraphics();
            }

        }
        public Color BackgroundColor
        {
            get;
            set;
        }

        public override void Draw(Painter p)
        {
            p.Clear(Color.Blue);

            p.DrawImage(_bmpToFillOn, _imgOffsetX, _imgOffsetY);

            p.FillColor = Color.Yellow;
            p.FillEllipse(20, 20, 30, 30);

            //p.StrokeColor = Color.Red;
            //p.DrawLine(0, 0, 100, 100);
        }



        public override void MouseDown(int mx, int my, bool isRightButton)
        {
            int x = mx - _imgOffsetX;
            int y = my - _imgOffsetY;

            //FloodFill _filler = new FloodFill(Color.Red, (byte)_tolerance);

            var spanCollectionOutput = new FloodFill.HSpanCollection();

            _floodFill.SetRangeCollectionOutput(spanCollectionOutput);
            _floodFill.Fill(_bmpToFillOn, x, y);
            _floodFill.SetRangeCollectionOutput(null);

            //try tracing for vxs
            using (VxsTemp.Borrow(out VertexStore v1))
            {
                spanCollectionOutput.ReconstructVxs(v1);

            }


            this.InvalidateGraphics();
        }
    }
}
