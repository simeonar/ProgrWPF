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
using Microsoft.Win32; // For SaveFileDialog
using System.IO;       // For File operations
using System.Text;     // For StringBuilder
using System.Linq;     // For Count(p => ...)
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Pdf;

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

            // Initialize simulation timer
            simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            simulationTimer.Tick += SimulationTimer_Tick;

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
                    // --- Create Document ---
                    Document document = new Document();
                    document.Info.Title = "CMM Measurement Report";
                    document.Info.Author = "ProgrWPF CMM Application";

                    // --- Page Setup ---
                    Section section = document.AddSection();
                    section.PageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;
                    section.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
                    section.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
                    section.PageSetup.TopMargin = Unit.FromCentimeter(2.5);
                    section.PageSetup.BottomMargin = Unit.FromCentimeter(2.0);

                    // --- Header ---
                    Paragraph header = section.Headers.Primary.AddParagraph();
                    header.AddText("ProgrWPF CMM // Measurement Report");
                    header.Format.Font.Size = 9;
                    header.Format.Alignment = ParagraphAlignment.Right;

                    // --- Footer ---
                    Paragraph footer = section.Footers.Primary.AddParagraph();
                    footer.AddText("Page ");
                    footer.AddPageField();
                    footer.AddText(" of ");
                    footer.AddNumPagesField();
                    footer.Format.Font.Size = 9;
                    footer.Format.Alignment = ParagraphAlignment.Center;

                    // --- Title ---
                    Paragraph title = section.AddParagraph("Measurement Report");
                    title.Format.Font.Size = 24;
                    title.Format.Font.Bold = true;
                    title.Format.Alignment = ParagraphAlignment.Center;
                    title.Format.SpaceAfter = Unit.FromCentimeter(1);

                    // --- Summary Section ---
                    int totalPoints = measurementResults.Count;
                    int passedPoints = measurementResults.Count(r => r.Status == MeasurementStatus.Passed);
                    int failedPoints = measurementResults.Count(r => r.Status == MeasurementStatus.Failed);

                    Paragraph summary = section.AddParagraph();
                    summary.AddFormattedText("Summary:", TextFormat.Bold);
                    summary.AddText($" Total Points: {totalPoints} | ");
                    summary.AddFormattedText($"Passed: {passedPoints}", TextFormat.Bold);
                    summary.AddText(" | ");
                    summary.AddFormattedText($"Failed: {failedPoints}", TextFormat.Bold);
                    summary.Format.Font.Size = 11;
                    summary.Format.SpaceAfter = Unit.FromCentimeter(1);

                    // --- Results Table ---
                    Table table = section.AddTable();
                    table.Borders.Width = 0.25;
                    table.Borders.Color = MigraDoc.DocumentObjectModel.Colors.Gray;

                    // Define columns - adjusted for landscape
                    double[] columnWidths = { 140, 55, 80, 50, 50, 50, 50, 50, 50, 65, 65 };
                    foreach (var width in columnWidths)
                    {
                        table.AddColumn(Unit.FromPoint(width));
                    }

                    // Create header row
                    Row headerRow = table.AddRow();
                    headerRow.HeadingFormat = true;
                    headerRow.Format.Font.Bold = true;
                    headerRow.Format.Font.Size = 9;
                    headerRow.Shading.Color = MigraDoc.DocumentObjectModel.Colors.LightGray;
                    headerRow.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;

                    string[] headers = { "Point Name", "Status", "Timestamp", "Exp. X", "Exp. Y", "Exp. Z", "Act. X", "Act. Y", "Act. Z", "Deviation", "Tolerance" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        headerRow.Cells[i].AddParagraph(headers[i]);
                    }

                    // Add data rows
                    int rowIndex = 0;
                    foreach (var result in measurementResults)
                    {
                        Row row = table.AddRow();
                        row.Height = Unit.FromCentimeter(0.6);
                        row.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
                        row.Format.Font.Size = 8;

                        // Alternating row color
                        if (rowIndex % 2 == 1)
                        {
                            row.Shading.Color = MigraDoc.DocumentObjectModel.Colors.WhiteSmoke;
                        }
                        rowIndex++;

                        row.Cells[0].AddParagraph(result.PointName);
                        row.Cells[1].AddParagraph(result.Status.ToString());
                        row.Cells[2].AddParagraph(result.Timestamp.ToString("g"));
                        row.Cells[3].AddParagraph(result.ExpectedX.ToString("F4"));
                        row.Cells[4].AddParagraph(result.ExpectedY.ToString("F4"));
                        row.Cells[5].AddParagraph(result.ExpectedZ.ToString("F4"));
                        row.Cells[6].AddParagraph(result.ActualX.ToString("F4"));
                        row.Cells[7].AddParagraph(result.ActualY.ToString("F4"));
                        row.Cells[8].AddParagraph(result.ActualZ.ToString("F4"));
                        row.Cells[9].AddParagraph(result.Deviation.ToString("F4"));
                        row.Cells[10].AddParagraph(result.Tolerance.ToString("F4"));

                        // Color code status cell
                        switch (result.Status)
                        {
                            case MeasurementStatus.Passed:
                                row.Cells[1].Shading.Color = MigraDoc.DocumentObjectModel.Colors.LightGreen;
                                break;
                            case MeasurementStatus.Failed:
                                row.Cells[1].Shading.Color = MigraDoc.DocumentObjectModel.Colors.LightCoral;
                                break;
                        }
                    }

                    // --- Render and Save ---
                    PdfDocumentRenderer renderer = new PdfDocumentRenderer();
                    renderer.Document = document;
                    renderer.RenderDocument();
                    renderer.PdfDocument.Save(saveFileDialog.FileName);

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
                    var sb = new StringBuilder();
                    // Add header row
                    sb.AppendLine("PointName,Status,Timestamp,ExpectedX,ExpectedY,ExpectedZ,ActualX,ActualY,ActualZ,Deviation,Tolerance");

                    // Add data rows
                    foreach (var result in measurementResults)
                    {
                        var line = string.Join(",",
                            string.Format("\"{0}\"", result.PointName.Replace("\"", "\"\"")), // Handle commas/quotes in name
                            result.Status,
                            result.Timestamp.ToString("o"), // ISO 8601 format for consistency
                            result.ExpectedX,
                            result.ExpectedY,
                            result.ExpectedZ,
                            result.ActualX,
                            result.ActualY,
                            result.ActualZ,
                            result.Deviation,
                            result.Tolerance);
                        sb.AppendLine(line);
                    }

                    File.WriteAllText(saveFileDialog.FileName, sb.ToString());
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
