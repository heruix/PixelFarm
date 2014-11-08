﻿//2014 BSD,WinterDev   
using System;
namespace PixelFarm.Agg
{
    public struct ScanlineSpan
    {
        public short x;
        public short len;
        public short cover_index;

        public ScanlineSpan(int x, int cover_index)
        {
            this.x = (short)x;
            this.len = 1;
            this.cover_index = (short)cover_index;
        }
        public ScanlineSpan(int x, int len, int cover_index)
        {
            this.x = (short)x;
            this.len = (short)len;
            this.cover_index = (short)cover_index;
        }
#if DEBUG
        public override string ToString()
        {
            return "x:" + x + ",len:" + len + ",cover:" + cover_index;
        }
#endif
    }
}