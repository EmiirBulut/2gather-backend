using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Infrastructure.Settings;

namespace TwoGather.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IResend resend, IOptions<ResendOptions> options, ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendInviteAsync(string toEmail, string listName, string inviterName, string inviteToken, MemberRole role, CancellationToken cancellationToken = default)
    {
        var acceptUrl = $"{_options.AppBaseUrl}/invite/accept?token={inviteToken}";
        var roleLabel = role switch
        {
            MemberRole.Editor => "Editör",
            MemberRole.Viewer => "İzleyici",
            _ => role.ToString()
        };

        var message = new EmailMessage
        {
            From = $"{_options.FromName} <{_options.FromEmail}>",
            Subject = $"{inviterName} sizi \"{listName}\" listesine davet etti",
            HtmlBody = BuildInviteHtml(inviterName, listName, roleLabel, acceptUrl)
        };
        message.To.Add(toEmail);

        try
        {
            await _resend.EmailSendAsync(message, cancellationToken);
            _logger.LogInformation("Invite email sent to {Email} for list {ListName}", toEmail, listName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invite email to {Email} for list {ListName}", toEmail, listName);
            throw;
        }
    }

    private static string BuildInviteHtml(string inviterName, string listName, string roleLabel, string inviteUrl) => $"""
        <!DOCTYPE html>
        <html lang="tr">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
          <title>2gather — Davet</title>
        </head>
        <body style="margin:0;padding:0;background:#F5F3EE;font-family:'Inter',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#F5F3EE;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="560" cellpadding="0" cellspacing="0" style="background:#FFFFFF;border-radius:16px;overflow:hidden;border:1px solid #E8E6E0;">
                  <tr>
                    <td style="background:#3D5A4C;padding:32px 40px;">
                      <p style="margin:0;font-size:22px;font-weight:700;color:#FFFFFF;letter-spacing:-0.5px;">2gather</p>
                      <p style="margin:4px 0 0;font-size:12px;color:#B8CBB8;letter-spacing:0.05em;">BİRLİKTE PLAYIN</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:40px;">
                      <p style="margin:0 0 8px;font-size:11px;color:#9B9B9B;letter-spacing:0.1em;text-transform:uppercase;">Davet</p>
                      <h1 style="margin:0 0 24px;font-size:28px;font-weight:700;color:#1A1A1A;line-height:1.2;">Sizi bir listeye davet ettiler</h1>
                      <p style="margin:0 0 24px;font-size:15px;color:#6B6B6B;line-height:1.6;">
                        <strong style="color:#1A1A1A;">{inviterName}</strong>, sizi
                        <strong style="color:#1A1A1A;">"{listName}"</strong> listesine
                        <strong style="color:#3D5A4C;">{roleLabel}</strong> olarak davet etti.
                      </p>
                      <table width="100%" cellpadding="0" cellspacing="0" style="background:#EEF2EF;border-radius:10px;margin-bottom:32px;">
                        <tr>
                          <td style="padding:16px 20px;">
                            <p style="margin:0;font-size:13px;color:#3D5A4C;font-weight:600;">{roleLabel} olarak şunları yapabilirsiniz:</p>
                            <p style="margin:6px 0 0;font-size:13px;color:#6B8F7A;line-height:1.5;">{GetRoleDescription(roleLabel)}</p>
                          </td>
                        </tr>
                      </table>
                      <table cellpadding="0" cellspacing="0">
                        <tr>
                          <td style="background:#3D5A4C;border-radius:10px;">
                            <a href="{inviteUrl}" style="display:inline-block;padding:14px 32px;font-size:15px;font-weight:600;color:#FFFFFF;text-decoration:none;">Daveti Kabul Et →</a>
                          </td>
                        </tr>
                      </table>
                      <p style="margin:24px 0 0;font-size:13px;color:#9B9B9B;line-height:1.5;">Bu davet 48 saat içinde geçerliliğini yitirir. Butona tıklayamazsanız aşağıdaki bağlantıyı tarayıcınıza kopyalayın:</p>
                      <p style="margin:8px 0 0;font-size:12px;color:#9B9B9B;word-break:break-all;">{inviteUrl}</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:24px 40px;border-top:1px solid #E8E6E0;">
                      <p style="margin:0;font-size:12px;color:#9B9B9B;">Bu daveti siz istemediyseniz bu e-postayı görmezden gelebilirsiniz.</p>
                      <p style="margin:8px 0 0;font-size:12px;color:#9B9B9B;">© 2gather — Birlikte Planlayın</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string GetRoleDescription(string roleLabel) => roleLabel switch
    {
        "Editör" => "Liste üzerinde item ve seçenek ekleyebilir, düzenleyebilir ve işaretleyebilirsiniz.",
        "İzleyici" => "Listeyi görüntüleyebilir, ilerlemeyi takip edebilirsiniz.",
        _ => "Liste üzerinde işlem yapabilirsiniz."
    };
}
