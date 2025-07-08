using ProgrWPF.Data;
using ProgrWPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes; // Added for Ellipse
using Microsoft.Win32; // For SaveFileDialog
using System.IO;       // For File operations

namespace ProgrWPF.UI
{
    public class RightPanelFactory
    {
        public event EventHandler<MeasurementResult> SelectionChanged;
        public event EventHandler<(MeasurementResult result, double progress)> MeasurementProgressChanged;

        private TreeView resultsTreeView;
        private ObservableCollection<MeasurementResult> measurementResults;
        private readonly ReportService reportService;
        private readonly MeasurementService measurementService;

        public RightPanelFactory()
        {
            reportService = new ReportService();
            measurementService = new MeasurementService();

            // Subscribe to MeasurementService events to update the UI
            measurementService.PointMeasurementStarted += (result) => result.UpdateDetails();
            measurementService.PointMeasurementCompleted += (result) => result.UpdateDetails();
            measurementService.MeasurementReset += (results) => {
                foreach(var result in results)
                {
                    result.UpdateDetails();
                }
            };

            // Forward the progress event
            measurementService.PointMeasurementProgress += (args) => MeasurementProgressChanged?.Invoke(this, args);
        }

        public System.Windows.Controls.Border CreateRightPanel()
        {
            var rightPanel = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Colors.WhiteSmoke),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Colors.LightGray),
                BorderThickness = new Thickness(1, 0, 0, 0) // Left border
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Toolbar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Results View
            rightPanel.Child = mainGrid;

