﻿using System;
using System.Collections.Generic;
using FlutterBinding.Mapping;
using FlutterBinding.Shell;
using Newtonsoft.Json;
using static FlutterBinding.Mapping.Helper;

namespace FlutterBinding.UI
{
    // These appear to be called from the C++ Window

    public static class Hooks
    {
        static void _updateWindowMetrics(
            double devicePixelRatio,
            double width,
            double height,
            double paddingTop,
            double paddingRight,
            double paddingBottom,
            double paddingLeft,
            double viewInsetTop,
            double viewInsetRight,
            double viewInsetBottom,
            double viewInsetLeft)
        {
            var window = Window.Instance;
            window.devicePixelRatio = devicePixelRatio;
            window.physicalSize     = new Size(width, height);
            window.padding = new WindowPadding(
                top: paddingTop,
                right: paddingRight,
                bottom: paddingBottom,
                left: paddingLeft);
            window.viewInsets = new WindowPadding(
                top: viewInsetTop,
                right: viewInsetRight,
                bottom: viewInsetBottom,
                left: viewInsetLeft);
            _invoke(Window.Instance.onMetricsChanged, Window.Instance.OnMetricsChangedZone);
        }

        delegate string _LocaleClosure();

        static String _localeClosure() => Window.Instance.locale.ToString();

        static _LocaleClosure _getLocaleClosure() => _localeClosure;

        static void _updateLocales(List<String> locales)
        {
            const int stringsPerLocale = 4;
            int numLocales = (int)Math.Truncate((double)locales.Count / stringsPerLocale);
            Window.Instance.locales = new List<Locale>(numLocales);
            for (int localeIndex = 0; localeIndex < numLocales; localeIndex++)
            {
                Window.Instance.locales[localeIndex] = new Locale(
                    locales[localeIndex * stringsPerLocale],
                    locales[localeIndex * stringsPerLocale + 1]);
            }

            _invoke(Window.Instance.onLocaleChanged, Window.Instance.OnLocaleChangedZone);
        }

        static void _updateUserSettingsData(String jsonData)
        {
            Dictionary<String, Object> data = JsonConvert.DeserializeObject<Dictionary<String, Object>>(jsonData);
            _updateTextScaleFactor(Convert.ToDouble(data["textScaleFactor"]));
            _updateAlwaysUse24HourFormat(Convert.ToBoolean(data["alwaysUse24HourFormat"]));
        }

        static void _updateTextScaleFactor(double textScaleFactor)
        {
            Window.Instance.textScaleFactor = textScaleFactor;
            _invoke(Window.Instance.onTextScaleFactorChanged, Window.Instance.OnTextScaleFactorChangedZone);
        }

        static void _updateAlwaysUse24HourFormat(bool alwaysUse24HourFormat)
        {
            Window.Instance.alwaysUse24HourFormat = alwaysUse24HourFormat;
        }

        static void _updateSemanticsEnabled(bool enabled)
        {
            Window.Instance.semanticsEnabled = enabled;
            _invoke(Window.Instance.onSemanticsEnabledChanged, Window.Instance.OnSemanticsEnabledChangedZone);
        }

        static void _updateAccessibilityFeatures(int values)
        {
            AccessibilityFeatures newFeatures = (AccessibilityFeatures)values;
            if (newFeatures == Window.Instance.accessibilityFeatures)
                return;
            Window.Instance.accessibilityFeatures = newFeatures;
            _invoke(Window.Instance.onAccessibilityFeaturesChanged, Window.Instance.OnAccessibilityFlagsChangedZone);
        }

        //static void _dispatchPlatformMessage(PlatformMessage platformMessage)
        //{
        //    if (Window.Instance.onPlatformMessage != null)
        //    {
        //        _invoke(
        //            () => Window.Instance.onPlatformMessage(platformMessage),
        //            Window.Instance.OnPlatformMessageZone);
        //    }
        //    //else
        //    //{
        //    //    Window.Instance.RespondToPlatformMessage(responseId, null);
        //    //}
        //}

