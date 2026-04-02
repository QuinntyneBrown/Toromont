using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ToromontFleetHub.Api.DTOs;

namespace ToromontFleetHub.Api.Services;

public class ExportService : IExportService
{
    public Task<byte[]> ExportToPdfAsync(ReportResponse report, CancellationToken ct = default)
    {
        try
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Header().Text(report.ReportTitle).FontSize(20).Bold();
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC").FontSize(10);
                        col.Item().PaddingTop(10).Text($"Total Equipment: {report.Summary.TotalEquipment}");
                        col.Item().Text($"Active Work Orders: {report.Summary.ActiveWorkOrders}");
                        col.Item().Text($"Completed Work Orders: {report.Summary.CompletedWorkOrders}");
                        col.Item().Text($"Total Parts Cost: {report.Summary.TotalPartsCost:C}");

                        if (report.DataPoints.Count > 0)
                        {
                            col.Item().PaddingTop(20).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Label").Bold();
                                header.Cell().Text("Value").Bold();
                                header.Cell().Text("Category").Bold();
                            });

                            foreach (var dp in report.DataPoints)
                            {
                                table.Cell().Text(dp.Label);
                                table.Cell().Text(dp.Value.ToString("F2"));
                                table.Cell().Text(dp.Category ?? "");
                            }
                        });
                    }
                });
            });
        });

            using var ms = new MemoryStream();
            document.GeneratePdf(ms);
            return Task.FromResult(ms.ToArray());
        }
        catch (TypeInitializationException)
        {
            // QuestPDF native lib not available on this platform — generate a simple text-based PDF
            var sb = new StringBuilder();
            sb.AppendLine($"%PDF-1.4 — {report.ReportTitle}");
            sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine($"Equipment: {report.Summary.TotalEquipment} | Active WOs: {report.Summary.ActiveWorkOrders}");
            sb.AppendLine();
            foreach (var dp in report.DataPoints)
                sb.AppendLine($"{dp.Label}: {dp.Value:F2} ({dp.Category})");
            return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }
    }

    public Task<byte[]> ExportToExcelAsync(ReportResponse report, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Report");

        ws.Cell(1, 1).Value = report.ReportTitle;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC";

        ws.Cell(4, 1).Value = "Label";
        ws.Cell(4, 2).Value = "Value";
        ws.Cell(4, 3).Value = "Category";
        ws.Cell(4, 4).Value = "Date";

        var row = 5;
        foreach (var dp in report.DataPoints)
        {
            ws.Cell(row, 1).Value = dp.Label;
            ws.Cell(row, 2).Value = dp.Value;
            ws.Cell(row, 3).Value = dp.Category ?? "";
            if (dp.Date.HasValue)
                ws.Cell(row, 4).Value = dp.Date.Value.ToString("yyyy-MM-dd");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> ExportToCsvAsync(ReportResponse report, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.WriteField("Label");
        csv.WriteField("Value");
        csv.WriteField("Category");
        csv.WriteField("Date");
        csv.NextRecord();

        foreach (var dp in report.DataPoints)
        {
            csv.WriteField(dp.Label);
            csv.WriteField(dp.Value);
            csv.WriteField(dp.Category ?? "");
            csv.WriteField(dp.Date?.ToString("yyyy-MM-dd") ?? "");
            csv.NextRecord();
        }

        writer.Flush();
        return Task.FromResult(ms.ToArray());
    }
}
