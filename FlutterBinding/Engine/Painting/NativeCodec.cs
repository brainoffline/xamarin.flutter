﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FlutterBinding.UI;
using SkiaSharp;

namespace FlutterBinding.Engine.Painting
{
    public static class NativeCodec
    {
        public static Task<SKCodec> InstantiateImageCodec(List<int> list, ImageInfo imageInfo, double decodedCacheRatioCap)
        {
            return Task.FromResult((SKCodec)null);
        }

        public static SKImage GetImage(this SKCodecFrameInfo info, SKCodec codec)
        {
            return SKImage.Create(codec.Info);
        }
    }
}
