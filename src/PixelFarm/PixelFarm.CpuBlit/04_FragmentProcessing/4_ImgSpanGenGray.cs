//BSD, 2014-present, WinterDev
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


using System;
using img_subpix_const = PixelFarm.CpuBlit.Imaging.ImageFilterLookUpTable.ImgSubPixConst;
namespace PixelFarm.CpuBlit.FragmentProcessing
{
    // it should be easy to write a 90 rotating or mirroring filter too. LBB 2012/01/14
    public class ImgSpanGenGray_NNStepXby1 : ImgSpanGen
    {
        const int BASE_SHIFT = 8;
        const int BASE_SCALE = (int)(1 << BASE_SHIFT);
        const int BASE_MASK = BASE_SCALE - 1;
        IBitmapSrc srcRW;
        public ImgSpanGenGray_NNStepXby1(IBitmapSrc src, ISpanInterpolator spanInterpolator)
            : base(spanInterpolator)
        {
            srcRW = src;
            if (srcRW.BitDepth != 8)
            {
                throw new NotSupportedException("The source is expected to be 8 bit.");
            }
        }
        public sealed override void GenerateColors(Drawing.Color[] outputColors, int startIndex, int x, int y, int len)
        {
            int bytesBetweenPixelsInclusive = srcRW.BytesBetweenPixelsInclusive;
            ISpanInterpolator spanInterpolator = Interpolator;
            spanInterpolator.Begin(x + dx, y + dy, len);
            int x_hr;
            int y_hr;
            spanInterpolator.GetCoord(out x_hr, out y_hr);
            int x_lr = x_hr >> img_subpix_const.SHIFT;
            int y_lr = y_hr >> img_subpix_const.SHIFT;
            int bufferIndex;
            bufferIndex = srcRW.GetByteBufferOffsetXY(x_lr, y_lr);

            unsafe
            {
                CpuBlit.Imaging.TempMemPtr srcBuffPtr = srcRW.GetBufferPtr();
                byte* pSource = (byte*)srcBuffPtr.Ptr;
                {
                    do
                    {
                        //outputColors[startIndex].red = pSource[bufferIndex];
                        //outputColors[startIndex].green = pSource[bufferIndex];
                        //outputColors[startIndex].blue = pSource[bufferIndex];
                        //outputColors[startIndex].alpha = 255;

                        byte grayValue = pSource[bufferIndex];
                        outputColors[startIndex] = Drawing.Color.FromArgb(255,
                           grayValue,
                           grayValue,
                           grayValue);

                        startIndex++;
                        bufferIndex += bytesBetweenPixelsInclusive;
                    } while (--len != 0);
                }
                srcBuffPtr.Release();
            }
        }
    }
}