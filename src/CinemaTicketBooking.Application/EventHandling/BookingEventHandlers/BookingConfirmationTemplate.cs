using System.Text;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Carries all data needed to render the booking-confirmation email template.
/// </summary>
public sealed class BookingConfirmationEmailModel
{
    public required string RecipientEmail { get; init; }
    public required string RecipientName { get; init; }
    public required string BookingCode { get; init; }
    public required string BookingId { get; init; }
    public required string MovieName { get; init; }
    public required string CinemaName { get; init; }
    public required string ScreenCode { get; init; }
    public required string ScreenFormat { get; init; }
    public required DateTimeOffset ShowTimeStartAt { get; init; }
    public required DateTimeOffset ShowTimeEndAt { get; init; }
    public required IReadOnlyList<TicketLineItem> Tickets { get; init; }
    public required IReadOnlyList<ConcessionLineItem> Concessions { get; init; }
    public required decimal TotalAmount { get; init; }
}

public sealed class TicketLineItem
{
    public required string SeatCode { get; init; }
    public required string SeatType { get; init; }
    public required decimal Price { get; init; }
}

public sealed class ConcessionLineItem
{
    public required string Name { get; init; }
    public required int Quantity { get; init; }
    public required decimal Price { get; init; }
}

/// <summary>
/// Renders the booking-confirmation HTML email using inline template strings.
/// Self-contained: no Razor engine needed.
/// </summary>
public static class BookingConfirmationTemplate
{
    private const string VietnamTimeZone = "SE Asia Standard Time";
    private static readonly TimeZoneInfo Vn7 = TimeZoneInfo.FindSystemTimeZoneById(VietnamTimeZone);

    // =============================================
    // Public entry point
    // =============================================

    /// <summary>
    /// Builds the full HTML email body from the given model.
    /// </summary>
    public static string Render(BookingConfirmationEmailModel m)
    {
        var startVn = TimeZoneInfo.ConvertTime(m.ShowTimeStartAt, Vn7);
        var endVn = TimeZoneInfo.ConvertTime(m.ShowTimeEndAt, Vn7);

        // QR data-uri: We embed a Google Charts QR image as a fallback since
        // no server-side image library is required.
        var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(m.BookingId)}";

        var sb = new StringBuilder(8192);
        sb.Append(Head(m.MovieName));
        sb.Append(OpenBody());
        sb.Append(HeroSection(m.MovieName));
        sb.Append(BookingCodeBadge(m.BookingCode));
        sb.Append(ShowtimeSection(m, startVn, endVn));
        sb.Append(TicketsSection(m.Tickets));
        if (m.Concessions.Count > 0)
        {
            sb.Append(ConcessionsSection(m.Concessions));
        }
        sb.Append(TotalSection(m.TotalAmount));
        sb.Append(QrSection(qrUrl, m.BookingCode));
        sb.Append(Footer());
        sb.Append(CloseBody());

