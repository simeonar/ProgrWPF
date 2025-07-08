using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // For StatusBar
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32; // For OpenFileDialog
using ProgrWPF.Data;   // For data models and loader
using ProgrWPF.UI;     // Using the new UI namespace

namespace ProgrWPF
{
    public partial class MainWindow : Window
    {
        private bool isMenuCollapsed = false;
        private Grid mainGrid;
        private Border ribbonContainer;
        private TabControl ribbonTabControl;
        private Grid contentGrid;
        private Button toggleButton;
        private Border leftPanel;
        private Border rightPanel;
        private ColumnDefinition leftColumn;
        private ColumnDefinition rightColumn;

        private LeftPanelFactory leftPanelFactory; // Make factory accessible
        private RightPanelFactory rightPanelFactory; // Make factory accessible

        // Status Bar controls
        private TextBlock statusPointName;
        private ProgressBar statusProgressBar;
        private TextBlock statusPercentage;

        private double expandedMenuHeight = 130.0;
        private double collapsedMenuHeight = 80.0; 

        public MainWindow()
        {
            Title = "WPF Demo UI";
            Width = 1024;
            Height = 768;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));

            CreateInterface();
        }

        private void CreateInterface()
        {
            // 1. Main grid
            mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(expandedMenuHeight, GridUnitType.Pixel) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // For Status Bar
            Content = mainGrid;

            // 2. Top menu (Ribbon) from Factory
            var ribbonFactory = new RibbonFactory();
            var ribbonComponents = ribbonFactory.CreateRibbon(expandedMenuHeight);

            // Assign components from the factory
            ribbonContainer = ribbonComponents.RibbonContainer;
            toggleButton = ribbonComponents.ToggleButton;
            ribbonTabControl = ribbonComponents.RibbonTabControl;

            // Hook up event handlers from the factory components to MainWindow methods
            toggleButton.Click += ToggleMenu_Click;
            ribbonFactory.ModelLoadRequested += RibbonFactory_ModelLoadRequested; // Hook up the new event
            ribbonComponents.LeftPanelCheckBox.Checked += ToggleLeftPanel;
            ribbonComponents.LeftPanelCheckBox.Unchecked += ToggleLeftPanel;
            ribbonComponents.RightPanelCheckBox.Checked += ToggleRightPanel;
            ribbonComponents.RightPanelCheckBox.Unchecked += ToggleRightPanel;

            Grid.SetRow(ribbonContainer, 0);
            mainGrid.Children.Add(ribbonContainer);

            // 3. Content panels
            CreateContentPanels();
            Grid.SetRow(contentGrid, 1);
            mainGrid.Children.Add(contentGrid);

            // 4. Status Bar
            CreateStatusBar();
            Grid.SetRow(mainGrid.Children[mainGrid.Children.Count - 1], 2);
        }

        // CreateRibbon, CreateTabItem, and CreateRibbonButton are now removed, as they are in RibbonFactory.

        private void CreateContentPanels()
        {
            contentGrid = new Grid();

            leftColumn = new ColumnDefinition { Width = new GridLength(250) };
            rightColumn = new ColumnDefinition { Width = new GridLength(250) };

            contentGrid.ColumnDefinitions.Add(leftColumn);
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(rightColumn);

            // Left panel from Factory
            leftPanelFactory = new LeftPanelFactory(); // Assign to the field
            leftPanel = leftPanelFactory.CreateLeftPanel();
            Grid.SetColumn(leftPanel, 0);

            // Center panel from Factory
            var centerPanelFactory = new CenterPanelFactory();
            var centerPanel = centerPanelFactory.CreateCenterPanel();
            Grid.SetColumn(centerPanel, 1);

            // Right panel from Factory
            rightPanelFactory = new RightPanelFactory(); // Assign to the field
            rightPanel = rightPanelFactory.CreateRightPanel();
            rightPanelFactory.SelectionChanged += RightPanel_SelectionChanged;
            rightPanelFactory.MeasurementProgressChanged += RightPanel_MeasurementProgressChanged;
            Grid.SetColumn(rightPanel, 2);

            contentGrid.Children.Add(leftPanel);
            contentGrid.Children.Add(centerPanel);
            contentGrid.Children.Add(rightPanel);
        }

        private void CreateStatusBar()
        {
            var statusBar = new StatusBar
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White
            };

            var mainPanel = new DockPanel { Margin = new Thickness(5, 2, 5, 2) };

            statusPointName = new TextBlock { Text = "Ready", VerticalAlignment = VerticalAlignment.Center };
            statusProgressBar = new ProgressBar { Width = 100, Height = 16, Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            statusPercentage = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

            DockPanel.SetDock(statusPointName, Dock.Left);
            mainPanel.Children.Add(statusPointName);

            var progressPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            progressPanel.Children.Add(statusProgressBar);
            progressPanel.Children.Add(statusPercentage);
            mainPanel.Children.Add(progressPanel);

            statusBar.Items.Add(mainPanel);
            mainGrid.Children.Add(statusBar);
        }

        private void RightPanel_SelectionChanged(object sender, MeasurementResult result)
        {
            if (result != null)
            {
                statusPointName.Text = result.PointName;
                statusProgressBar.Value = 0;
                statusPercentage.Text = "";
                if (result.Status == MeasurementStatus.Passed || result.Status == MeasurementStatus.Failed)
                {
                    statusProgressBar.Value = 100;
                    statusPercentage.Text = result.Status.ToString();
                }
            }
            else
            {
                statusPointName.Text = "Ready";
                statusProgressBar.Value = 0;
                statusPercentage.Text = "";
            }
        }

        private void RightPanel_MeasurementProgressChanged(object sender, (MeasurementResult result, double progress) args)
        {
            if (args.result != null)
            {
                statusPointName.Text = $"Measuring: {args.result.PointName}";
                statusProgressBar.Value = args.progress;
                statusPercentage.Text = $"{args.progress:F0}%";

                if (args.progress >= 100)
                {
                    statusPointName.Text = args.result.PointName;
                    statusPercentage.Text = args.result.Status.ToString();
                }
            }
        }

        private void RibbonFactory_ModelLoadRequested(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "3D Models (*.glb)|*.glb|All files (*.*)|*.*",
                Title = "Select a Model File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string modelPath = openFileDialog.FileName;
                // For now, just display the model path in the center panel (placeholder)
                if (contentGrid.Children.Count > 1 && contentGrid.Children[1] is Border centerPanel)
                {
                    if (centerPanel.Child is TextBlock textBlock)
                    {
                        textBlock.Text = $"Model loaded: {Path.GetFileName(modelPath)}";
                    }
                }

                // Load corresponding XML data
                string xmlPath = Path.ChangeExtension(modelPath, ".xml");
                if (File.Exists(xmlPath))
                {
                    var points = CmmDataLoader.LoadPoints(xmlPath);
                    leftPanelFactory.UpdateCmmPoints(points);
                    rightPanelFactory.SetPointsToMeasure(points); // Pass points to the right panel
                }
                else
                {
                    MessageBox.Show($"Could not find the corresponding data file: {Path.GetFileName(xmlPath)}", "Data File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ToggleLeftPanel(object sender, RoutedEventArgs e)
        {
            if (leftColumn != null && sender is CheckBox checkBox)
            {
                leftColumn.Width = checkBox.IsChecked == true ? new GridLength(250) : new GridLength(0);
            }
        }

        private void ToggleRightPanel(object sender, RoutedEventArgs e)
        {
            if (rightColumn != null && sender is CheckBox checkBox)
            {
                rightColumn.Width = checkBox.IsChecked == true ? new GridLength(250) : new GridLength(0);
            }
        }

        private void ToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            isMenuCollapsed = !isMenuCollapsed;

            if (toggleButton != null)
            {
                toggleButton.Content = isMenuCollapsed ? "▼" : "▲";
            }

            var animation = new DoubleAnimation
            {
                To = isMenuCollapsed ? collapsedMenuHeight : expandedMenuHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Animate the container's height
            if (ribbonContainer != null)
            {
                ribbonContainer.BeginAnimation(HeightProperty, animation);
            }
            
            // Animate the Grid row's height
            if (mainGrid != null)
            {
                var rowAnimation = new GridLengthAnimation
                {
                    From = mainGrid.RowDefinitions[0].Height,
                    To = new GridLength(isMenuCollapsed ? collapsedMenuHeight : expandedMenuHeight),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                mainGrid.RowDefinitions[0].BeginAnimation(RowDefinition.HeightProperty, rowAnimation);
            }

            // Hide/show text and resize buttons
            if (ribbonTabControl != null)
            {
                foreach (TabItem tab in ribbonTabControl.Items)
                {
                    if (tab.Content is Panel panel)
                    {
                        foreach (var child in panel.Children) // Use var to avoid type casting errors
                        {
                            if (child is Border section) // Apply logic only to sections (Borders)
                            {
                                if (section.Child is StackPanel sp && sp.Children.Count > 1)
                                {
                                    var icon = sp.Children[0] as FrameworkElement;
                                    var textPanel = sp.Children[1] as FrameworkElement;

                                    if (textPanel != null && icon != null)
                                    {
                                        if (isMenuCollapsed)
                                        {
                                            textPanel.Visibility = Visibility.Collapsed;
                                            icon.Margin = new Thickness(0);
                                            icon.Width = 24;
                                            icon.Height = 24;
                                            section.Width = 40;
                                            section.Height = 40;
                                        }
                                        else
                                        {
                                            textPanel.Visibility = Visibility.Visible;
                                            icon.Margin = new Thickness(0, 5, 0, 5);
                                            icon.Width = 32;
                                            icon.Height = 32;
                                            section.Width = 80; // Restore width
                                            section.Height = Double.NaN; // Restore auto-height
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
