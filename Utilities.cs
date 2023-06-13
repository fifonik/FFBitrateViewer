using System.Windows;

namespace Utilities
{

    public class DragDropHighlighter
    {
        public static readonly DependencyProperty IsDroppingAboveProperty = DependencyProperty.RegisterAttached("IsDroppingAbove", typeof(bool), typeof(DragDropHighlighter), new UIPropertyMetadata(false));
        public static readonly DependencyProperty IsDroppingBelowProperty = DependencyProperty.RegisterAttached("IsDroppingBelow", typeof(bool), typeof(DragDropHighlighter), new UIPropertyMetadata(false));


        public static bool GetIsDroppingAbove(DependencyObject source)
        {
            return (bool)source.GetValue(IsDroppingAboveProperty);
        }


        public static void SetIsDroppingAbove(DependencyObject target, bool value)
        {
            target.SetValue(IsDroppingAboveProperty, value);
        }


        public static bool GetIsDroppingBelow(DependencyObject source)
        {
            return (bool)source.GetValue(IsDroppingBelowProperty);
        }


        public static void SetIsDroppingBelow(DependencyObject target, bool value)
        {
            target.SetValue(IsDroppingBelowProperty, value);
        }
    }

}
