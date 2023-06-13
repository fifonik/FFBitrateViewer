using System.Windows;


namespace FFBitrateViewer
{
    // Cannot bind to VM from within GridView so use this technique
    // https://stackoverflow.com/questions/15494226/cannot-find-source-for-binding-with-reference-relativesource-findancestor
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

    }
}
