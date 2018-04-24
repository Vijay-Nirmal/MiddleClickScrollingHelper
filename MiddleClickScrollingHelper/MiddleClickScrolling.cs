﻿using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
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
        private static double _threshold = 50;
        private static bool _isPressed = false;
        private static bool _isMoved = false;
        private static Point _startPosition;
        private static bool _isDeferredMovingStarted = false;
        private static double _factor = 50;
        private static Point _currentPosition;
        private static Timer _timer;
        private static ScrollViewer _scrollViewer;
        private static uint _oldCursorID = 100;

        /// <summary>
        /// Attached <see cref="DependencyProperty"/> for enabling middle click scrolling
        /// </summary>
        public static readonly DependencyProperty EnableMiddleClickScrollingProperty =
            DependencyProperty.RegisterAttached("EnableMiddleClickScrolling", typeof(bool), typeof(MiddleClickScrolling), new PropertyMetadata(false, OnEnableMiddleClickScrollingChanged));

        /// <summary>
        /// Get <see cref="EnableMiddleClickScrollingProperty"/>. Returns `true` if middle click scrolling is enabled else retuen `false`
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool GetEnableMiddleClickScrolling(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableMiddleClickScrollingProperty);
        }

        /// <summary>
        /// Set <see cref="EnableMiddleClickScrollingProperty"/>. `true` to enable middle click scrolling
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetEnableMiddleClickScrolling(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableMiddleClickScrollingProperty, value);
        }

        /// <summary>
        /// Function will be called when <see cref="EnableMiddleClickScrollingProperty"/> is updated
        /// </summary>
        /// <param name="d">Holds the dependency object</param>
        /// <param name="e">Holds the dependency object args</param>
        private static void OnEnableMiddleClickScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                _scrollViewer = scrollViewer;
            }
            else
            {
                _scrollViewer = (d as FrameworkElement).FindDescendant<ScrollViewer>();

                if (_scrollViewer == null)
                {
                    (d as FrameworkElement).Loaded += (sender, arg) =>
                    {
                        _scrollViewer = (sender as FrameworkElement).FindDescendant<ScrollViewer>();

                        if (_scrollViewer != null)
                        {
                            UpdateChange((bool)e.NewValue);
                        }
                    };
                }
            }

            if (_scrollViewer == null)
            {
                return;
            }

            UpdateChange((bool)e.NewValue);
        }

        /// <summary>
        /// Function to update changes in <see cref="EnableMiddleClickScrollingProperty"/>
        /// </summary>
        /// <param name="newValue">New value from the <see cref="EnableMiddleClickScrollingProperty"/></param>
        private static void UpdateChange(bool newValue)
        {
            if (newValue)
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

        /// <summary>
        /// Function to set default value and subscribe to events
        /// </summary>
        private static void SubscribeMiddleClickScrolling()
        {
            _isPressed = true;
            _isMoved = false;
            _startPosition = default(Point);
            _isDeferredMovingStarted = false;
            _currentPosition = default(Point);
            _timer = new Timer(Scroll, null, 5, 5);

            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
        }

        /// <summary>
        /// Function to set default value and unsubscribe to events
        /// </summary>
        private static void UnsubscribeMiddleClickScrolling()
        {
            _isPressed = false;
            _isMoved = false;
            _startPosition = default(Point);
            _currentPosition = default(Point);
            _isDeferredMovingStarted = false;
            _timer.Dispose();

            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        /// <summary>
        /// This function will be called for every small intervel by <see cref="Timer"/>
        /// </summary>
        /// <param name="state">Default param for <see cref="Timer"/>. In this function it will be `null`</param>
        private static void Scroll(object state)
        {
            var offsetX = _currentPosition.X - _startPosition.X;
            var offsetY = _currentPosition.Y - _startPosition.Y;

            SetCursorType(offsetX, offsetY);

            if (Math.Abs(offsetX) > _threshold || Math.Abs(offsetY) > _threshold)
            {
                offsetX = Math.Abs(offsetX) < _threshold ? 0 : offsetX;
                offsetY = Math.Abs(offsetY) < _threshold ? 0 : offsetY;

                offsetX /= _factor;
                offsetY /= _factor;

                offsetX = offsetX > 0 ? Math.Pow(offsetX, 2) : -Math.Pow(offsetX, 2);
                offsetY = offsetY > 0 ? Math.Pow(offsetY, 2) : -Math.Pow(offsetY, 2);

                offsetX = offsetX > 100 ? 100 : offsetX;
                offsetY = offsetY > 100 ? 100 : offsetY;

                RunInUIThread(() =>
                {
                    _scrollViewer?.ChangeView(_scrollViewer.HorizontalOffset + offsetX, _scrollViewer.VerticalOffset + offsetY, null, true);
                });
            }
        }

        /// <summary>
        /// Function to check the status of scrolling
        /// </summary>
        /// <returns>Return true if the scrolling is started</returns>
        private static bool CanScroll()
        {
            return _isDeferredMovingStarted || (_isPressed && !_isDeferredMovingStarted);
        }

        private static void ScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Unsubscribe if deferred moving is started
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

                // SubscribeMiddle if middle button is pressed
                if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    SubscribeMiddleClickScrolling();

                    _startPosition = Window.Current.CoreWindow.PointerPosition;
                    _currentPosition = Window.Current.CoreWindow.PointerPosition;
                }
            }
        }

        private static void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            // If condution that occures before scrolling begins
            if (_isPressed && !_isMoved)
            {
                PointerPoint pointerPoint = args.CurrentPoint;

                if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    _currentPosition = Window.Current.CoreWindow.PointerPosition;

                    var offsetX = _currentPosition.X - _startPosition.X;
                    var offsetY = _currentPosition.Y - _startPosition.Y;

                    // Settign _isMoved if pointer goes out of threshold value
                    if (Math.Abs(offsetX) > _threshold || Math.Abs(offsetY) > _threshold)
                    {
                        _isMoved = true;
                    }
                }
            }

            // Update current position of the pointer if scrolling started
            if (CanScroll())
            {
                _currentPosition = Window.Current.CoreWindow.PointerPosition;
            }
        }

        private static void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            // Start deferred moving if the pointer is pressed and not moved
            if (_isPressed && !_isMoved)
            {
                _isDeferredMovingStarted = true;

                // Event to stop deferred scrolling if pointer exited
                Window.Current.CoreWindow.PointerExited -= CoreWindow_PointerExited;
                Window.Current.CoreWindow.PointerExited += CoreWindow_PointerExited;

                SetCursorType(0, 0);
            }
            else
            {
                _isDeferredMovingStarted = false;
            }

            // Unsubscribe if the pointer is pressed and not DeferredMoving
            if (_isPressed && !_isDeferredMovingStarted)
            {
                UnsubscribeMiddleClickScrolling();
            }
        }

        private static void CoreWindow_PointerExited(CoreWindow sender, PointerEventArgs args)
        {
            Window.Current.CoreWindow.PointerExited -= CoreWindow_PointerExited;
            UnsubscribeMiddleClickScrolling();
        }

        /// <summary>
        /// Change cursor type depend upon offset from starting position
        /// </summary>
        /// <param name="offsetX">Horizontal offset from starting position</param>
        /// <param name="offsetY">Vertical offset from starting position</param>
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

        /// <summary>
        /// Run the give input action in UIThread
        /// </summary>
        /// <param name="action">Action to be run on UIThread</param>
        private static async void RunInUIThread(Action action)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                action();
            });
        }
    }
}
