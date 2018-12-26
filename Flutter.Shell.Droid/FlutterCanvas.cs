﻿using Android.OS;
using Android.Support.V7.App;
using FlutterBinding.UI;
using SkiaSharp.Views.Android;

namespace Flutter.Shell.Droid
{
    public class FlutterCanvas : AppCompatActivity
    {
        private SKCanvasView Canvas { get; set; }
        private FlutterSurface Surface { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.FlutterPage);

            Canvas = FindViewById<SKCanvasView>(Resource.Id.skiaView);

            var scale = Resources.DisplayMetrics.Density;
            Surface = new FlutterSurface(scale);

            Canvas.PaintSurface += OnCanvasPaintSurface;
        }

        private void OnCanvasPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            Surface.OnPaintSurface(args.Surface, args.Info);
        }

        protected override void OnDestroy()
        {
            Canvas.PaintSurface -= OnCanvasPaintSurface;
            base.OnDestroy();
        }
    }
}
