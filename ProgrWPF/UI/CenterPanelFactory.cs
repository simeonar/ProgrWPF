using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProgrWPF.UI
{
    public class CenterPanelFactory
    {
        public Border CreateCenterPanel()
        {
            var centerPanel = new Border 
            { 
                Background = new SolidColorBrush(Colors.White), 
                Margin = new Thickness(0, 5, 0, 5) 
            };
            centerPanel.Child = new TextBox 
            { 
                Text = "Main workspace...", 
                AcceptsReturn = true, 
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, 
                BorderThickness = new Thickness(0)
            };
            return centerPanel;
        }
    }
}
