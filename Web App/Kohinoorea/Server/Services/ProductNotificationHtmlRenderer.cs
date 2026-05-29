using System.Net;
using System.Text.RegularExpressions;
using Kohinoorea.Shared.Models.Commerce;

namespace Kohinoorea.Server.Services;

public static class ProductNotificationHtmlRenderer
{
    public static string Render(ProductDto product)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var title = string.IsNullOrWhiteSpace(product.Name) ? "New product available" : product.Name.Trim();
        var description = StripHtml(product.Description ?? string.Empty);
        if (description.Length > 280)
        {
            description = description[..280].TrimEnd() + "…";
        }

        var price = product.Price > 0 ? $"${product.Price:0.##}" : "—";
        var image = string.IsNullOrWhiteSpace(product.ImageLink) ? null : product.ImageLink.Trim();
        var ctaUrl = "https://kohinoorea.com/dashboard#products";

        var lines = new List<string>
        {
            "<!DOCTYPE html>",
            "<html lang='en'>",
            "<head>",
            "  <meta charset='utf-8'>",
            "  <meta name='viewport' content='width=device-width, initial-scale=1'>",
            "  <title>New Kohinoorea Product</title>",
            "  <style>",
            "    body{margin:0;padding:32px;background:#060810;color:#e5e7eb;font-family:Inter,Arial,sans-serif;}",
            "    .wrap{max-width:860px;margin:0 auto;}",
            "    .card{background:linear-gradient(180deg,rgba(255,255,255,0.05),rgba(255,255,255,0.03));border:1px solid rgba(255,255,255,0.10);border-radius:28px;overflow:hidden;box-shadow:0 28px 90px rgba(0,0,0,0.45);}",
            "    .top{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:22px 24px;border-bottom:1px solid rgba(255,255,255,0.08);}",
            "    .brand{display:flex;align-items:center;gap:12px;font-weight:800;letter-spacing:-0.03em;font-size:18px;}",
            "    .mark{width:42px;height:42px;border-radius:14px;display:grid;place-items:center;background:linear-gradient(135deg,#fbbf24,#f97316);color:#111827;}",
            "    .badge{display:inline-flex;align-items:center;gap:8px;padding:8px 12px;border-radius:999px;border:1px solid rgba(34,197,94,0.22);background:rgba(34,197,94,0.11);color:#22c55e;font-weight:700;font-size:12px;}",
            "    .hero{display:grid;grid-template-columns: 1.05fr 0.95fr;gap:18px;padding:24px;}",
            "    .img{border-radius:22px;overflow:hidden;border:1px solid rgba(255,255,255,0.10);background:rgba(255,255,255,0.03);min-height:220px;}",
            "    .img img{width:100%;height:100%;object-fit:cover;display:block;}",
            "    .content h1{margin:0 0 10px;font-size:26px;letter-spacing:-0.04em;}",
            "    .content p{margin:0 0 14px;color:#9ca3af;line-height:1.65;font-size:14px;}",
            "    .meta{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px;margin-top:14px;}",
            "    .box{border:1px solid rgba(255,255,255,0.10);background:rgba(0,0,0,0.20);border-radius:18px;padding:14px 16px;}",
            "    .box strong{display:block;font-size:11px;letter-spacing:0.08em;text-transform:uppercase;color:#fbbf24;margin-bottom:6px;}",
            "    .box span{font-size:14px;color:#e5e7eb;}",
            "    .cta{display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin-top:18px;}",
            "    .btn{display:inline-block;padding:12px 16px;border-radius:16px;font-weight:800;text-decoration:none;background:linear-gradient(135deg,#f59e0b,#fb923c);color:#111827;}",
            "    .btn2{display:inline-block;padding:12px 16px;border-radius:16px;font-weight:800;text-decoration:none;border:1px solid rgba(255,255,255,0.14);background:rgba(255,255,255,0.06);color:#e5e7eb;}",
            "    .foot{padding:18px 24px;border-top:1px solid rgba(255,255,255,0.08);color:#9ca3af;font-size:12px;line-height:1.6;}",
            "    @media (max-width:720px){body{padding:16px}.hero{grid-template-columns:1fr}.img{min-height:190px}}",
            "  </style>",
            "</head>",
            "<body>",
            "  <div class='wrap'>",
            "    <div class='card'>",
            "      <div class='top'>",
            "        <div class='brand'><div class='mark'>K</div><div>Kohinoorea</div></div>",
            "        <div class='badge'>New Product</div>",
            "      </div>",
            "      <div class='hero'>",
            "        <div class='content'>",
            $"          <h1>{Encode(title)}</h1>",
            $"          <p>{Encode(string.IsNullOrWhiteSpace(description) ? "We just added a new product to your dashboard." : description)}</p>",
            "          <div class='meta'>",
            $"            <div class='box'><strong>Price</strong><span>{Encode(price)}</span></div>",
            $"            <div class='box'><strong>Status</strong><span>{(product.IsActive ? "Active" : "Hidden")}</span></div>",
            "          </div>",
            "          <div class='cta'>",
            "            <a class='btn2' href='https://kohinoorea.com'>Open Website</a>",
            "          </div>",
            "        </div>",
            "        <div class='img'>",
        };

        lines.Add(image is null
            ? "          <div style='height:100%;display:grid;place-items:center;color:#9ca3af;font-size:13px;'>No image</div>"
            : $"          <img src='{Encode(image)}' alt='{Encode(title)}' />");

        lines.AddRange(new[]
        {
            "        </div>",
            "      </div>",
            "      <div class='foot'>You’re receiving this email because you have an active Kohinoorea account. If you did not request this, you can ignore it.</div>",
            "    </div>",
            "  </div>",
            "</body>",
            "</html>"
        });

        return string.Join(Environment.NewLine, lines);
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var noTags = Regex.Replace(input, "<.*?>", string.Empty);
        return WebUtility.HtmlDecode(noTags).Trim();
    }
}
