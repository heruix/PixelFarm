﻿//MIT, 2019-present, WinterDev
//----------------------------------- 

using System;
using System.Collections.Generic;
using PixelFarm.DrawingGL;
using PixelFarm.Platforms;
namespace PixelFarm.Drawing.BitmapAtlas
{
    //TODO: review class and method names

    class MySimpleGLBitmapAtlasManager : BitmapAtlasManager<GLBitmap>
    {
        public MySimpleGLBitmapAtlasManager(TextureKind textureKind)
            : base(textureKind)
        {
            SetLoadNewBmpDel(atlas =>
            {
                //create new one
                AtlasItemImage totalGlyphImg = atlas.TotalImg;
                //load to glbmp  
                GLBitmap found = new GLBitmap(totalGlyphImg.Bitmap, false);
                found.IsYFlipped = false;
                return found;
            });
        }
    }   
}