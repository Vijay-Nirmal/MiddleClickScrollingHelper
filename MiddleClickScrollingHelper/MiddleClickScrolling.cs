using System;
using System.Diagnostics;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace MiddleClickScrollingHelper
{
    public class MiddleClickScrolling
    {
        private static bool isPressed = false;
        private static bool isMoved = false;
        private static Point? startPosition = null;
        private static bool isDeferredMovingStarted = false;
        private static double slowdown = 5;
        private static Point? currentPosition = null;
        private static Timer timer;
        private static ScrollViewer scrollViewer;

        // Using a DependencyProperty as the backing store for EnableMiddleClickScrolling.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableMiddleClickScrollingProperty =
            DependencyProperty.RegisterAttached("EnableMiddleClickScrolling", typeof(bool), typeof(MiddleClickScrolling), new PropertyMetadata(false, OnEnableMiddleClickScrollingChanged));

        public static bool GetEnableMiddleClickScrolling(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableMiddleClickScrollingProperty);
        }

        public static void SetEnableMiddleClickScrolling(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableMiddleClickScrollingProperty, value);
        }

        private static void OnEnableMiddleClickScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            scrollViewer = d as ScrollViewer;

            if (scrollViewer == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                scrollViewer.PointerPressed -= ScrollViewer_PointerPressed;
                scrollViewer.PointerPressed += ScrollViewer_PointerPressed;
            }
            else
            {
                scrollViewer.PointerPressed -= ScrollViewer_PointerPressed;
                UnsubscribeMiddleClickScrolling();
            }
        }

        private static void SubscribeMiddleClickScrolling()
        {
            isPressed = true;
            isMoved = false;
            isDeferredMovingStarted = false;
            timer = new Timer(ScrollAsync, null, 50, 50);
            
            scrollViewer.PointerMoved -= ScrollViewer_PointerMoved;
            scrollViewer.PointerReleased -= ScrollViewer_PointerReleased;
            
            scrollViewer.PointerMoved += ScrollViewer_PointerMoved;
            scrollViewer.PointerReleased += ScrollViewer_PointerReleased;
        }

        private static void UnsubscribeMiddleClickScrolling()
        {
            isPressed = false;
            isMoved = false;
            startPosition = null;
            currentPosition = null;
            isDeferredMovingStarted = false;

            timer.Dispose();
            
            scrollViewer.PointerMoved -= ScrollViewer_PointerMoved;
            scrollViewer.PointerReleased -= ScrollViewer_PointerReleased;

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private static async void ScrollAsync(object state)
        {
            Debug.WriteLine("Can Scroll - " + CanScroll());

            if (CanScroll())
            {
                var offsetX = currentPosition.Value.X - startPosition.Value.X;
                var offsetY = currentPosition.Value.Y - startPosition.Value.Y;

                Debug.WriteLine(offsetX + " - " + offsetY);

                SetCursorType(offsetX, offsetY);

                if (Math.Abs(offsetX) > 75.0 || Math.Abs(offsetY) > 75.0)
                {
                    offsetX = Math.Abs(offsetX) < 75.0 ? 0 : offsetX;
                    offsetY = Math.Abs(offsetY) < 75.0 ? 0 : offsetY;

                    offsetX /= slowdown;
                    offsetY /= slowdown;

                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        scrollViewer.ChangeView(scrollViewer.HorizontalOffset + offsetX, scrollViewer.VerticalOffset + offsetY, null);
                    });
                }
            }
        }

        private static bool CanScroll()
        {
            Debug.WriteLine("isPressed - " + isPressed + " - isDeferredMovingStarted - " + isDeferredMovingStarted);
            return isDeferredMovingStarted || (isPressed && !isDeferredMovingStarted);
        }

        private static void ScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (isDeferredMovingStarted)
            {
                UnsubscribeMiddleClickScrolling();
                return;
            }

            Pointer pointer = e.Pointer;

            if (pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                PointerPoint pointerPoint = e.GetCurrentPoint(scrollViewer);

                if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    startPosition = pointerPoint.Position;
                    currentPosition = pointerPoint.Position;
                    SubscribeMiddleClickScrolling();
                }
            }
        }

        private static void ScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isPressed && !isMoved)
            {
                PointerPoint pointerPoint = e.GetCurrentPoint(scrollViewer);

                if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    currentPosition = pointerPoint.Position;
                    var offsetX = currentPosition.Value.X - startPosition.Value.X;
                    var offsetY = currentPosition.Value.Y - startPosition.Value.Y;

                    if (Math.Abs(offsetX) > 75.0 || Math.Abs(offsetY) > 75.0)
                    {
                        isMoved = true;
                    }
                }
            }

            if(CanScroll())
            {
                PointerPoint pointerPoint = e.GetCurrentPoint(scrollViewer);

                currentPosition = pointerPoint.Position;
            }
        }

        private static void ScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isPressed && !isMoved)
            {
                isDeferredMovingStarted = true;
            }
            else
            {
                isDeferredMovingStarted = false;
            }

            Debug.WriteLine("isDeferredMovingStarted - " + isDeferredMovingStarted);

            if (isPressed && !isDeferredMovingStarted)
            {
                UnsubscribeMiddleClickScrolling();
            }
        }

        private static async void SetCursorType(double offsetX, double offsetY)
        {
            uint cursorID  =  101;

            if (Math.Abs(offsetX) < 75.0 && Math.Abs(offsetY) < 75.0)
            {
                cursorID = 101;
            }
            else
            {
                if (Math.Abs(offsetX) < 75.0 && offsetY < -75.0)
                {
                    cursorID = 102;
                }

                if (offsetX > 75.0 && offsetY < -75.0)
                {
                    cursorID = 103;
                }

                if (offsetX > 75.0 && Math.Abs(offsetY) < 75.0)
                {
                    cursorID = 104;
                }

                if (offsetX > 75.0 && offsetY > 75.0)
                {
                    cursorID = 105;
                }

                if (Math.Abs(offsetX) < 75.0 && offsetY > 75.0)
                {
                    cursorID = 106;
                }

                if (offsetX < -75.0 && offsetY < -75.0)
                {
                    cursorID = 107;
                }

                if (offsetX < -75.0 && Math.Abs(offsetY) < 75.0)
                {
                    cursorID = 108;
                }

                if (offsetX < -75.0 && offsetY < -75.0)
                {
                    cursorID = 109;
                }
            }

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, cursorID);
            });
        }
    }
}
