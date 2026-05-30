using Kohinoorea.Shared.Models.Commerce;

namespace Kohinoorea.Server.Services;

public static class PlanExpiringSoonHtmlRenderer
{
    public static string Render(ProductDto product, ActivePlanDto plan)
    {
        var name = string.IsNullOrWhiteSpace(plan.UserFullName) ? "there" : plan.UserFullName.Trim();
        var validTo = plan.ValidToUtc?.ToLocalTime().ToString("dd MMM yyyy") ?? "soon";
        var image = string.IsNullOrWhiteSpace(product.ImageLink) ? null : product.ImageLink.Trim();
        var summary = ExtractFirstParagraph(product.Description);

        return Wrap(
            title: "Plan Expiring Soon",
            eyebrow: "KOHINOOR EA • RENEWAL REMINDER",
            heading: "Your plan is expiring soon",
            bodyHtml: $@"
              <p style=""margin:0;color:#9ca3af;line-height:1.7"">
                Hi {Escape(name)}, your <strong style=""color:#f3f4f6"">{Escape(product.Name)}</strong> plan will expire on
                <strong style=""color:#f3f4f6"">{Escape(validTo)}</strong>.
              </p>
              <div style=""margin-top:14px;padding:14px;border-radius:16px;background:rgba(0,0,0,.18);border:1px solid rgba(255,255,255,.10);color:#cbd5e1;line-height:1.7"">
                Renew now to avoid interruption. Reply to this email or open a support ticket from your dashboard and we’ll help you continue instantly.
              </div>
              {RenderProductCard(image, product.Name, summary, product.Price)}
              <div style=""margin-top:16px;color:#9ca3af;font-size:12px;line-height:1.6"">
                Order: <strong style=""color:#e5e7eb"">ORD-{plan.OrderId}</strong>
              </div>"
        );
    }

    private static string Wrap(string title, string eyebrow, string heading, string bodyHtml)
    {
        return $@"<!doctype html>
<html lang=""en"">
<head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""><title>{Escape(title)}</title></head>
<body style=""margin:0;padding:0;background:#0b1020;color:#e5e7eb;font-family:Inter,system-ui,Segoe UI,Arial,sans-serif;"">
  <div style=""max-width:760px;margin:0 auto;padding:26px;"">
    <div style=""border:1px solid rgba(255,255,255,.10);border-radius:22px;background:linear-gradient(180deg,rgba(255,255,255,.05),rgba(255,255,255,.03));padding:22px;"">
      <div style=""font-size:11px;letter-spacing:.12em;text-transform:uppercase;color:#f59e0b;font-weight:900"">{Escape(eyebrow)}</div>
      <h1 style=""margin:10px 0 10px;font-size:22px;letter-spacing:-.02em"">{Escape(heading)}</h1>
      {bodyHtml}
    </div>
    <div style=""margin-top:14px;color:#6b7280;font-size:12px;text-align:center"">
      © {DateTime.UtcNow.Year} Kohinoor EA • This is an automated notification.
    </div>
  </div>
</body>
</html>";
    }

    private static string RenderProductCard(string? imageUrl, string name, string? summary, decimal price)
    {
        var img = string.IsNullOrWhiteSpace(imageUrl)
            ? ""
            : $@"<div style=""height:160px;border-radius:16px;overflow:hidden;border:1px solid rgba(255,255,255,.10);background:rgba(0,0,0,.18)"">
                  <img src=""{Escape(imageUrl)}"" alt=""{Escape(name)}"" style=""width:100%;height:100%;object-fit:cover;display:block"">
                </div>";

        var summaryHtml = string.IsNullOrWhiteSpace(summary)
            ? ""
            : $@"<div style=""margin-top:10px;color:#cbd5e1;line-height:1.7"">{Escape(summary)}</div>";

        return $@"
          <div style=""margin-top:16px;padding:14px;border-radius:18px;border:1px solid rgba(255,255,255,.10);background:rgba(0,0,0,.14)"">
            {img}
            <div style=""margin-top:12px;font-weight:900;font-size:16px"">{Escape(name)}</div>
            {summaryHtml}
            <div style=""margin-top:10px;color:#f59e0b;font-weight:900;font-size:14px"">${price:0.00}</div>
          </div>";
    }

    private static string? ExtractFirstParagraph(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(html, @"<p>(.*?)</p>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }

    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value);
}

