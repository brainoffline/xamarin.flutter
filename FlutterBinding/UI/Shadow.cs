﻿using System;
using System.Collections.Generic;
using FlutterBinding.Mapping;
using SkiaSharp;

namespace FlutterBinding.UI
{
    /// A single shadow.
    ///
    /// Multiple shadows are stacked together in a [TextStyle].
    public class Shadow
    {
        /// Construct a shadow.
        ///
        /// The default shadow is a black shadow with zero offset and zero blur.
        /// Default shadows should be completely covered by the casting element,
        /// and not be visible.
        ///
        /// Transparency should be adjusted through the [color] alpha.
        ///
        /// Shadow order matters due to compositing multiple translucent objects not
        /// being commutative.
        public Shadow(
            Color color = null,
            Offset offset = null,
            double blurRadius = 0.0)
        {
            //assert(color != null, 'Text shadow color was null.'),
            //assert(offset != null, 'Text shadow offset was null.'),
            //assert(blurRadius >= 0.0, 'Text shadow blur radius should be non-negative.');

            Color  = color ?? new Color(ColorDefault);
            Offset = offset ?? Offset.zero;
            BlurRadius = blurRadius;
        }

        private const uint ColorDefault = 0xFF000000;

        // Constants for shadow encoding.
        private const int BytesPerShadow = 16;
        private const int ColorOffset = 0 << 2;
        private const int XOffset = 1 << 2;
        private const int YOffset = 2 << 2;
        private const int BlurOffset = 3 << 2;

        /// Color that the shadow will be drawn with.
        ///
        /// The shadows are shapes composited directly over the base canvas, and do not
        /// represent optical occlusion.
        public Color Color { get; }

        /// The displacement of the shadow from the casting element.
        ///
        /// Positive x/y offsets will shift the shadow to the right and down, while
        /// negative offsets shift the shadow to the left and up. The offsets are
        /// relative to the position of the element that is casting it.
        public Offset Offset { get; }

        /// The standard deviation of the Gaussian to convolve with the shadow's shape.
        public double BlurRadius { get; }

        /// Converts a blur radius in pixels to sigmas.
        ///
        /// See the sigma argument to [MaskFilter.blur].
        ///
        // See SkBlurMask::ConvertRadiusToSigma().
        // <https://github.com/google/skia/blob/bb5b77db51d2e149ee66db284903572a5aac09be/src/effects/SkBlurMask.cpp#L23>
        public static double ConvertRadiusToSigma(double radius)
        {
            return radius * 0.57735 + 0.5;
        }

        /// The [blurRadius] in sigmas instead of logical pixels.
        ///
        /// See the sigma argument to [MaskFilter.blur].
        public double BlurSigma => ConvertRadiusToSigma(BlurRadius);

        /// Create the [Paint] object that corresponds to this shadow description.
        ///
        /// The [offset] is not represented in the [Paint] object.
        /// To honor this as well, the shape should be translated by [offset] before
        /// being filled using this [Paint].
        ///
        /// This class does not provide a way to disable shadows to avoid inconsistencies
        /// in shadow blur rendering, primarily as a method of reducing test flakiness.
        /// [toPaint] should be overriden in subclasses to provide this functionality.
        public Paint ToPaint()
        {
            return new Paint
            {
                Color      = Color,
                MaskFilter = MaskFilter.Blur(SKBlurStyle.Normal, BlurSigma)
            };
        }

        /// Returns a new shadow with its [offset] and [blurRadius] scaled by the given
        /// factor.
        public Shadow Scale(double factor)
        {
            return new Shadow(
                color: Color,
                offset: Offset * factor,
                blurRadius: BlurRadius * factor);
        }