        static void _dispatchPointerDataPacket(Types.ByteData packet)
        {
            if (Window.Instance.onPointerDataPacket != null)
                _invoke1<PointerDataPacket>((d) => Window.Instance.onPointerDataPacket(d), Window.Instance.OnPointerDataPacketZone, _unpackPointerDataPacket(packet));
        }

        static void _dispatchSemanticsAction(int id, int action, Types.ByteData args)
        {
            _invoke3<int, SemanticsAction, Types.ByteData>(
                (a, b, c) => Window.Instance.onSemanticsAction(a, b, c),
                Window.Instance.OnSemanticsActionZone,
                id,
                (SemanticsAction)action,
                args);
        }

        static void _beginFrame(int microseconds)
        {
            _invoke1<Types.Duration>((d) => Window.Instance.onBeginFrame(d), Window.Instance.OnBeginFrameZone, new Types.Duration(microseconds: microseconds));
        }

        static void _drawFrame()
        {
            _invoke(Window.Instance.onDrawFrame, Window.Instance.OnDrawFrameZone);
        }

        /// Invokes [callback] inside the given [zone].
        static void _invoke(VoidCallback callback, Types.Zone zone)
        {
            if (callback == null)
                return;

            //assert(zone != null);

            if (identical(zone, Types.Zone.current))
            {
                callback();
            }
            else
            {
                zone.runGuarded(callback);
            }
        }

        /// Invokes [callback] inside the given [zone] passing it [arg].
        static void _invoke1<A>(Action<A> callback, Types.Zone zone, A arg)
        {
            if (callback == null)
                return;

            if (identical(zone, Types.Zone.current))
            {
                callback(arg);
            }
            else
            {
                zone.runUnaryGuarded<A>(callback, arg);
            }
        }

        /// Invokes [callback] inside the given [zone] passing it [arg1] and [arg2].
        static void _invoke2<A1, A2>(Action<A1, A2> callback, Types.Zone zone, A1 arg1, A2 arg2)
        {
            if (callback == null)
                return;

            if (identical(zone, Types.Zone.current))
            {
                callback(arg1, arg2);
            }
            else
            {
                zone.runBinaryGuarded<A1, A2>(callback, arg1, arg2);
            }
        }

        /// Invokes [callback] inside the given [zone] passing it [arg1], [arg2] and [arg3].
        static void _invoke3<A1, A2, A3>(Action<A1, A2, A3> callback, Types.Zone zone, A1 arg1, A2 arg2, A3 arg3)
        {
            if (callback == null)
                return;

            if (identical(zone, Types.Zone.current))
            {
                callback(arg1, arg2, arg3);
            }
            else
            {
                zone.runGuarded(() => { callback(arg1, arg2, arg3); });
            }
        }

        // If this value changes, update the encoding code in the following files:
        //
        //  * pointer_data.cc
        //  * FlutterView.java
        const int _kPointerDataFieldCount = 19;

        static PointerDataPacket _unpackPointerDataPacket(Types.ByteData packet)
        {
            const int kStride = 8; // Its an 8 const anyway - Int64List.bytesPerElement;
            const int kBytesPerPointerData = _kPointerDataFieldCount * kStride;
            int length = packet.lengthInBytes / kBytesPerPointerData;
            List<PointerData> data = new List<PointerData>(length);
            for (int i = 0; i < length; ++i)
            {
                int offset = i * _kPointerDataFieldCount;
                data[i] = new PointerData(
                    timeStamp: new Types.Duration(microseconds: packet.getInt64(kStride * offset++, (int)Painting._kFakeHostEndian)),
                    change: (PointerChange)packet.getInt64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    kind: (PointerDeviceKind)packet.getInt64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    device: packet.getInt64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    physicalX: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    physicalY: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    buttons: packet.getInt64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    obscured: packet.getInt64(kStride * offset++, (int)Painting._kFakeHostEndian) != 0,
                    pressure: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    pressureMin: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    pressureMax: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    distance: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    distanceMax: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    radiusMajor: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    radiusMinor: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    radiusMin: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    radiusMax: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    orientation: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian),
                    tilt: packet.getFloat64(kStride * offset++, (int)Painting._kFakeHostEndian)
                );
            }

            return new PointerDataPacket(data: data);
        }
    }
}
