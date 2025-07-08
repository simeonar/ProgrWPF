using ProgrWPF.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProgrWPF.UI
{
    public class LeftPanelFactory
    {
        private TreeView cmmPointTreeHierarchy;
        private TreeView cmmPointTreeByType;
        private DataGrid propertyGrid;
        private ObservableCollection<PropertyItem> propertyItems;

        public Border CreateLeftPanel()
        {
            var leftPanel = new Border
            {
                Background = new SolidColorBrush(Colors.WhiteSmoke),
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(0, 0, 1, 0), // Right border to separate from center
            };

            var leftPanelTabControl = new TabControl
            {
                TabStripPlacement = Dock.Left,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };

            leftPanelTabControl.Items.Add(CreateVerticalTabItem("Explorer"));
            leftPanelTabControl.Items.Add(CreateVerticalTabItem("Search"));
            leftPanelTabControl.Items.Add(CreateVerticalTabItem("History"));

            // Apply a consistent style to the TabItems
            var tabItemStyle = new Style(typeof(TabItem));
            tabItemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            tabItemStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Colors.LightGray)));
            tabItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Colors.WhiteSmoke)));
            tabItemStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, -1, 0))); // Overlap borders

            var selectedBorder = new SolidColorBrush(Colors.DodgerBlue);
            selectedBorder.Freeze();
            var selectedBackground = new SolidColorBrush(Colors.White);
            selectedBackground.Freeze();

            var trigger = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
            trigger.Setters.Add(new Setter(Control.BorderBrushProperty, selectedBorder));
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, selectedBackground));
            tabItemStyle.Triggers.Add(trigger);

            leftPanelTabControl.Resources.Add(typeof(TabItem), tabItemStyle);

            leftPanel.Child = leftPanelTabControl;

            return leftPanel;
        }

        private TabItem CreateVerticalTabItem(string header)
        {
            var tabItem = new TabItem();

            // Rotated TextBlock for vertical header
            var textBlock = new TextBlock
            {
                Text = header,
                TextAlignment = TextAlignment.Center,
                LayoutTransform = new RotateTransform(-90)
            };

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerPanel.Children.Add(textBlock);

            tabItem.Header = headerPanel;

            // Content for the tab
            if (header == "Explorer")
            {
                tabItem.Content = CreateExplorerContent();
            }
            else
            {
                tabItem.Content = new TextBlock { Text = $"Content for {header}", Margin = new Thickness(10) };
            }

            return tabItem;
        }

        private UIElement CreateExplorerContent()
        {
            var mainExplorerGrid = new Grid();
            mainExplorerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainExplorerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) }); // Splitter
            mainExplorerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- Top Part: Tree View with Tabs ---
            cmmPointTreeHierarchy = CreateNewTreeView();
            cmmPointTreeByType = CreateNewTreeView();

            var treeViewTabs = new TabControl { TabStripPlacement = Dock.Bottom };
            treeViewTabs.Items.Add(new TabItem { Header = "Hierarchy", Content = cmmPointTreeHierarchy });
            treeViewTabs.Items.Add(new TabItem { Header = "By Type", Content = cmmPointTreeByType });
            treeViewTabs.Items.Add(new TabItem { Header = "Issues", Content = new TextBlock { Text = "No issues found.", Margin = new Thickness(5) } });

            Grid.SetRow(treeViewTabs, 0);
            mainExplorerGrid.Children.Add(treeViewTabs);

            // --- Splitter ---
            var splitter = new GridSplitter
            {
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.LightGray,
                ResizeDirection = GridResizeDirection.Rows
            };
            Grid.SetRow(splitter, 1);
            mainExplorerGrid.Children.Add(splitter);

            // --- Bottom Part: Property Grid with Tabs ---
            propertyItems = new ObservableCollection<PropertyItem>();
            propertyGrid = new DataGrid
            {
                ItemsSource = propertyItems,
                AutoGenerateColumns = true,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                HeadersVisibility = System.Windows.Controls.DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                Background = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            var propertyGridTabs = new TabControl { TabStripPlacement = Dock.Bottom };
            propertyGridTabs.Items.Add(new TabItem { Header = "Properties", Content = propertyGrid });
            propertyGridTabs.Items.Add(new TabItem { Header = "Metadata", Content = new TextBlock { Text = "No metadata available.", Margin = new Thickness(5) } });
            propertyGridTabs.Items.Add(new TabItem { Header = "Tolerances", Content = new TextBlock { Text = "No tolerances defined.", Margin = new Thickness(5) } });

            Grid.SetRow(propertyGridTabs, 2);
            mainExplorerGrid.Children.Add(propertyGridTabs);

            return mainExplorerGrid;
        }

        private TreeView CreateNewTreeView()
        {
            var treeView = new TreeView
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.White
            };
            treeView.SelectedItemChanged += CmmPointTree_SelectedItemChanged;
            return treeView;
        }

        public void UpdateCmmPoints(List<CmmPoint> points)
        {
            // 1. Populate the standard hierarchy view
            cmmPointTreeHierarchy.Items.Clear();
            var rootNodeHierarchy = new TreeViewItem { Header = "All Points", IsExpanded = true };
            cmmPointTreeHierarchy.Items.Add(rootNodeHierarchy);
            foreach (var point in points)
            {
                rootNodeHierarchy.Items.Add(CreatePointNode(point));
            }

            // 2. Populate the grouped-by-type view
            cmmPointTreeByType.Items.Clear();
            var rootNodeByType = new TreeViewItem { Header = "Points by Feature", IsExpanded = true };
            cmmPointTreeByType.Items.Add(rootNodeByType);
            var groupedPoints = points.GroupBy(p => p.FeatureType).OrderBy(g => g.Key);

            foreach (var group in groupedPoints)
            {
                var groupNode = new TreeViewItem { Header = group.Key, IsExpanded = true };
                foreach (var point in group)
                {
                    groupNode.Items.Add(CreatePointNode(point));
                }
                rootNodeByType.Items.Add(groupNode);
            }
        }

        private TreeViewItem CreatePointNode(CmmPoint point)
        {
            return new TreeViewItem
            {
                Header = point.Name,
                Tag = point // Store the full CmmPoint object
            };
        }

        private void CmmPointTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem && selectedItem.Tag is CmmPoint point)
            {
                DisplayPointProperties(point);
            }
        }

        private void DisplayPointProperties(CmmPoint point)
        {
            propertyItems.Clear();
            propertyItems.Add(new PropertyItem { Name = "Name", Value = point.Name });
            propertyItems.Add(new PropertyItem { Name = "X", Value = point.X.ToString() });
            propertyItems.Add(new PropertyItem { Name = "Y", Value = point.Y.ToString() });
            propertyItems.Add(new PropertyItem { Name = "Z", Value = point.Z.ToString() });
            propertyItems.Add(new PropertyItem { Name = "Feature Type", Value = point.FeatureType });
            propertyItems.Add(new PropertyItem { Name = "Description", Value = point.FeatureDescription });
        }
    }
}
