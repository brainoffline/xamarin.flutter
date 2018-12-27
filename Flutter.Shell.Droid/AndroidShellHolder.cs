﻿using Android.Util;
using Flutter.Shell.Droid.View;
using FlutterBinding.Engine;
using FlutterBinding.Engine.Assets;
using FlutterBinding.Shell;
using FlutterBinding.UI;
using System.Threading;
using System.Threading.Tasks;
using Engine = FlutterBinding.Shell.Engine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Flutter.Shell.Droid
{
    public class AndroidShellHolder
    {
        private const string Tag = "AndroidShellHolder";

        private Settings _settings;
        private FlutterNativeView _view;
        public PlatformView PlatformView { get; private set; }
        private ThreadHost _threadHost;
        private bool _isBackgroundView;
        private FlutterBinding.Shell.Shell _shell;
        private static int _shellCount = 0;

        private AndroidShellHolder() { }


        public static async Task<AndroidShellHolder> Create(
            Settings settings,
            FlutterNativeView view,
            bool isBackgroundView)
        {
            AndroidShellHolder holder = new AndroidShellHolder
            {
                _settings         = settings,
                _view             = view,
                _isBackgroundView = isBackgroundView
            };

            Interlocked.Increment(ref _shellCount);

            string threadLabel = $"Shell:{_shellCount}";

            if (isBackgroundView)
                holder._threadHost = new ThreadHost(threadLabel, ThreadHost.Type.UI | ThreadHost.Type.Platform);
            else
                holder._threadHost = new ThreadHost(threadLabel, ThreadHost.Type.UI | ThreadHost.Type.Platform | ThreadHost.Type.GPU | ThreadHost.Type.IO);

            TaskRunner gpuTR, ioTR;
            if (isBackgroundView)
            {
                gpuTR = ioTR = holder._threadHost.UIThread;
            }
            else
            {
                gpuTR = holder._threadHost.GPUThread;
                ioTR  = holder._threadHost.IOThread;
            }

            TaskRunners taskRunners = new TaskRunners(
                threadLabel,
                holder._threadHost.PlatformThread,
                gpuTR,
                holder._threadHost.UIThread,
                ioTR
            );

            holder._shell = await FlutterBinding.Shell.Shell.Create(
                taskRunners,
                holder._settings,
                OnCreatePlatformView,
                OnCreateRasterizer);

            holder.IsValid = holder._shell != null;

            if (holder.IsValid)
            {
                // Description of Android thread priority
                // https://medium.com/mindorks/exploring-android-thread-priority-5d0542eebbd1
                taskRunners.GPUTaskRunner.PostTask(
                    () =>
                    {
                        // Android describes -8 as "most important display threads, for
                        // compositing the screen and retrieving input events". Conservatively
                        // set the GPU thread to slightly lower priority than it.
                        Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    });
                taskRunners.UITaskRunner.PostTask(() => { Thread.CurrentThread.Priority = ThreadPriority.AboveNormal; });
            }

            return holder;

            // Local functions
            PlatformView OnCreatePlatformView(FlutterBinding.Shell.Shell shell)
            {
                holder.PlatformView = new AndroidPlatformView(
                    shell,
                    shell.TaskRunners,
                    holder._view);

                return holder.PlatformView;
            }

            Rasterizer OnCreateRasterizer(FlutterBinding.Shell.Shell shell) => new Rasterizer(shell.TaskRunners);
        }

        public bool IsValid { get; private set; }

        public void Launch(RunConfiguration configuration)
        {
            if (!IsValid) return;

            _shell.TaskRunners.UITaskRunner.PostTask(
                () =>
                {
                    Log.Info(Tag, "Attempting to launch engine configuration...");
                    Engine engine = _shell.Engine;
                    if (engine == null || engine.Run(configuration) == Engine.RunStatus.Failure)
                    {
                        Log.Error(Tag, "Could not launch engine in configuration.");
                    }
                    else
                    {
                        Log.Error(Tag, "Engine configuration successfully started and run.");
                    }
                });
        }

        public void SetViewportMetrics(ViewportMetrics metrics)
        {
            if (!IsValid) return;

            _shell.TaskRunners.UITaskRunner.PostTask(() => { _shell.Engine.SetViewportMetrics(metrics); });
        }

        public void DispatchPointerDataPacket(PointerDataPacket packet)
        {
            if (!IsValid) return;

            _shell.TaskRunners.UITaskRunner.PostTask(() => { _shell.Engine.DispatchPointerDataPacket(packet); });
        }

        public Settings GetSettings()
        {
            return _settings;
        }

        public Rasterizer.Screenshot Screenshot(Rasterizer.ScreenshotType type, bool base64Encode)
        {
            if (!IsValid) return new Rasterizer.Screenshot();

            return _shell.Screenshot(type, base64Encode).Result;
        }

        public void UpdateAssetManager(AssetManager assetManager)
        {
            if (!IsValid) return;

            _shell.Engine.UpdateAssetManager(assetManager);
        }
    }
}