using ProgrWPF.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace ProgrWPF.Services
{
    public class ReportService
    {
        public void ExportToPdf(IEnumerable<MeasurementResult> measurementResults, string filePath)
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
            int totalPoints = measurementResults.Count();
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
            renderer.PdfDocument.Save(filePath);
        }

        public void ExportToCsv(IEnumerable<MeasurementResult> measurementResults, string filePath)
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

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
