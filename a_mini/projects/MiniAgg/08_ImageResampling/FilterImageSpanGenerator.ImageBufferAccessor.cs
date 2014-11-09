//2014 BSD,WinterDev   
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

namespace PixelFarm.Agg.Image
{

    partial class FilterImageSpanGenerator
    {
        sealed class ImageBufferAccessor
        {
            IImageReaderWriter imgRW;
            int m_x, m_x0, m_y, m_distanceBetweenPixelsInclusive;
            byte[] m_buffer;
            int m_currentBufferOffset = -1;
            int m_src_width;
            int m_src_height;

            public ImageBufferAccessor(IImageReaderWriter imgRW)
            {
                Attach(imgRW);
            }

            void Attach(IImageReaderWriter imgRW)
            {
                this.imgRW = imgRW;
                m_buffer = imgRW.GetBuffer();
                m_src_width = imgRW.Width;
                m_src_height = imgRW.Height;
                m_distanceBetweenPixelsInclusive = imgRW.BytesBetweenPixelsInclusive;
            }


            byte[] GetPixels(out int bufferByteOffset)
            {
                int x = m_x;
                int y = m_y;
                unchecked
                {
                    if ((uint)x >= (uint)m_src_width)
                    {
                        if (x < 0)
                        {
                            x = 0;
                        }
                        else
                        {
                            x = (int)m_src_width - 1;
                        }
                    }

                    if ((uint)y >= (uint)m_src_height)
                    {
                        if (y < 0)
                        {
                            y = 0;
                        }
                        else
                        {
                            y = (int)m_src_height - 1;
                        }
                    }
                }

                bufferByteOffset = imgRW.GetBufferOffsetXY(x, y);
                return imgRW.GetBuffer();
            }

            public byte[] GetSpan(int x, int y, int len, out int bufferOffset)
            {
                m_x = m_x0 = x;
                m_y = y;
                unchecked
                {
                    if ((uint)y < (uint)m_src_height
                        && x >= 0 && x + len <= (int)m_src_width)
                    {
                        bufferOffset = imgRW.GetBufferOffsetXY(x, y);
                        m_buffer = imgRW.GetBuffer();
                        m_currentBufferOffset = bufferOffset;
                        return m_buffer;
                    }
                }

                m_currentBufferOffset = -1;
                return GetPixels(out bufferOffset);
            }

            public byte[] NextX(out int bufferOffset)
            {
                // this is the code (managed) that the original agg used.  
                // It looks like it doesn't check x but, It should be a bit faster and is valid 
                // because "span" checked the whole length for good x.
                if (m_currentBufferOffset != -1)
                {
                    m_currentBufferOffset += m_distanceBetweenPixelsInclusive;
                    bufferOffset = m_currentBufferOffset;
                    return m_buffer;
                }
                ++m_x;
                return GetPixels(out bufferOffset);
            }

            public byte[] NextY(out int bufferOffset)
            {
                ++m_y;
                m_x = m_x0;
                if (m_currentBufferOffset != -1
                    && (uint)m_y < (uint)imgRW.Height)
                {
                    m_currentBufferOffset = imgRW.GetBufferOffsetXY(m_x, m_y);
                    imgRW.GetBuffer();
                    bufferOffset = m_currentBufferOffset; ;
                    return m_buffer;
                }

                m_currentBufferOffset = -1;
                return GetPixels(out bufferOffset);
            }
        }
<<<<<<< HEAD

=======
>>>>>>> 459_retro
    }
}