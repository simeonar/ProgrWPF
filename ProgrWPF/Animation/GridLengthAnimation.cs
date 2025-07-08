using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace ProgrWPF
{
    // Helper class for GridLength animation
    public class GridLengthAnimation : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);

        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));

        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromVal = From.Value;
            double toVal = To.Value;
            double progress = animationClock.CurrentProgress ?? 0.0;

            if (fromVal > toVal)
                return new GridLength((1 - progress) * (fromVal - toVal) + toVal, From.IsStar ? GridUnitType.Star : GridUnitType.Pixel);
            else
                return new GridLength(progress * (toVal - fromVal) + fromVal, To.IsStar ? GridUnitType.Star : GridUnitType.Pixel);
        }
    }
}
