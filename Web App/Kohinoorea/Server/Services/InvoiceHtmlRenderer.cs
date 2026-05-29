using System.Globalization;
using System.Net;
using Kohinoorea.Shared.Models.Commerce;

namespace Kohinoorea.Server.Services;

public static class InvoiceHtmlRenderer
{
    public static string RenderOrderInvoiceHtml(OrderTraceDto order, string currencyCountryCode = "US")
    {
        var quantity = order.Quantity > 0 ? order.Quantity : 1;
        var unitPrice = order.UnitPrice > 0 ? order.UnitPrice : quantity > 0 ? order.TotalAmount / quantity : order.TotalAmount;
        var generatedAt = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
        var orderedAt = order.OrderedAtUtc.ToLocalTime().ToString("dd MMM yyyy hh:mm tt");
        var statusLabel = string.IsNullOrWhiteSpace(order.Status) ? "Completed" : order.Status.Trim();
        var billedTo = string.IsNullOrWhiteSpace(order.UserEmail) ? order.UserFullName : order.UserEmail;
        var paymentMethod = "Card";

        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var invoiceHtml = string.Join(Environment.NewLine, new[]
        {
            "<!DOCTYPE html>",
            "<html lang='en'>",
            "<head>",
            "  <meta charset='utf-8'>",
            "  <meta name='viewport' content='width=device-width, initial-scale=1'>",
            $"  <title>Invoice ORD-{order.OrderId}</title>",
            "  <style>",
            "    body{margin:0;padding:32px;background:#f5f0e6;color:#1f2937;font-family:Inter,Arial,sans-serif;}",
            "    .invoice{max-width:860px;margin:0 auto;background:#ffffff;border-radius:28px;padding:40px;box-shadow:0 24px 70px rgba(15,23,42,0.12);border:1px solid #f0e7d5;}",
            "    .top{display:flex;justify-content:space-between;align-items:flex-start;gap:24px;margin-bottom:28px;}",
            "    .brand{display:inline-flex;align-items:center;gap:12px;font-weight:800;letter-spacing:-0.03em;font-size:22px;}",
            "    .mark{width:44px;height:44px;border-radius:14px;display:grid;place-items:center;background:linear-gradient(135deg,#fbbf24,#f97316);color:#111827;}",
            "    .label{text-align:right;}",
            "    .label h1{margin:0 0 8px;font-size:28px;letter-spacing:-0.05em;}",
            "    .label p,.card span,.billing span,.note{color:#6b7280;font-size:14px;line-height:1.6;}",
            "    .grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px;margin-bottom:28px;}",
            "    .card,.billing,.summary{border:1px solid #ece5d7;border-radius:18px;padding:16px 18px;background:#faf7f1;}",
            "    .card strong,.billing strong{display:block;margin-bottom:6px;font-size:13px;text-transform:uppercase;letter-spacing:0.08em;color:#9a6a12;}",
            "    .billing{margin-bottom:24px;}",
            "    table{width:100%;border-collapse:collapse;margin-bottom:24px;}",
            "    th,td{padding:14px 12px;border-bottom:1px solid #ece5d7;text-align:left;font-size:14px;}",
            "    th{font-size:12px;text-transform:uppercase;letter-spacing:0.08em;color:#6b7280;}",
            "    .amount{text-align:right;white-space:nowrap;}",
            "    .summary-wrap{display:flex;justify-content:flex-end;}",
            "    .row{display:flex;justify-content:space-between;gap:16px;margin-bottom:10px;font-size:14px;}",
            "    .row.total{margin-top:14px;padding-top:14px;border-top:1px solid #ece5d7;font-weight:800;font-size:17px;color:#111827;}",
            "    .note{margin-top:28px;}",
            "    @media print{body{padding:0;background:#ffffff;}.invoice{box-shadow:none;border:0;}}",
            "  </style>",
            "</head>",
            "<body>",
            "  <div class='invoice'>",
            "    <div class='top'>",
            "      <div class='brand'><div class='mark'>K</div><div>Kohinoorea</div></div>",
            $"      <div class='label'><h1>Invoice</h1><p>Invoice No: INV-ORD-{order.OrderId}<br>Generated: {Encode(generatedAt)}</p></div>",
            "    </div>",
            "    <div class='grid'>",
            $"      <div class='card'><strong>Order ID</strong><span>ORD-{order.OrderId}</span></div>",
            $"      <div class='card'><strong>Order Date</strong><span>{Encode(orderedAt)}</span></div>",
            $"      <div class='card'><strong>Status</strong><span>{Encode(statusLabel)}</span></div>",
            "    </div>",
            $"    <div class='billing'><strong>Billed To</strong><span>{Encode(billedTo)}</span></div>",
            "    <table>",
            "      <thead><tr><th>Product</th><th>Qty</th><th>Unit Price</th><th class='amount'>Line Total</th></tr></thead>",
            "      <tbody>",
            $"        <tr><td>{Encode(order.ProductName)}</td><td>{quantity}</td><td>{Encode(FormatMoney(unitPrice, currencyCountryCode))}</td><td class='amount'>{Encode(FormatMoney(order.TotalAmount, currencyCountryCode))}</td></tr>",
            "      </tbody>",
            "    </table>",
            "    <div class='summary-wrap'>",
            "      <div class='summary'>",
            $"        <div class='row'><span>Payment Method</span><span>{Encode(paymentMethod)}</span></div>",
            $"        <div class='row'><span>Subtotal</span><span>{Encode(FormatMoney(order.TotalAmount, currencyCountryCode))}</span></div>",
            $"        <div class='row'><span>Tax</span><span>{Encode(FormatMoney(0, currencyCountryCode))}</span></div>",
            $"        <div class='row total'><span>Total</span><span>{Encode(FormatMoney(order.TotalAmount, currencyCountryCode))}</span></div>",
            "      </div>",
            "    </div>",
            "    <p class='note'>This invoice was generated from your Kohinoorea dashboard order history. Keep it for your records.</p>",
            "  </div>",
            "</body>",
            "</html>"
        });

        return invoiceHtml;
    }

    private static string FormatMoney(decimal amount, string countryCode)
    {
        // In this app, default/fallback prices are USD.
        var culture = countryCode?.Trim().ToUpperInvariant() == "IN"
            ? CultureInfo.GetCultureInfo("en-IN")
            : CultureInfo.GetCultureInfo("en-US");

        return string.Format(culture, "{0:C0}", amount);
    }
}

