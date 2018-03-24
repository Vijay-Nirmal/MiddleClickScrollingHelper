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
        private static bool _isPressed = false;
        private static bool _isMoved = false;
        private static Point? _startPosition = null;
        private static bool _isDeferredMovingStarted = false;
        private static double _slowdown = 4;
        private static Point? _currentPosition = null;
        private static Timer _timer;
        private static ScrollViewer _scrollViewer;
        private static uint _oldCursorID = 100;
        
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
            _scrollViewer = d as ScrollViewer;

            if (_scrollViewer == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                _scrollViewer.PointerPressed -= ScrollViewer_PointerPressed;
                _scrollViewer.PointerPressed += ScrollViewer_PointerPressed;
            }
            else
            {
                _scrollViewer.PointerPressed -= ScrollViewer_PointerPressed;
                UnsubscribeMiddleClickScrolling();
            }
        }

        private static void SubscribeMiddleClickScrolling()
        {
            _isPressed = true;
            _isMoved = false;
            _isDeferredMovingStarted = false;
            _timer = new Timer(ScrollAsync, null, 50, 50);

            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
        }

        private static void UnsubscribeMiddleClickScrolling()
        {
            _isPressed = false;
            _isMoved = false;
            _startPosition = null;
            _currentPosition = null;
            _isDeferredMovingStarted = false;

            _timer.Dispose();

            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private static async void ScrollAsync(object state)
        {

            if (CanScroll())
            {
                var offsetX = _currentPosition.Value.X - _startPosition.Value.X;
                var offsetY = _currentPosition.Value.Y - _startPosition.Value.Y;

                SetCursorType(offsetX, offsetY);

                if (Math.Abs(offsetX) > 75.0 || Math.Abs(offsetY) > 75.0)
                {
                    offsetX = Math.Abs(offsetX) < 75.0 ? 0 : offsetX;
                    offsetY = Math.Abs(offsetY) < 75.0 ? 0 : offsetY;

                    offsetX /= _slowdown;
                    offsetY /= _slowdown;

                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        _scrollViewer.ChangeView(_scrollViewer.HorizontalOffset + offsetX, _scrollViewer.VerticalOffset + offsetY, null);
                    });
                }
            }
        }

        private static bool CanScroll()
        {
            return _isDeferredMovingStarted || (_isPressed && !_isDeferredMovingStarted);
        }

        private static void ScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_isDeferredMovingStarted)
            {
                UnsubscribeMiddleClickScrolling();
                return;
            }

            Pointer pointer = e.Pointer;

            if (pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                PointerPoint pointerPoint = e.GetCurrentPoint(_scrollViewer);

                if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    _startPosition = Window.Current.CoreWindow.PointerPosition;
                    _currentPosition = Window.Current.CoreWindow.PointerPosition;

                    Debug.WriteLine("startPosition - " + _startPosition);

                    SubscribeMiddleClickScrolling();
                }
            }
        }

        static int i = 0;

        private static void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
#if(DEBUG)
            Debug.WriteLine(i++);
#endif

            if (_isPressed && !_isMoved)
            {
                PointerPoint pointerPoint = args.CurrentPoint;

                if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    _currentPosition = Window.Current.CoreWindow.PointerPosition;
                    Debug.WriteLine("currentPosition - " + _currentPosition);

                    var offsetX = _currentPosition.Value.X - _startPosition.Value.X;
                    var offsetY = _currentPosition.Value.Y - _startPosition.Value.Y;

                    if (Math.Abs(offsetX) > 75.0 || Math.Abs(offsetY) > 75.0)
                    {
                        _isMoved = true;
                    }
                }
            }

            if (CanScroll())
            {
                _currentPosition = Window.Current.CoreWindow.PointerPosition;
            }
        }

        private static void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            if (_isPressed && !_isMoved)
            {
                _isDeferredMovingStarted = true;
            }
            else
            {
                _isDeferredMovingStarted = false;
            }

            if (_isPressed && !_isDeferredMovingStarted)
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

                if (offsetX < -75.0 && offsetY > 75.0)
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

            if (_oldCursorID != cursorID)
            {
                _oldCursorID = cursorID;
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, cursorID);
                });
            }
        }
    }
}
