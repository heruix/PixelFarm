//MIT, 2014-present, WinterDev
//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
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

using PixelFarm.Drawing;
using PixelFarm.Agg.Gradients;
namespace PixelFarm.Agg
{
    //==========================================================span_gradient
    public class GradientSpanGen : ISpanGenerator
    {
        const int GR_SUBPIX_SHIFT = 4;                              //-----gradient_subpixel_shift
        internal const int GR_SUBPIX_SCALE = 1 << GR_SUBPIX_SHIFT;   //-----gradient_subpixel_scale
        const int GR_SUBPIX_MASK = GR_SUBPIX_SCALE - 1;    //-----gradient_subpixel_mask
        const int SUBPIX_SHIFT = 8;
        const int DOWN_SCALE_SHIFT = SUBPIX_SHIFT - GR_SUBPIX_SHIFT;
        ISpanInterpolator _interpolator;
        IGradientValueCalculator _grValueCalculator;
        IGradientColorsProvider _colorsProvider;
        int _d1;
        int _d2;

        float _stepRatio;

        int _xoffset;
        int _yoffset;
        //--------------------------------------------------------------------
        public GradientSpanGen() { }

        public void SetOffset(int x, int y)
        {
            _xoffset = x;
            _yoffset = y;
        }
        public void Reset(ISpanInterpolator inter,
                  IGradientValueCalculator gvc,
                  IGradientColorsProvider m_colorsProvider,
                  double d1, double d2)
        {
            _xoffset = _yoffset = 0;//reset

            this._interpolator = inter;
            this._grValueCalculator = gvc;
            this._colorsProvider = m_colorsProvider;
            _d1 = AggMath.iround(d1 * GR_SUBPIX_SCALE);
            _d2 = AggMath.iround(d2 * GR_SUBPIX_SCALE);
            int dd = _d2 - _d1;
            if (dd < 1)
            {
                dd = 1;
            }
            _stepRatio = (float)m_colorsProvider.GradientSteps / (float)dd;
            _xoffset = _yoffset = 0;//reset
        }

        //--------------------------------------------------------------------
        public void Prepare() { }
        //--------------------------------------------------------------------
        public void GenerateColors(Color[] outputColors, int startIndex, int x, int y, int spanLen)
        {
            //set interpolation start point
            //spanLen => horizontal span len

            _interpolator.Begin(_xoffset + x + 0.5, _yoffset + y + 0.5, spanLen);

            int gradientSteps = _colorsProvider.GradientSteps;

            do
            {
                //find actual x and y 
                _interpolator.GetCoord(out x, out y);

                float d = _grValueCalculator.Calculate(x >> DOWN_SCALE_SHIFT,
                                                      y >> DOWN_SCALE_SHIFT,
                                                      _d2);
                d = ((d - _d1) * _stepRatio);
                if (d < 0)
                {
                    d = 0;
                }
                else if (d >= gradientSteps)
                {
                    d = gradientSteps - 1;
                }

                outputColors[startIndex++] = _colorsProvider.GetColor((int)d);
                _interpolator.Next();//**
            }
            while (--spanLen != 0);
        }
    }

    //=====================================================gradient_linear_color
    public class LinearGradientColorsProvider : Gradients.IGradientColorsProvider
    {
        Color _c1;
        Color _c2;
        int _gradientSteps;

        public LinearGradientColorsProvider() { }

        public int GradientSteps { get { return _gradientSteps; } }
        public Color GetColor(int v)
        {
            //get gradient color between c1 and c2 and specific step
            return _c1.CreateGradient(_c2, (float)(v) / (float)(_gradientSteps - 1));
        }

        public void SetColors(Color c1, Color c2, int gradientSteps = 256)
        {
            _c1 = c1;
            _c2 = c2;
            //-------
            if (gradientSteps < 2)
            {
                gradientSteps = 2;
            }
            //-------
            _gradientSteps = gradientSteps;
        }
    }
}