        return sb.ToString();
    }

    // =============================================
    // Template segments
    // =============================================

    private static string Head(string movieName) => $$"""
        <!DOCTYPE html>
        <html lang="vi">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Xác nhận đặt vé – {{HtmlEncode(movieName)}}</title>
          <style>
            /* ── Reset ── */
            body,table,td,a{margin:0;padding:0;border:none;font-family:'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;}
            img{border:0;display:block;}
            /* ── Layout ── */
            .wrapper{background:#0b0e14;padding:32px 16px;}
            .card{max-width:600px;margin:0 auto;background:#161a21;border-radius:16px;overflow:hidden;box-shadow:0 8px 40px rgba(0,0,0,.6);border:1px solid rgba(255,255,255,0.05);}
            /* ── Hero ── */
            .hero{background:linear-gradient(135deg,#61b4fe 0%,#00f4fe 100%);padding:40px 32px;text-align:center;}
            .hero-logo{font-size:24px;font-weight:800;color:#0b0e14;letter-spacing:2px;margin-bottom:4px;}
            .hero-tagline{font-size:12px;color:rgba(11,14,20,0.7);letter-spacing:4px;text-transform:uppercase;font-weight:700;}
            .hero-movie{font-size:24px;font-weight:700;color:#0b0e14;margin-top:20px;line-height:1.3;}
            /* ── Booking code badge ── */
            .code-wrap{background:#10131a;padding:24px 32px;text-align:center;border-bottom:1px solid rgba(255,255,255,.05);}
            .code-label{font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#a9abb3;margin-bottom:8px;}
            .code-value{font-size:26px;font-weight:800;letter-spacing:4px;color:#00f4fe;font-family:monospace;}
            /* ── Sections ── */
            .section{padding:24px 32px;border-bottom:1px solid rgba(255,255,255,.05);}
            .section-title{font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#73757d;margin-bottom:18px;font-weight:700;}
            /* ── Info rows ── */
            .info-grid{width:100%;}
            .info-label{font-size:11px;color:#73757d;margin-bottom:4px;text-transform:uppercase;letter-spacing:1px;}
            .info-val{font-size:14px;color:#ecedf6;font-weight:600;}
            /* ── Showtime highlight ── */
            .time-block{background:rgba(97, 180, 254, 0.05);border:1px solid rgba(97, 180, 254, 0.2);border-radius:12px;padding:20px;text-align:center;margin-top:20px;}
            .time-day{font-size:12px;color:#61b4fe;letter-spacing:2px;text-transform:uppercase;font-weight:700;}
            .time-hours{font-size:28px;font-weight:800;color:#ecedf6;margin-top:6px;}
            .time-duration{font-size:12px;color:#a9abb3;margin-top:4px;}
            /* ── Ticket table ── */
            .tkt-table{width:100%;border-collapse:collapse;}
            .tkt-table th{font-size:11px;letter-spacing:2px;text-transform:uppercase;color:#73757d;padding:8px 0;text-align:left;border-bottom:1px solid rgba(255,255,255,.05);}
            .tkt-table td{padding:12px 0;color:#ecedf6;font-size:14px;border-bottom:1px solid rgba(255,255,255,.03);vertical-align:middle;}
            .tkt-table td.price{color:#ecedf6;font-weight:700;text-align:right;font-variant-numeric:tabular-nums;}
            .tkt-type-badge{display:inline-block;font-size:10px;padding:2px 8px;border-radius:4px;background:rgba(97, 180, 254, 0.1);color:#61b4fe;font-weight:700;letter-spacing:1px;text-transform:uppercase;margin-left:8px;border:1px solid rgba(97, 180, 254, 0.2);}
            /* ── Total row ── */
            .total-row td{padding-top:16px;border-bottom:none;}
            .total-label{font-size:14px;color:#a9abb3;font-weight:600;text-align:right;padding-right:16px;text-transform:uppercase;letter-spacing:1px;}
            .total-amount{font-size:24px;font-weight:800;color:#00f4fe;text-align:right;font-variant-numeric:tabular-nums;}
            /* ── QR section ── */
            .qr-wrap{background:#10131a;padding:32px;text-align:center;}
            .qr-img-box{display:inline-block;padding:12px;background:#fff;border-radius:16px;border:4px solid rgba(0, 244, 254, 0.3);margin-bottom:16px;}
            .qr-hint{font-size:13px;color:#73757d;max-width:240px;margin:0 auto;line-height:1.5;}
            /* ── Footer ── */
            .footer{padding:32px;text-align:center;background:#0b0e14;}
            .footer p{font-size:12px;color:#52555c;line-height:1.6;margin:6px 0;}
            .footer a{color:#61b4fe;text-decoration:none;font-weight:600;}
          </style>
        </head>
        """;

    private static string OpenBody() => """
        <body>
        <div class="wrapper">
        <div class="card">
        """;

    private static string HeroSection(string movieName) => $"""
        <div class="hero">
          <div class="hero-logo">VICINEMA TICKET</div>
          <div class="hero-tagline">thanh toán thành công</div>
          <div class="hero-movie">{HtmlEncode(movieName)}</div>
        </div>
        """;
    
    private static string BookingCodeBadge(string bookingCode) => $"""
        <div class="code-wrap">
          <div class="code-label">Mã vé của bạn</div>
          <div class="code-value">{HtmlEncode(bookingCode)}</div>
        </div>
        """;

    private static string ShowtimeSection(BookingConfirmationEmailModel m, DateTimeOffset startVn, DateTimeOffset endVn)
    {
        var duration = (int)(endVn - startVn).TotalMinutes;
        return $"""
        <div class="section">
          <div class="section-title">Thông tin suất chiếu</div>
          <table class="info-grid">
            <tr>
              <td class="info-item">
                <div class="info-label">Rạp / Phòng</div>
                <div class="info-val">{HtmlEncode(m.CinemaName)} – {HtmlEncode(m.ScreenCode)}</div>
              </td>
              <td class="info-item">
                <div class="info-label">Định dạng</div>
                <div class="info-val"><span class="tkt-type-badge" style="margin-left:0;">{HtmlEncode(m.ScreenFormat)}</span></div>
              </td>
            </tr>
            <tr>
              <td class="info-item">
                <div class="info-label">Khách hàng</div>
                <div class="info-val">{HtmlEncode(m.RecipientName)}</div>
              </td>
              <td class="info-item">
                <div class="info-label">Số lượng</div>
                <div class="info-val">{m.Tickets.Count} vé</div>
              </td>
            </tr>
          </table>
          <div class="time-block">
            <div class="time-day">{startVn:dddd, dd MMMM yyyy}</div>
            <div class="time-hours">{startVn:HH:mm} – {endVn:HH:mm}</div>
            <div class="time-duration">Suất chiếu kéo dài ~{duration} phút</div>
          </div>
        </div>
        """;
    }

    private static string TicketsSection(IReadOnlyList<TicketLineItem> tickets)
    {
        var rows = new StringBuilder();
        foreach (var t in tickets)
        {
            rows.Append($"""
                <tr>
                  <td>{HtmlEncode(t.SeatCode)}<span class="tkt-type-badge">{HtmlEncode(t.SeatType)}</span></td>
                  <td class="price">{t.Price:#,##0} ₫</td>
                </tr>
                """);
        }

        return $"""
        <div class="section">
          <div class="section-title">Chi tiết vé</div>
          <table class="tkt-table">
            <thead>
              <tr>
                <th>Ghế</th>
                <th style="text-align:right;">Giá</th>
              </tr>
            </thead>
            <tbody>
              {rows}
            </tbody>
          </table>
        </div>
        """;
    }

    private static string ConcessionsSection(IReadOnlyList<ConcessionLineItem> items)
    {
        var rows = new StringBuilder();
        foreach (var i in items)
        {
            rows.Append($"""
                <tr>
                  <td>{HtmlEncode(i.Name)} <span style="font-size:11px;color:#888;">x{i.Quantity}</span></td>
                  <td class="price">{(i.Price * i.Quantity):#,##0} ₫</td>
                </tr>
                """);
        }

        return $"""
        <div class="section">
          <div class="section-title">Bắp nước & Dịch vụ</div>
          <table class="tkt-table">
            <tbody>
              {rows}
            </tbody>
          </table>
        </div>
        """;
    }

    private static string TotalSection(decimal total)
    {
        return $"""
        <div class="section" style="border-bottom:none; background:rgba(0, 244, 254, 0.03);">
          <table class="tkt-table" style="margin-top:0;">
            <tr class="total-row">
                <td class="total-label" style="padding-top:0;">Tổng cộng</td>
                <td class="total-amount" style="padding-top:0;">{total:#,##0} ₫</td>
            </tr>
          </table>
        </div>
        """;
    }

    private static string QrSection(string qrUrl, string bookingCode) => $"""
        <div class="qr-wrap">
          <div class="section-title" style="margin-bottom:20px; text-align:center;">Mã QR Check-in</div>
          <div class="qr-img-box">
            <img src="{qrUrl}" width="180" height="180" alt="QR {HtmlEncode(bookingCode)}" />
          </div>
          <div class="qr-hint">Xuất trình mã QR này tại quầy hoặc cổng soát vé để vào rạp</div>
        </div>
        """;

    private static string Footer() => $"""
        <div class="footer">
          <p>Email này được gửi tự động từ hệ thống đặt vé. Vui lòng không trả lời.</p>
          <p>Hỗ trợ: <a href="mailto:support@vicinema.vn">support@cinema.vn</a> | Hotline: 1900 xxxx</p>
          <p style="margin-top:16px; opacity:0.6;">© {DateTimeOffset.UtcNow.Year} Vicinema. All rights reserved.</p>
        </div>
        """;

    private static string CloseBody() => """
        </div>
        </div>
        </body>
        </html>
        """;

    // =============================================
    // Helpers
    // =============================================

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
