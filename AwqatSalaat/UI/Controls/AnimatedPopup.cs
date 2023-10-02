using AwqatSalaat.Interop;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AwqatSalaat.UI.Controls
{
    public class AnimatedPopup : Popup
    {
        // The purpose of this metadata is to avoid merging propertychanged callbacks
        private class CustomFrameworkPropertyMetadata : FrameworkPropertyMetadata
        {
            public CustomFrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions options, PropertyChangedCallback propertyChangedCallback)
                : base(defaultValue, options, propertyChangedCallback) { }

            protected override void Merge(PropertyMetadata baseMetadata, DependencyProperty dp)
            {
                PropertyChangedCallback propertyChangedCallback = new PropertyChangedCallback(PropertyChangedCallback);
                base.Merge(baseMetadata, dp);
                PropertyChangedCallback = propertyChangedCallback;
            }
        }

        private class AnimationContext : IDisposable
        {
            private bool disposedValue;

            public AnimationNativeData NativeData { get; } = new AnimationNativeData();
            public DateTime StartTime { get; }
            public DateTime EndTime => StartTime.Add(Duration);
            public TimeSpan Duration { get; }
            public RECT OriginRect { get; }
            public IEasingFunction EasingFunction { get; }
            public RelativePlacement Placement { get; }

            public AnimationContext(TimeSpan duration, RECT originRect, RelativePlacement placement, IEasingFunction easingFunction, bool delay)
            {
                Duration = duration;
                OriginRect = originRect;
                Placement = placement;
                EasingFunction = easingFunction;
                StartTime = DateTime.Now.AddMilliseconds(delay ? animationDelay : 0);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        NativeData.Dispose();
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private class AnimationNativeData : IDisposable
        {
            public readonly IntPtr BlendFunctionPtr = Marshal.AllocHGlobal(Marshal.SizeOf<BLENDFUNCTION>());
            public readonly IntPtr DestinationPointPtr = Marshal.AllocHGlobal(Marshal.SizeOf<POINT>());
            public readonly IntPtr SizePtr = Marshal.AllocHGlobal(Marshal.SizeOf<SIZE>());

            private bool disposedValue;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects)
                    }

                    Marshal.FreeHGlobal(SizePtr);
                    Marshal.FreeHGlobal(DestinationPointPtr);
                    Marshal.FreeHGlobal(BlendFunctionPtr);

                    disposedValue = true;
                }
            }

            ~AnimationNativeData()
            {
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private enum RelativePlacement
        {
            Bottom,
            Right,
            Top,
            Left
        }

        // The main reason for this is to control the closure of the popup to be able to play closing animation BEFORE hiding the window
        private static readonly FrameworkPropertyMetadata baseIsOpenMetadata;

        // (in ms) This is the delay for waiting the system to finish setting up the window and showing it (it won't be drawn)
        private const int animationDelay = 50;

        // (in px) This is used as the distance for translation animation
        private const int animationPosOffset = 50;

        static AnimatedPopup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedPopup), new FrameworkPropertyMetadata(typeof(AnimatedPopup)));
            baseIsOpenMetadata = (FrameworkPropertyMetadata)IsOpenProperty.GetMetadata(typeof(AnimatedPopup));
            IsOpenProperty.OverrideMetadata(
                typeof(AnimatedPopup),
                new CustomFrameworkPropertyMetadata(
                    baseIsOpenMetadata.DefaultValue,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    propertyChangedCallback: OnIsOpenChanged));
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AnimatedPopup popup = (AnimatedPopup)d;
            bool isShow = (bool)e.NewValue;
            if (isShow)
            {
                if (popup.UseNativeAnimation)
                {
                    // Disable popuproot animation if exist
                    popup.SetCurrentValue(PopupAnimationProperty, PopupAnimation.None);
                }
                baseIsOpenMetadata.PropertyChangedCallback.Invoke(d, e);
            }
            else
            {
                popup.OnCloseRequested();
                DispatcherTimer timer = new DispatcherTimer();
                timer.Tick += (s, ea) =>
                {
                    timer.Stop();
                    baseIsOpenMetadata.PropertyChangedCallback.Invoke(d, e);
                };
                timer.Interval = popup.UseNativeAnimation ? popup.DefaultNativeAnimationDuration : TimeSpan.Zero;
                timer.Start();
            }
        }

        private readonly IntPtr hiddenBlendFuncPtr;
        private readonly IntPtr visibleBlendFuncPtr;
        private readonly DispatcherTimer openingAnimationTimer;
        private readonly DispatcherTimer closingAnimationTimer;
        private HwndSource hwndSource;
        private AnimationContext animationCtx;

        protected HwndSource HwndSource => hwndSource;
        protected IntPtr Handle => hwndSource?.Handle ?? throw new InvalidOperationException();

        public virtual IEasingFunction OpeningEasingFunction { get; } = new ExponentialEase() { Exponent = 4, EasingMode = EasingMode.EaseOut };
        public virtual IEasingFunction ClosingEasingFunction { get; } = new ExponentialEase() { Exponent = 4, EasingMode = EasingMode.EaseOut };
        public virtual TimeSpan DefaultNativeAnimationDuration { get; } = TimeSpan.FromMilliseconds(250);
        public virtual bool AnimateSizeOnOpening { get; } = true;
        public virtual bool AnimateOpacityOnOpening { get; } = true;
        public virtual bool AnimatePositionOnOpening { get; } = true;
        public virtual bool AnimateSizeOnClosing { get; } = true;
        public virtual bool AnimateOpacityOnClosing { get; } = true;
        public virtual bool AnimatePositionOnClosing { get; } = true;
        public bool UseNativeAnimation { get; set; } = true;

        public AnimatedPopup() : base()
        {
            // We will use this to hide the window before the start of animation
            BLENDFUNCTION pblend = new BLENDFUNCTION(BlendOperation.AC_SRC_OVER, 0, 0, AlphaFormat.AC_SRC_ALPHA);
            hiddenBlendFuncPtr = Marshal.AllocHGlobal(Marshal.SizeOf<BLENDFUNCTION>());
            Marshal.StructureToPtr(pblend, hiddenBlendFuncPtr, false);

            // We will use this to make the window visible if opacity animation is disabled
            pblend.SourceConstantAlpha = 255;
            visibleBlendFuncPtr = Marshal.AllocHGlobal(Marshal.SizeOf<BLENDFUNCTION>());
            Marshal.StructureToPtr(pblend, visibleBlendFuncPtr, false);

            // "Send" is the highest priority so it helps in getting better timer resolution
            openingAnimationTimer = new DispatcherTimer(DispatcherPriority.Send);
            openingAnimationTimer.Tick += NativeOpeningAnimation;
            openingAnimationTimer.Interval = TimeSpan.FromMilliseconds(10);

            // "Send" is the highest priority so it helps in getting better timer resolution
            closingAnimationTimer = new DispatcherTimer(DispatcherPriority.Send);
            closingAnimationTimer.Tick += NativeClosingAnimation;
            closingAnimationTimer.Interval = TimeSpan.FromMilliseconds(10);
        }

        protected sealed override void OnOpened(EventArgs e)
        {
            hwndSource = (HwndSource)HwndSource.FromVisual(this.Child);

            if (UseNativeAnimation)
            {
                // Disable drawing to avoid showing the window at its initial position/opacity (flicker)
                User32.SendMessage(Handle, (uint)WindowMessage.WM_SETREDRAW, 0, 0);

                // Begin animation when rendering is finished. This help to avoid flicker at the begining
                hwndSource.ContentRendered += (_, __) => BeginNativeOpeningAnimation(DefaultNativeAnimationDuration);
            }

            base.OnOpened(e);
        }

        protected sealed override void OnClosed(EventArgs e)
        {
            closingAnimationTimer?.Stop();
            animationCtx?.Dispose();
            hwndSource = null;
            base.OnClosed(e);
        }

        private void OnCloseRequested()
        {
            openingAnimationTimer?.Stop();
            if (UseNativeAnimation)
            {
                BeginNativeClosingAnimation(DefaultNativeAnimationDuration);
            }
        }

        private void BeginNativeClosingAnimation(TimeSpan duration)
        {
            Child.SetCurrentValue(IsHitTestVisibleProperty, false);
            OnClosingAnimationStarting();

            User32.GetWindowRect(Handle, out RECT orgRect);

            animationCtx = new AnimationContext(duration, orgRect, GetRelativePlacement(orgRect), ClosingEasingFunction, false);

            closingAnimationTimer.Start();

            OnClosingAnimationStarted();
        }

        private void BeginNativeOpeningAnimation(TimeSpan duration)
        {
            Child.SetCurrentValue(IsHitTestVisibleProperty, false);
            OnOpeningAnimationStarting();

            User32.GetWindowRect(Handle, out RECT orgRect);

            animationCtx = new AnimationContext(duration, orgRect, GetRelativePlacement(orgRect), OpeningEasingFunction, true);

            openingAnimationTimer.Start();

            Task.Delay(animationDelay).ContinueWith(t =>
            {
                User32.SendMessage(Handle, (uint)WindowMessage.WM_SETREDRAW, 1, 0);
                Dispatcher.BeginInvoke(new Action(OnOpeningAnimationStarted), null);
            });
        }

        private void NativeOpeningAnimation(object sender, EventArgs e)
        {
            if (animationCtx is null)
            {
                return;
            }

            // If we are in the delay period, then hide the window
            if (animationCtx.StartTime > DateTime.Now)
            {
                User32.UpdateLayeredWindow(hwndSource.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, hiddenBlendFuncPtr, UpdateLayeredWindowFlags.ULW_ALPHA);
                return;
            }

            // We reached end of animation
            if (DateTime.Now >= animationCtx.EndTime)
            {
                openingAnimationTimer.Stop();

                // in case the animation didn't reach 100%
                POINT ppoint = new POINT() { x = animationCtx.OriginRect.left, y = animationCtx.OriginRect.top };

                Marshal.StructureToPtr(ppoint, animationCtx.NativeData.DestinationPointPtr, false);

                User32.UpdateLayeredWindow(hwndSource.Handle, IntPtr.Zero, animationCtx.NativeData.DestinationPointPtr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, visibleBlendFuncPtr, UpdateLayeredWindowFlags.ULW_ALPHA);

                animationCtx.Dispose();
                animationCtx = null;

                OnOpeningAnimationCompleted();
                Child.ClearValue(IsHitTestVisibleProperty);
            }
            else
            {
                double progress = (DateTime.Now - animationCtx.StartTime).TotalMilliseconds / animationCtx.Duration.TotalMilliseconds;
                double ease = animationCtx.EasingFunction.Ease(progress);
                byte alpha = (byte)(ease * 255);

                BLENDFUNCTION pblend = new BLENDFUNCTION(BlendOperation.AC_SRC_OVER, 0, alpha, AlphaFormat.AC_SRC_ALPHA);
                POINT ppoint = new POINT() { x = animationCtx.OriginRect.left, y = animationCtx.OriginRect.top };
                switch (animationCtx.Placement)
                {
                    case RelativePlacement.Bottom:
                        ppoint.y -= Convert.ToInt32(animationPosOffset * (1 - ease));
                        break;
                    case RelativePlacement.Top:
                        ppoint.y += Convert.ToInt32(animationPosOffset * (1 - ease));
                        break;
                    case RelativePlacement.Right:
                        ppoint.x -= Convert.ToInt32(animationPosOffset * (1 - ease));
                        break;
                    case RelativePlacement.Left:
                        ppoint.x += Convert.ToInt32(animationPosOffset * (1 - ease));
                        break;
                }
                SIZE psize = new SIZE { cx = animationCtx.OriginRect.right - ppoint.x, cy = animationCtx.OriginRect.bottom - ppoint.y };

                if (AnimateOpacityOnOpening)
                {
                    Marshal.StructureToPtr(pblend, animationCtx.NativeData.BlendFunctionPtr, false);
                }
                if (AnimatePositionOnOpening)
                {
                    Marshal.StructureToPtr(ppoint, animationCtx.NativeData.DestinationPointPtr, false);
                }
                if (AnimateSizeOnOpening)
                {
                    Marshal.StructureToPtr(psize, animationCtx.NativeData.SizePtr, false);
                }

                User32.UpdateLayeredWindow(
                    hwndSource.Handle,
                    IntPtr.Zero,
                    AnimatePositionOnOpening ? animationCtx.NativeData.DestinationPointPtr : IntPtr.Zero,
                    AnimateSizeOnOpening ? animationCtx.NativeData.SizePtr : IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    AnimateOpacityOnOpening ? animationCtx.NativeData.BlendFunctionPtr : visibleBlendFuncPtr,
                    UpdateLayeredWindowFlags.ULW_ALPHA);

                OnOpeningAnimationProgressed(progress);
            }
        }

        private void NativeClosingAnimation(object sender, EventArgs e)
        {
            if (animationCtx is null)
            {
                return;
            }

            // We reached end of animation
            if (DateTime.Now >= animationCtx.EndTime)
            {
                closingAnimationTimer.Stop();

                animationCtx.Dispose();
                animationCtx = null;

                OnClosingAnimationCompleted();
                Child.ClearValue(IsHitTestVisibleProperty);
            }
            else
            {
                double progress = (DateTime.Now - animationCtx.StartTime).TotalMilliseconds / animationCtx.Duration.TotalMilliseconds;
                double ease = animationCtx.EasingFunction.Ease(progress);
                byte alpha = (byte)((1 - ease) * 255);

                BLENDFUNCTION pblend = new BLENDFUNCTION(BlendOperation.AC_SRC_OVER, 0, alpha, AlphaFormat.AC_SRC_ALPHA);
                POINT ppoint = new POINT() { x = animationCtx.OriginRect.left, y = animationCtx.OriginRect.top };
                switch (animationCtx.Placement)
                {
                    case RelativePlacement.Bottom:
                        ppoint.y -= Convert.ToInt32(animationPosOffset * ease);
                        break;
                    case RelativePlacement.Top:
                        ppoint.y += Convert.ToInt32(animationPosOffset * ease);
                        break;
                    case RelativePlacement.Right:
                        ppoint.x -= Convert.ToInt32(animationPosOffset * ease);
                        break;
                    case RelativePlacement.Left:
                        ppoint.x += Convert.ToInt32(animationPosOffset * ease);
                        break;
                }
                SIZE psize = new SIZE { cx = animationCtx.OriginRect.right - ppoint.x, cy = animationCtx.OriginRect.bottom - ppoint.y };

                if (AnimateOpacityOnClosing)
                {
                    Marshal.StructureToPtr(pblend, animationCtx.NativeData.BlendFunctionPtr, false);
                }
                if (AnimatePositionOnClosing)
                {
                    Marshal.StructureToPtr(ppoint, animationCtx.NativeData.DestinationPointPtr, false);
                }
                if (AnimateSizeOnClosing)
                {
                    Marshal.StructureToPtr(psize, animationCtx.NativeData.SizePtr, false);
                }

                User32.UpdateLayeredWindow(
                    hwndSource.Handle,
                    IntPtr.Zero,
                    AnimatePositionOnClosing ? animationCtx.NativeData.DestinationPointPtr : IntPtr.Zero,
                    AnimateSizeOnClosing ? animationCtx.NativeData.SizePtr : IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    AnimateOpacityOnClosing ? animationCtx.NativeData.BlendFunctionPtr : visibleBlendFuncPtr,
                    UpdateLayeredWindowFlags.ULW_ALPHA);

                OnClosingAnimationProgressed(progress);
            }
        }

        protected virtual void OnOpeningAnimationStarting() { }
        protected virtual void OnOpeningAnimationStarted() { }
        protected virtual void OnOpeningAnimationProgressed(double progress) { }
        protected virtual void OnOpeningAnimationCompleted() { }
        protected virtual void OnClosingAnimationStarting() { }
        protected virtual void OnClosingAnimationStarted() { }
        protected virtual void OnClosingAnimationProgressed(double progress) { }
        protected virtual void OnClosingAnimationCompleted() { }

        private RelativePlacement GetRelativePlacement(RECT popupRect)
        {
            var positionInParent = TransformToAncestor(VisualParent as Visual).Transform(new Point());
            var parentWndHandle = ((HwndSource)HwndSource.FromVisual(VisualParent as Visual)).Handle;
            User32.GetWindowRect(parentWndHandle, out RECT parentRect);
            parentRect.left += Convert.ToInt32(positionInParent.X);
            parentRect.top += Convert.ToInt32(positionInParent.Y);

            int topToBottom = Math.Abs(popupRect.top - parentRect.bottom);
            int leftToRight = Math.Abs(popupRect.left - parentRect.right);
            int bottomToTop = Math.Abs(popupRect.bottom - parentRect.top);
            int rightToLeft = Math.Abs(popupRect.right - parentRect.left);

            int min = new[] { topToBottom, leftToRight, bottomToTop, rightToLeft }.Min();

            if (topToBottom == min)
            {
                return RelativePlacement.Bottom;
            }
            else if (leftToRight == min)
            {
                return RelativePlacement.Right;
            }
            else if (bottomToTop == min)
            {
                return RelativePlacement.Top;
            }
            else
            {
                return RelativePlacement.Left;
            }
        }

        ~AnimatedPopup()
        {
            Marshal.FreeHGlobal(hiddenBlendFuncPtr);
            Marshal.FreeHGlobal(visibleBlendFuncPtr);
        }
    }
}
