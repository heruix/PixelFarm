﻿//MIT, 2016-present, WinterDev
//Apache2, https://xmlgraphics.apache.org/

using System;
using PixelFarm.Drawing;
using PixelFarm.CpuBlit;

namespace PixelFarm.DrawingGL
{
    partial class GLPainter
    {
        ClipingTechnique _currentClipTech;
        RectInt _clipBox;
        public bool EnableBuiltInMaskComposite { get; set; }
        public override RectInt ClipBox
        {
            get => _clipBox;
            set => _clipBox = value;
        }

        public override void SetClipRgn(VertexStore vxs)
        {

            //TODO: review mask combine mode
            //1. Append
            //2. Replace 

            //this version we use replace 
            //clip rgn implementation
            //this version replace only
            //TODO: add append clip rgn 

            //TODO: implement complex framebuffer-based mask

            if (vxs != null)
            {
                if (PixelFarm.Drawing.SimpleRectClipEvaluator.EvaluateRectClip(vxs, out RectangleF clipRect))
                {
                    this.SetClipBox(
                        (int)Math.Floor(clipRect.X), (int)Math.Floor(clipRect.Y),
                        (int)Math.Ceiling(clipRect.Right), (int)Math.Ceiling(clipRect.Bottom));

                    _currentClipTech = ClipingTechnique.ClipSimpleRect;
                }
                else
                {
                    //not simple rect => 
                    //use mask technique
                    _currentClipTech = ClipingTechnique.ClipMask;
                    //1. switch to mask buffer  
                    PathRenderVx pathRenderVx = _pathRenderVxBuilder.CreatePathRenderVx(vxs);
                    _pcx.EnableMask(pathRenderVx);
                }
            }
            else
            {
                //remove clip rgn if exists**
                switch (_currentClipTech)
                {
                    case ClipingTechnique.ClipMask:
                        _pcx.DisableMask();
                        break;
                    case ClipingTechnique.ClipSimpleRect:
                        this.SetClipBox(0, 0, this.Width, this.Height);
                        break;
                }
                _currentClipTech = ClipingTechnique.None;
            }
        }
        public override void SetClipBox(int left, int top, int right, int bottom)
        {
            _pcx.SetClipRect(left, top, right - left, bottom - top);
        }
    }
}