            // 1. Toolbar
            var toolBar = new ToolBar { Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
            toolBar.Items.Add(CreateToolBarButton("▶", "Start Measurement", Start_Click));
            toolBar.Items.Add(CreateToolBarButton("⏸", "Pause Measurement", Pause_Click));
            toolBar.Items.Add(CreateToolBarButton("⏹", "Stop Measurement", Stop_Click));
            toolBar.Items.Add(new Separator());
            toolBar.Items.Add(CreateExportMenuButton()); // Replace the simple button with a menu
            Grid.SetRow(toolBar, 0);
            mainGrid.Children.Add(toolBar);

            // 2. Results TreeView
            measurementResults = new ObservableCollection<MeasurementResult>();
            resultsTreeView = new TreeView
            {
                ItemsSource = measurementResults,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            resultsTreeView.ItemTemplate = CreateResultTemplate();
            resultsTreeView.SelectedItemChanged += ResultsTreeView_SelectedItemChanged;

            Grid.SetRow(resultsTreeView, 1);
            mainGrid.Children.Add(resultsTreeView);

            return rightPanel;
        }

        private FrameworkElement CreateExportMenuButton()
        {
            var exportMenu = new MenuItem
            {
                Header = "Export",
                ToolTip = "Export measurement results",
                FontWeight = FontWeights.Bold
            };

            var pdfMenuItem = new MenuItem { Header = "Export as PDF" };
            pdfMenuItem.Click += ExportPdf_Click;
            exportMenu.Items.Add(pdfMenuItem);

            var csvMenuItem = new MenuItem { Header = "Export as CSV" };
            csvMenuItem.Click += ExportCsv_Click;
            exportMenu.Items.Add(csvMenuItem);

            var menu = new Menu { Background = Brushes.Transparent, Items = { exportMenu } };
            return menu;
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (measurementResults == null || measurementResults.Count == 0)
            {
                MessageBox.Show("No measurement results to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf|All files (*.*)|*.*",
                Title = "Save Measurement Report as PDF",
                FileName = $"MeasurementReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    reportService.ExportToPdf(measurementResults, saveFileDialog.FileName);
                    MessageBox.Show($"Report successfully saved to {saveFileDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save PDF report. Error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (measurementResults == null || measurementResults.Count == 0)
            {
                MessageBox.Show("No measurement results to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Save Measurement Report",
                FileName = $"MeasurementReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    reportService.ExportToCsv(measurementResults, saveFileDialog.FileName);
                    MessageBox.Show($"Report successfully saved to {saveFileDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save report. Error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private Button CreateToolBarButton(string content, string toolTip, RoutedEventHandler clickHandler)
        {
            var button = new Button { Content = content, ToolTip = toolTip, FontWeight = FontWeights.Bold };
            button.Click += clickHandler;
            return button;
        }

        private HierarchicalDataTemplate CreateResultTemplate()
        {
            var template = new HierarchicalDataTemplate(typeof(MeasurementResult))
            {
                ItemsSource = new Binding("Details")
            };

            // Main item layout (StackPanel with Status, Name, and Repeat Button)
            var mainPanel = new FrameworkElementFactory(typeof(DockPanel));

            // Status Ellipse
            var ellipseFactory = new FrameworkElementFactory(typeof(Ellipse));
            ellipseFactory.SetValue(Ellipse.WidthProperty, 12.0);
            ellipseFactory.SetValue(Ellipse.HeightProperty, 12.0);
            ellipseFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 5, 0));
            ellipseFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            // Binding for status color
            var statusBinding = new Binding("Status");
            var greenTrigger = new DataTrigger { Binding = statusBinding, Value = MeasurementStatus.Passed };
            greenTrigger.Setters.Add(new Setter(Shape.FillProperty, Brushes.Green));
            var redTrigger = new DataTrigger { Binding = statusBinding, Value = MeasurementStatus.Failed };
            redTrigger.Setters.Add(new Setter(Shape.FillProperty, Brushes.Red));
            var orangeTrigger = new DataTrigger { Binding = statusBinding, Value = MeasurementStatus.InProgress };
            orangeTrigger.Setters.Add(new Setter(Shape.FillProperty, Brushes.Orange));
            var grayTrigger = new DataTrigger { Binding = statusBinding, Value = MeasurementStatus.NotMeasured };
            grayTrigger.Setters.Add(new Setter(Shape.FillProperty, Brushes.Gray));

            var style = new System.Windows.Style(typeof(Ellipse));
            style.Triggers.Add(greenTrigger);
            style.Triggers.Add(redTrigger);
            style.Triggers.Add(orangeTrigger);
            style.Triggers.Add(grayTrigger);
            ellipseFactory.SetValue(FrameworkElement.StyleProperty, style);
            ellipseFactory.SetValue(DockPanel.DockProperty, Dock.Left); // Correct way to set attached property
            mainPanel.AppendChild(ellipseFactory);

            // Repeat Button
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.ContentProperty, "Repeat");
            buttonFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(5, 0, 0, 0));
            buttonFactory.SetValue(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand);
            buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(Repeat_Click));
            buttonFactory.SetValue(DockPanel.DockProperty, Dock.Right); // Correct way to set attached property
            mainPanel.AppendChild(buttonFactory);

            // Point Name TextBlock
            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, new Binding("PointName"));
            textFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            mainPanel.AppendChild(textFactory);

            template.VisualTree = mainPanel;

            // Sub-item template (for details like Deviation, Tolerance)
            var subItemTemplate = new DataTemplate(typeof(PropertyItem));
            var subItemText = new FrameworkElementFactory(typeof(TextBlock));
            subItemText.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            subItemText.SetValue(FrameworkElement.MarginProperty, new Thickness(22, 0, 0, 0)); // Indent sub-items
            subItemTemplate.VisualTree = subItemText;

            template.ItemTemplate = subItemTemplate;

            return template;
        }

        private void ResultsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is MeasurementResult result)
            {
                SelectionChanged?.Invoke(this, result);
            }
        }

        public void SetPointsToMeasure(List<CmmPoint> points)
        {
            measurementResults.Clear();
            foreach (var point in points)
            {
                var result = new MeasurementResult
                {
                    PointName = point.Name,
                    Status = MeasurementStatus.NotMeasured,
                    ExpectedX = point.X, ExpectedY = point.Y, ExpectedZ = point.Z,
                    Tolerance = 0.1 // Default tolerance
                };
                result.UpdateDetails(); // Initial population of details
                measurementResults.Add(result);
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (measurementResults == null || measurementResults.Count == 0)
            {
                MessageBox.Show("No points loaded to measure.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            measurementService.StartMeasurement(measurementResults);
        }

        private void Pause_Click(object sender, RoutedEventArgs e) => measurementService.PauseMeasurement();

        private void Stop_Click(object sender, RoutedEventArgs e) => measurementService.StopMeasurement();

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MeasurementResult result)
            {
                measurementService.RepeatMeasurement(result);
            }
        }
    }
}
