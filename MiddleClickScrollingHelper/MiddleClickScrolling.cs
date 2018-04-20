using System;
using System.Diagnostics;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

namespace MiddleClickScrollingHelper
{
    public class MiddleClickScrolling
    {
        private static double _threshold = 75.0;
        private static bool _isPressed = false;
        private static bool _isMoved = false;
        private static Point? _startPosition = null;
        private static bool _isDeferredMovingStarted = false;
        private static double _factor = 4;
        private static Point? _currentPosition = null;
        private static Timer _timer;
        private static ScrollViewer _scrollViewer;
        private static uint _oldCursorID = 100;
        private static Slider _sliderVertical;
        private static Slider _sliderHorizontal;
        private static Storyboard _verticalStoryboard;
        private static Storyboard _horizontalStoryboard;
        private static DoubleAnimation _verticalDoubleAnimation = null;
        private static DoubleAnimation _horizontalDoubleAnimation = null;

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
            _startPosition = null;
            _isDeferredMovingStarted = false;
            _currentPosition = null;
            _timer = new Timer(Scroll, null, 100, 100);

            _sliderVertical = new Slider()
            {
                SmallChange = 0.0000000001,
                Minimum = double.MinValue,
                Maximum = double.MaxValue,
                StepFrequency = 0.0000000001
            };

            _sliderVertical.ValueChanged -= OnVerticalOffsetChanged;
            _sliderVertical.ValueChanged += OnVerticalOffsetChanged;

            _verticalStoryboard = new Storyboard();

            _verticalDoubleAnimation = new DoubleAnimation()
            {
                EnableDependentAnimation = true,
                Duration = new TimeSpan(0, 0, 1)
            };

            Storyboard.SetTarget(_verticalStoryboard, _sliderVertical);
            Storyboard.SetTargetProperty(_verticalDoubleAnimation, "Value");
            _verticalStoryboard.Children.Add(_verticalDoubleAnimation);

            _sliderHorizontal = new Slider()
            {
                SmallChange = 0.0000000001,
                Minimum = double.MinValue,
                Maximum = double.MaxValue,
                StepFrequency = 0.0000000001
            };

            _sliderHorizontal.ValueChanged -= OnHorizontalOffsetChanged;
            _sliderHorizontal.ValueChanged += OnHorizontalOffsetChanged;

            _horizontalStoryboard = new Storyboard();

            _horizontalDoubleAnimation = new DoubleAnimation()
            {
                EnableDependentAnimation = true,
                Duration = new TimeSpan(0, 0, 1)
            };

            Storyboard.SetTarget(_horizontalStoryboard, _sliderHorizontal);
            Storyboard.SetTargetProperty(_horizontalDoubleAnimation, "Value");
            _horizontalStoryboard.Children.Add(_horizontalDoubleAnimation);

            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
        }

