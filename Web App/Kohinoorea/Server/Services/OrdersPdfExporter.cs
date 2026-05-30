using Kohinoorea.Shared.Models.Commerce;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Kohinoorea.Server.Services;

public static class OrdersPdfExporter
{
    public static byte[] Render(IReadOnlyList<OrderTraceDto> orders, string filterText)
    {
        var generatedUtc = DateTime.UtcNow;
        var filter = string.IsNullOrWhiteSpace(filterText) ? "No filter" : filterText.Trim();

        IContainer HeaderCell(IContainer c) => c
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(6)
            .PaddingHorizontal(6)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1);

        IContainer BodyCell(IContainer c) => c
            .PaddingVertical(6)
            .PaddingHorizontal(6)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Kohinoor EA — Orders Export").FontSize(16).SemiBold();
                        col.Item().Text($"Generated: {generatedUtc:dd MMM yyyy HH:mm} UTC").FontColor(Colors.Grey.Darken2);
                        col.Item().Text($"Items: {orders.Count}").FontColor(Colors.Grey.Darken2);
                    });

                    row.ConstantItem(220).AlignRight().Column(col =>
                    {
                        col.Item().PaddingTop(6).Text("Filter").FontSize(9).FontColor(Colors.Grey.Darken2);
                        col.Item()
                            .Background(Colors.Grey.Lighten4)
                            .Padding(8)
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Text(filter)
                            .FontSize(10);
                    });
                });

                page.Content().PaddingTop(14).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(58);      // Order
                        columns.RelativeColumn(1.4f);    // Product
                        columns.RelativeColumn(1.1f);    // User
                        columns.RelativeColumn(1.6f);    // Email
                        columns.ConstantColumn(30);      // Qty
                        columns.ConstantColumn(58);      // Total
                        columns.ConstantColumn(78);      // Ordered
                        columns.ConstantColumn(62);      // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("ORDER").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                        header.Cell().Element(HeaderCell).Text("PRODUCT").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                        header.Cell().Element(HeaderCell).Text("USER").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                        header.Cell().Element(HeaderCell).Text("EMAIL").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                        header.Cell().Element(HeaderCell).AlignRight().Text("QTY").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                        header.Cell().Element(HeaderCell).AlignRight().Text("TOTAL").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                        header.Cell().Element(HeaderCell).Text("ORDERED").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                        header.Cell().Element(HeaderCell).Text("STATUS").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                    });

                    if (orders.Count == 0)
                    {
                        table.Cell().ColumnSpan(8).PaddingVertical(18).Text("No orders match this filter.").FontColor(Colors.Grey.Darken2);
                        return;
                    }

                    foreach (var o in orders)
                    {
                        table.Cell().Element(BodyCell).Text($"ORD-{o.OrderId}");
                        table.Cell().Element(BodyCell).Text(o.ProductName ?? string.Empty);
                        table.Cell().Element(BodyCell).Text(o.UserFullName ?? string.Empty);
                        table.Cell().Element(BodyCell).Text(o.UserEmail ?? string.Empty);
                        table.Cell().Element(BodyCell).AlignRight().Text(o.Quantity.ToString());
                        table.Cell().Element(BodyCell).AlignRight().Text(o.TotalAmount.ToString("0.00"));
                        table.Cell().Element(BodyCell).Text(o.OrderedAtUtc.ToString("dd MMM yyyy HH:mm"));
                        table.Cell().Element(BodyCell).Text(Normalize(o.Status)).SemiBold();
                    }
                });

                page.Footer().AlignCenter().Text("Kohinoor EA • Orders Export").FontSize(9).FontColor(Colors.Grey.Darken2);
            });
        });

        return document.GeneratePdf();
    }

    private static string Normalize(string? status)
    {
        var s = status?.Trim();
        if (string.IsNullOrWhiteSpace(s))
        {
            return "Pending";
        }

        return s.ToLowerInvariant() switch
        {
            "completed" => "Completed",
            "denied" => "Denied",
            "cancelled" => "Cancelled",
            _ => char.ToUpperInvariant(s[0]) + s[1..]
        };
    }
}

