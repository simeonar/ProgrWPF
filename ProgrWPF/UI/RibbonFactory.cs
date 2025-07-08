using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Required for Cursors
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProgrWPF.UI
{
    // A class to hold the created ribbon components that MainWindow needs to interact with.
    public class RibbonComponents
    {
        public Border RibbonContainer { get; private set; }
        public Button ToggleButton { get; private set; }
        public TabControl RibbonTabControl { get; private set; }
        public CheckBox LeftPanelCheckBox { get; private set; }
        public CheckBox RightPanelCheckBox { get; private set; }

        public RibbonComponents(Border ribbonContainer, Button toggleButton, TabControl ribbonTabControl, CheckBox leftPanelCheckBox, CheckBox rightPanelCheckBox)
        {
            RibbonContainer = ribbonContainer;
            ToggleButton = toggleButton;
            RibbonTabControl = ribbonTabControl;
            LeftPanelCheckBox = leftPanelCheckBox;
            RightPanelCheckBox = rightPanelCheckBox;
        }
    }

    public class RibbonFactory
    {
        public event EventHandler ModelLoadRequested;

        private CheckBox leftPanelCheckBox;
        private CheckBox rightPanelCheckBox;

        public RibbonComponents CreateRibbon(double initialHeight)
        {
            var ribbonContainer = new Border
            {
                Background = new SolidColorBrush(Colors.WhiteSmoke),
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Height = initialHeight,
                VerticalAlignment = VerticalAlignment.Top
            };

            var ribbonGrid = new Grid();
            ribbonContainer.Child = ribbonGrid;

            var ribbonTabControl = new TabControl
            {
                Padding = new Thickness(5),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            // Create tabs
            ribbonTabControl.Items.Add(CreateTabItem("Home", new Dictionary<string, string[]> 
            {
                { "Rules", new[] { "Manage rules", "Create new" } },
                { "Model", new[] { "Edit model", "View details" } },
                { "Part", new[] { "Select part", "Analyze" } }
            }));
          ribbonTabControl.Items.Add(CreateTabItem("View", null)); // Special content

            ribbonGrid.Children.Add(ribbonTabControl);

            // Collapse button
            var toggleButton = new Button
            {
                Content = "▲",
                FontWeight = FontWeights.Bold,
                Width = 25,
                Height = 25,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 5, 0),
                ToolTip = "Collapse/Expand Menu"
            };
            ribbonGrid.Children.Add(toggleButton);

            // The CheckBoxes are initialized within CreateTabItem, so they won't be null.
            return new RibbonComponents(ribbonContainer, toggleButton, ribbonTabControl, leftPanelCheckBox, rightPanelCheckBox);
        }

        private TabItem CreateTabItem(string header, Dictionary<string, string[]> buttons)
        {
            var tabItem = new TabItem { Header = header };
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            if (buttons != null)
            {
                foreach (var buttonInfo in buttons)
                {
                    panel.Children.Add(CreateRibbonSection(buttonInfo.Key, buttonInfo.Value));
                }
            }
            else if (header == "View")
            {
                // Special logic for the "View" tab
                leftPanelCheckBox = new CheckBox
                {
                    Content = "Left Panel",
                    IsChecked = true,
                    Margin = new Thickness(10),
                    VerticalAlignment = VerticalAlignment.Center
                };

                rightPanelCheckBox = new CheckBox
                {
                    Content = "Right Panel",
                    IsChecked = true,
                    Margin = new Thickness(10),
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(leftPanelCheckBox);
                panel.Children.Add(rightPanelCheckBox);
            }

            tabItem.Content = panel;
            return tabItem;
        }

        private Border CreateRibbonSection(string text, string[] descriptions)
        {
            var section = new Border
            {
                Width = 80,
                Padding = new Thickness(5),
                Cursor = Cursors.Hand,
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(0, 0, 1, 0), // Right separator
                Background = Brushes.Transparent
            };

            var stackPanel = new StackPanel();

            // Placeholder for an icon
            var iconPlaceholder = new Ellipse
            {
                Width = 32,
                Height = 32,
                Fill = new SolidColorBrush(Colors.DodgerBlue),
                Margin = new Thickness(0, 5, 0, 5)
            };

            var textStackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            textStackPanel.Children.Add(new TextBlock { Text = text, TextAlignment = TextAlignment.Center, FontSize = 11 });
            foreach (var desc in descriptions)
            {
                textStackPanel.Children.Add(new TextBlock { Text = desc, TextAlignment = TextAlignment.Center, FontSize = 9, Foreground = Brushes.Gray });
            }

            stackPanel.Children.Add(iconPlaceholder);
            stackPanel.Children.Add(textStackPanel);
            section.Child = stackPanel;

            // Hover effects
            var hoverBackground = new SolidColorBrush(Color.FromRgb(229, 243, 255)); // Light blue, Office-like
            hoverBackground.Freeze(); // Performance improvement
            section.MouseEnter += (s, e) => section.Background = hoverBackground;
            section.MouseLeave += (s, e) => section.Background = Brushes.Transparent;

            // Click handler
            if (text == "Model")
            {
                section.MouseLeftButtonUp += (s, e) => ModelLoadRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                section.MouseLeftButtonUp += (s, e) => MessageBox.Show($"Section '{text}' clicked!");
            }

            return section;
        }
    }
}
