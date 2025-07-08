using ProgrWPF.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes; // Added for Ellipse
using System.Windows.Threading;
using System.Threading.Tasks;

namespace ProgrWPF.UI
{
    public class RightPanelFactory
    {
        public event EventHandler<MeasurementResult> SelectionChanged;
        public event EventHandler<(MeasurementResult result, double progress)> MeasurementProgressChanged;

        private TreeView resultsTreeView;
        private ObservableCollection<MeasurementResult> measurementResults;
        private DispatcherTimer simulationTimer;
        private List<CmmPoint> pointsToMeasure;
        private int currentPointIndex;
        private Random random = new Random(); // Reusable Random instance

        public Border CreateRightPanel()
        {
            var rightPanel = new Border
            {
                Background = new SolidColorBrush(Colors.WhiteSmoke),
                BorderBrush = new SolidColorBrush(Colors.LightGray),
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
            toolBar.Items.Add(CreateToolBarButton("Export", "Export Results", Export_Click));
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

            // Initialize simulation timer
            simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            simulationTimer.Tick += SimulationTimer_Tick;

            return rightPanel;
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
            ellipseFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
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

            var style = new Style(typeof(Ellipse));
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
            textFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
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
            pointsToMeasure = points;
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
            if (pointsToMeasure == null || pointsToMeasure.Count == 0)
            {
                MessageBox.Show("No points loaded to measure.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            currentPointIndex = 0;
            simulationTimer.Start();
        }

        private void Pause_Click(object sender, RoutedEventArgs e) => simulationTimer.Stop();

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            simulationTimer.Stop();
            currentPointIndex = 0;
            // Reset status
            foreach(var result in measurementResults)
            {
                result.Status = MeasurementStatus.NotMeasured;
                result.UpdateDetails();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Export functionality not yet implemented.");

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MeasurementResult result)
            {
                MeasurePoint(result);
            }
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (currentPointIndex >= measurementResults.Count)
            {
                simulationTimer.Stop();
                return;
            }

            var result = measurementResults[currentPointIndex];
            MeasurePoint(result);

            currentPointIndex++;
        }

        private async void MeasurePoint(MeasurementResult result)
        {
            result.Status = MeasurementStatus.InProgress;
            result.UpdateDetails();
            MeasurementProgressChanged?.Invoke(this, (result, 0));

            // Simulate measurement work with progress updates
            for (int i = 1; i <= 10; i++)
            {
                await Task.Delay(100); // Simulate a step of work
                MeasurementProgressChanged?.Invoke(this, (result, i * 10));
            }

            // Simulate measurement result
            result.ActualX = result.ExpectedX + (random.NextDouble() - 0.5) * 0.2;
            result.ActualY = result.ExpectedY + (random.NextDouble() - 0.5) * 0.2;
            result.ActualZ = result.ExpectedZ + (random.NextDouble() - 0.5) * 0.2;
            result.Timestamp = DateTime.Now;

            result.Deviation = Math.Sqrt(
                Math.Pow(result.ActualX - result.ExpectedX, 2) +
                Math.Pow(result.ActualY - result.ExpectedY, 2) +
                Math.Pow(result.ActualZ - result.ExpectedZ, 2)
            );

            result.Status = result.Deviation <= result.Tolerance ? MeasurementStatus.Passed : MeasurementStatus.Failed;
            result.UpdateDetails(); // Update the details for the TreeView
            // The final progress update is handled by the SelectionChanged event logic in MainWindow
        }
    }
}
