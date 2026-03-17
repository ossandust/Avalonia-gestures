// This code is provided "as is", without any warranty.
// Use at your own risk.

using Avalonia;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace OssanDust.CustomGestures
{
    internal class PinchPanGestureRecognizer : GestureRecognizer
    {
        private float _initialDistance;
        private IPointer? _firstContact;
        private Point _firstPoint;
        private IPointer? _secondContact;
        private Point _secondPoint;
        private Point _origin;
        private bool _firstPointCaptured;
        private bool _secondPointCaptured;
        private float _moveThreshold = 10;
        private bool _moving;
        private bool _pinching;
        private bool _pendingPointerPress;
        private bool _emittingPendingPointerPress;
        private bool _blockPinchEnded;
        private PointerPressedEventArgs? _pressedEventArgs;

        public float MoveThreshold
        {
            get => _moveThreshold; 
            set => _moveThreshold = value; 
        }

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (_emittingPendingPointerPress)
            {
                _emittingPendingPointerPress = false;
                _blockPinchEnded = true;
                e.Handled = false;
                return;
            }
            if (_moving)
            {
                e.Handled = true;
                return;
            }
            if (Target is Visual visual && (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
            {
                if (_firstContact == null)
                {
                    _firstContact = e.Pointer;
                    _firstPoint = e.GetPosition(visual);
                    _pendingPointerPress = true;
                    _pressedEventArgs = e;
                    e.Handled = true;
                    return;
                }
                else if (_secondContact == null && _firstContact != e.Pointer)
                {
                    _secondContact = e.Pointer;
                    _secondPoint = e.GetPosition(visual);
                    _pendingPointerPress = false;
                }
                else
                {
                    e.Handled = true;
                    return;
                }

                if (_firstContact != null && _secondContact != null)
                {
                    _pendingPointerPress = false;
                    _initialDistance = GetDistance(_firstPoint, _secondPoint);
                    _origin = new Point((_firstPoint.X + _secondPoint.X) / 2.0f, (_firstPoint.Y + _secondPoint.Y) / 2.0f);
                    _firstPointCaptured = false;
                    _secondPointCaptured = false;
                    Capture(_firstContact);
                    Capture(_secondContact);
                    e.PreventGestureRecognition();
                    e.Handled = true;
                }
            }
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (Target is Visual visual)
            {
                var currentPoint = e.GetPosition(visual);
                if (_firstContact == e.Pointer)
                {
                    if (!_moving && GetDistance(currentPoint, _firstPoint) < _moveThreshold)
                    {
                        e.Handled = true;
                        return;
                    }
                    _firstPoint = currentPoint;
                    _firstPointCaptured = true;
                }
                else if (_secondContact == e.Pointer)
                {
                    if (!_moving && GetDistance(currentPoint, _secondPoint) < _moveThreshold)
                    {
                        e.Handled = true;
                        return;
                    }
                    _secondPoint = currentPoint;
                    _secondPointCaptured = true;
                }
                else
                {
                    return;
                }
                _moving = true;
                _pinching = _firstContact != null && _secondContact != null;
                if (_pinching && _firstPointCaptured && _secondPointCaptured)
                {
                    _firstPointCaptured = false;
                    _secondPointCaptured = false;
                    var distance = GetDistance(_firstPoint, _secondPoint);

                    var scale = distance / _initialDistance;
                    var position = new Point((_firstPoint.X + _secondPoint.X) / 2.0D, (_firstPoint.Y + _secondPoint.Y) / 2.0D);
                    var pinchEventArgs = new PinchPanEventArgs(scale, _origin, position);
                    Target?.RaiseEvent(pinchEventArgs);
                    e.Handled = pinchEventArgs.Handled;
                    e.PreventGestureRecognition();
                }
                else if (_pendingPointerPress)
                {
                    _pendingPointerPress = false;
                    _emittingPendingPointerPress = true;
                    Target?.RaiseEvent(_pressedEventArgs!);
                }
            }
        }

        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            _moving = false;
            if (_pendingPointerPress)
            {
                _pendingPointerPress = false;
                _emittingPendingPointerPress = true;
                Target?.RaiseEvent(_pressedEventArgs!);
            }
            if (RemoveContact(e.Pointer))
            {
                e.PreventGestureRecognition();
            }
        }

        private bool RemoveContact(IPointer pointer)
        {
            if (_firstContact == pointer || _secondContact == pointer)
            {
                if (_secondContact == pointer)
                {
                    _secondContact = null;
                }

                if (_firstContact == pointer)
                {
                    _firstContact = _secondContact;
                    _secondContact = null;
                }
                if (!_blockPinchEnded)
                {
                    Target?.RaiseEvent(new PinchEndedEventArgs());
                }
                if (_firstContact == null && _secondContact == null)
                {
                    _blockPinchEnded = false;
                }
                return true;
            }
            return false;
        }

        protected override void PointerCaptureLost(IPointer pointer)
        {
            RemoveContact(pointer);
        }

        private static float GetDistance(Point a, Point b)
        {
            var length = b - a;
            return (float)new Vector(length.X, length.Y).Length;
        }
    }
	 
    internal class PinchPanEventArgs : RoutedEventArgs
    {
        public PinchPanEventArgs(double scale, Point origin, Point position) : base(Gestures.PinchEvent)
        {
            Scale = scale;
            Origin = origin;
            Position = position;
        }

        public Point Origin { get; }
        public double Scale { get; }
        public Point Position { get; }
    }	 
}
