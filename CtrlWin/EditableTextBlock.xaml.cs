using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace CtrlWin
{
    public partial class EditableTextBlock : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(EditableTextBlock), new FrameworkPropertyMetadata(null));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public EditableTextBlock()
        {
            InitializeComponent();
        }

        private void TextBlockElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                TextBlockElement.Visibility = Visibility.Collapsed;
                TextBoxElement.Visibility = Visibility.Visible;
                TextBoxElement.Focus();
                TextBoxElement.SelectAll();
            }
        }

        private void TextBoxElement_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxElement.Visibility = Visibility.Collapsed;
            TextBlockElement.Visibility = Visibility.Visible;
            RaiseEvent(new RoutedEventArgs(TextChangedEvent));
        }

        private void TextBoxElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                TextBoxElement.Visibility = Visibility.Collapsed;
                TextBlockElement.Visibility = Visibility.Visible;
                RaiseEvent(new RoutedEventArgs(TextChangedEvent));
            }
            else if (e.Key == Key.Escape)
            {
                TextBoxElement.Visibility = Visibility.Collapsed;
                TextBlockElement.Visibility = Visibility.Visible;
            }
        }

        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent(
            "TextChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EditableTextBlock));

        public event RoutedEventHandler TextChanged
        {
            add { AddHandler(TextChangedEvent, value); }
            remove { RemoveHandler(TextChangedEvent, value); }
        }
    }
}