        /// Linearly interpolate between two shadows.
        ///
        /// If either shadow is null, this function linearly interpolates from a
        /// a shadow that matches the other shadow in color but has a zero
        /// offset and a zero blurRadius.
        ///
        /// {@template dart.ui.shadow.lerp}
        /// The `t` argument represents position on the timeline, with 0.0 meaning
        /// that the interpolation has not started, returning `a` (or something
        /// equivalent to `a`), 1.0 meaning that the interpolation has finished,
        /// returning `b` (or something equivalent to `b`), and values in between
        /// meaning that the interpolation is at the relevant point on the timeline
        /// between `a` and `b`. The interpolation can be extrapolated beyond 0.0 and
        /// 1.0, so negative values and values greater than 1.0 are valid (and can
        /// easily be generated by curves such as [Curves.elasticInOut]).
        ///
        /// Values for `t` are usually obtained from an [Animation<double>], such as
        /// an [AnimationController].
        /// {@endtemplate}
        public static Shadow Lerp(Shadow a, Shadow b, double t)
        {
            //assert(t != null);
            if (a == null && b == null)
                return null;
            if (a == null)
                return b.Scale(t);
            if (b == null)
                return a.Scale(1.0 - t);
            return new Shadow(
                color: Color.Lerp(a.Color, b.Color, t),
                offset: Offset.lerp(a.Offset, b.Offset, t),
                blurRadius: UI.Lerp.lerpDouble(a.BlurRadius, b.BlurRadius, t));
        }

        /// Linearly interpolate between two lists of shadows.
        ///
        /// If the lists differ in length, excess items are lerped with null.
        ///
        /// {@macro dart.ui.shadow.lerp}
        private static List<Shadow> LerpList(List<Shadow> a, List<Shadow> b, double t)
        {
            //assert(t != null);
            if (a == null && b == null)
                return null;

            if (a == null)
                a = new List<Shadow>();

            if (b == null)
                b = new List<Shadow>();

            List<Shadow> result = new List<Shadow>();
            int commonLength = Math.Min(a.Count, b.Count);
            for (int i = 0; i < commonLength; i += 1)
                result.Add(Shadow.Lerp(a[i], b[i], t));
            for (int i = commonLength; i < a.Count; i += 1)
                result.Add(a[i].Scale(1.0 - t));
            for (int i = commonLength; i < b.Count; i += 1)
                result.Add(b[i].Scale(t));
            return result;
        }

        protected bool Equals(Shadow other)
        {
            return Equals(Color, other.Color) && Equals(Offset, other.Offset) && BlurRadius.Equals(other.BlurRadius);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Shadow)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Color != null ? Color.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Offset != null ? Offset.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ BlurRadius.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Shadow left, Shadow right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Shadow left, Shadow right)
        {
            return !Equals(left, right);
        }

        /// Determines if lists [a] and [b] are deep equivalent.
        ///
        /// Returns true if the lists are both null, or if they are both non-null, have
        /// the same length, and contain the same Shadows in the same order. Returns
        /// false otherwise.
        public static bool ShadowsListEquals(List<Shadow> a, List<Shadow> b)
        {
            // Compare _shadows
            if (a == null)
                return b == null;
            if (b == null || a.Count != b.Count)
                return false;
            for (int index = 0; index < a.Count; ++index)
                if (a[index] != b[index])
                    return false;
            return true;
        }

        // Serialize [shadows] into ByteData. The format is a single uint_32_t at
        // the beginning indicating the number of shadows, followed by BytesPerShadow
        // bytes for each shadow.
        public static Types.ByteData EncodeShadows(List<Shadow> shadows)
        {
            if (shadows == null)
                return new Types.ByteData(0);

            int byteCount = shadows.Count * BytesPerShadow;
            Types.ByteData shadowsData = new Types.ByteData(byteCount);

            for (int shadowIndex = 0; shadowIndex < shadows.Count; ++shadowIndex)
            {
                Shadow shadow = shadows[shadowIndex];
                if (shadow == null)
                    continue;
                var shadowOffset = shadowIndex * BytesPerShadow;

                shadowsData.setInt32(
                    ColorOffset + shadowOffset,
                    (int)(shadow.Color.Value ^ Shadow.ColorDefault),
                    (int)Painting.FakeHostEndian);

                shadowsData.setFloat32(
                    XOffset + shadowOffset,
                    shadow.Offset.dx,
                    (int)Painting.FakeHostEndian);

                shadowsData.setFloat32(
                    YOffset + shadowOffset,
                    shadow.Offset.dy,
                    (int)Painting.FakeHostEndian);

                shadowsData.setFloat32(
                    BlurOffset + shadowOffset,
                    shadow.BlurRadius,
                    (int)Painting.FakeHostEndian);
            }

            return shadowsData;
        }

        public override string ToString() => $"TextShadow({Color}, {Offset}, {BlurRadius})";
    }
}