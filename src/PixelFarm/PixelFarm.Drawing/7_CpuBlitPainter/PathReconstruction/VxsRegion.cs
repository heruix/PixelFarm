﻿//MIT, 2019-present, WinterDev

using System;
using System.Collections.Generic;
using ClipperLib;
using PixelFarm.Drawing;
using PixelFarm.CpuBlit;
using PixelFarm.CpuBlit.VertexProcessing;

namespace PixelFarm.PathReconstruction
{

    public class VxsRegion : CpuBlitRegion
    {
        VertexStore _vxs;//vector path for the data
        bool _isSimpleRect;
        bool _evalRectBounds;
        Rectangle _bounds;

        /// <summary>
        /// create simple Rect region
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public VxsRegion(float left, float top, float width, float height)
        {
            _isSimpleRect = true;
            using (VxsTemp.Borrow(out VertexStore v1))
            using (VectorToolBox.Borrow(out SimpleRect rect))
            {
                rect.SetRectFromLTWH(left, top, width, height);
                rect.MakeVxs(v1);
                _vxs = v1.CreateTrim();
            }
            _bounds = new Rectangle(
               (int)Math.Round(left),
               (int)Math.Round(top),
               (int)Math.Round(width),
               (int)Math.Round(height));
        }
        /// <summary>
        /// create a region from vxs (may be simple rect vxs or complex vxs)
        /// </summary>
        /// <param name="vxs"></param>
        public VxsRegion(VertexStore vxs)
        {
            //COPY
            _vxs = vxs.CreateTrim();//we don't store outside data 

        }
        public override CpuBlitRegionKind Kind => CpuBlitRegionKind.VxsRegion;

        public override void Dispose()
        {
            if (_vxs != null)
            {
                _vxs = null;
            }
        }

        public VertexStore GetVxs() => _vxs;

        public override Rectangle GetRectBounds()
        {
            if (!_evalRectBounds)
            {

            }
            return _bounds;
        }
        public override Region CreateComplement(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }

        public override Region CreateExclude(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }
        public override Region CreateIntersect(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }

        public override Region CreateUnion(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }

    }


}