        private static void OnVerticalOffsetChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
#if(DEBUG)
            Debug.WriteLine("NewValue - " + e.NewValue);
#endif
            _scrollViewer?.ChangeView(null, e.NewValue, null, true);
        }

        private static void OnHorizontalOffsetChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _scrollViewer?.ChangeView(e.NewValue, null, null, true);
        }

        private static void UnsubscribeMiddleClickScrolling()
        {
            _isPressed = false;
            _isMoved = false;
            _startPosition = null;
            _currentPosition = null;
            _isDeferredMovingStarted = false;
            _timer.Dispose();

            _sliderVertical.ValueChanged -= OnVerticalOffsetChanged;
            _sliderHorizontal.ValueChanged -= OnHorizontalOffsetChanged;

            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private static void Scroll(object state)
        {
            if (_verticalDoubleAnimation == null || _horizontalDoubleAnimation == null)
            {
                return;
            }

            var offsetX = _currentPosition.Value.X - _startPosition.Value.X;
            var offsetY = _currentPosition.Value.Y - _startPosition.Value.Y;

            SetCursorType(offsetX, offsetY);

            if (Math.Abs(offsetX) > _threshold || Math.Abs(offsetY) > _threshold)
            {
                offsetX = Math.Abs(offsetX) < _threshold ? 0 : offsetX;
                offsetY = Math.Abs(offsetY) < _threshold ? 0 : offsetY;

                offsetX *= _factor;
                offsetY *= _factor;

#if (DEBUG)
                Debug.WriteLine("scrollViewer.HorizontalOffset - " + _scrollViewer.HorizontalOffset);
                Debug.WriteLine("scrollViewer.HorizontalOffset + offsetX - " + (_scrollViewer.HorizontalOffset + offsetX));
                Debug.WriteLine("scrollViewer.VerticalOffset - " + _scrollViewer.VerticalOffset);
                Debug.WriteLine("scrollViewer.VerticalOffset + offsetY - " + (_scrollViewer.VerticalOffset + offsetY));
#endif

                RunInUIThread(() =>
                {
                    _horizontalDoubleAnimation.From = _scrollViewer.HorizontalOffset;
                    _horizontalDoubleAnimation.To = _scrollViewer.HorizontalOffset + offsetX;

                    _verticalDoubleAnimation.From = _scrollViewer.VerticalOffset;
                    _verticalDoubleAnimation.To = _scrollViewer.VerticalOffset + offsetY;

                    _verticalStoryboard.Begin();
                    _horizontalStoryboard.Begin();
                });
            }
            else
            {
                RunInUIThread(() =>
                {
                    _verticalStoryboard.Stop();
                    _horizontalStoryboard.Stop();

                    _sliderVertical.Value = _scrollViewer.VerticalOffset;
                    _sliderHorizontal.Value = _scrollViewer.HorizontalOffset;
                });
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
                _scrollViewer = sender as ScrollViewer;

                PointerPoint pointerPoint = e.GetCurrentPoint(_scrollViewer);

                if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    SubscribeMiddleClickScrolling();

                    _startPosition = Window.Current.CoreWindow.PointerPosition;
                    _currentPosition = Window.Current.CoreWindow.PointerPosition;

                    Debug.WriteLine("startPosition - " + _startPosition);
                }
            }
        }

        private static void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            if (_isPressed && !_isMoved)
            {
                PointerPoint pointerPoint = args.CurrentPoint;

                if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    _currentPosition = Window.Current.CoreWindow.PointerPosition;
                    Debug.WriteLine("currentPosition - " + _currentPosition);

                    var offsetX = _currentPosition.Value.X - _startPosition.Value.X;
                    var offsetY = _currentPosition.Value.Y - _startPosition.Value.Y;

                    if (Math.Abs(offsetX) > _threshold || Math.Abs(offsetY) > _threshold)
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
                SetCursorType(0, 0);
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

        private static void SetCursorType(double offsetX, double offsetY)
        {
            uint cursorID  =  101;

            if (Math.Abs(offsetX) < _threshold && Math.Abs(offsetY) < _threshold)
            {
                cursorID = 101;
            }
            else
            {
                if (Math.Abs(offsetX) < _threshold && offsetY < -_threshold)
                {
                    cursorID = 102;
                }

                if (offsetX > _threshold && offsetY < -_threshold)
                {
                    cursorID = 103;
                }

                if (offsetX > _threshold && Math.Abs(offsetY) < _threshold)
                {
                    cursorID = 104;
                }

                if (offsetX > _threshold && offsetY > _threshold)
                {
                    cursorID = 105;
                }

                if (Math.Abs(offsetX) < _threshold && offsetY > _threshold)
                {
                    cursorID = 106;
                }

                if (offsetX < -_threshold && offsetY > _threshold)
                {
                    cursorID = 107;
                }

                if (offsetX < -_threshold && Math.Abs(offsetY) < _threshold)
                {
                    cursorID = 108;
                }

                if (offsetX < -_threshold && offsetY < -_threshold)
                {
                    cursorID = 109;
                }
            }

            if (_oldCursorID != cursorID)
            {
                _oldCursorID = cursorID;

                RunInUIThread(() =>
                {
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, cursorID);
                });
            }
        }

        private static async void RunInUIThread(Action action)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                action();
            });
        }
    }
}
