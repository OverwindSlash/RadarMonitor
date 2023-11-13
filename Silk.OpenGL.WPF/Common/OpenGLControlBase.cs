using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Silk.WPF.Common;

public abstract class OpenGLControlBase<TFrame> : Control where TFrame : FramebufferBase
{
    public static readonly DependencyProperty FpsProperty = DependencyProperty.Register(nameof(Fps), typeof(int), typeof(OpenGLControlBase<TFrame>), new PropertyMetadata(0));

    protected readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly List<int> _fpsSample = new();

    protected TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);
    protected TimeSpan _lastFrameStamp;

    protected TFrame Framebuffer { get; set; }
    public int Fps
    {
        get { return (int)GetValue(FpsProperty); }
        set { SetValue(FpsProperty, value); }
    }

    public abstract event Action Ready;
    public abstract event Action<TimeSpan> Render;
    public abstract event Action<object, TimeSpan> UpdateFrame;

    public void Start()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            IsVisibleChanged += (_, e) =>
            {
                if ((bool)e.NewValue)
                {
                    CompositionTarget.Rendering += CompositionTarget_Rendering;
                }
                else
                {
                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                }
            };

            Loaded += (_, _) => InvalidateVisual();

            OnStart();

            //SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }
    }

    private void CompositionTarget_Rendering(object sender, EventArgs e)
    {
        RenderingEventArgs args = (RenderingEventArgs)e;

        if (_lastRenderTime != args.RenderingTime)
        {
            InvalidateVisual();

            _fpsSample.Add(Convert.ToInt32(1000.0d / (args.RenderingTime.TotalMilliseconds - _lastRenderTime.TotalMilliseconds)));
            // 样本数 30
            if (_fpsSample.Count == 30)
            {
                Fps = Convert.ToInt32(_fpsSample.Average());
                _fpsSample.Clear();
            }

            _lastRenderTime = args.RenderingTime;
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            OnSizeChanged(sizeInfo);
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            DesignTimeHelper.DrawDesign(this, drawingContext);
        }
        else
        {
            if (Framebuffer != null && Framebuffer.D3dImage.IsFrontBufferAvailable)
            {
                OnDraw(drawingContext);

                _stopwatch.Restart();
            }
        }
    }

    private async void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            await Task.Delay(1000);
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                OnSessionChanged();
            }

        }
        else if (e.Reason == SessionSwitchReason.SessionLock)
        {
            //if (!DesignerProperties.GetIsInDesignMode(this))
            //{
            //    OnSessionChanged();
            //}
        }
    }

    protected abstract void OnStart();
    protected abstract void OnDraw(DrawingContext drawingContext);
    protected abstract void OnSizeChanged(SizeChangedInfo sizeInfo);

    public abstract void OnSessionChanged();

}
