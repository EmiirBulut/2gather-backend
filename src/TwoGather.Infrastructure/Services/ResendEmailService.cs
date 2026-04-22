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
            MemberRole.Editor => "Editor",
            MemberRole.Viewer => "Viewer",
            _ => role.ToString()
        };

        var html = BuildInviteHtml(listName, inviterName, roleLabel, acceptUrl);

        var message = new EmailMessage
        {
            From = $"{_options.FromName} <{_options.FromEmail}>",
            Subject = $"{inviterName} sizi \"{listName}\" listesine davet etti",
            HtmlBody = html
        };
        message.To.Add(toEmail);

        await _resend.EmailSendAsync(message, cancellationToken);

        _logger.LogInformation("Invite email sent to {Email} for list {ListName}", toEmail, listName);
    }

    private static string BuildInviteHtml(string listName, string inviterName, string role, string acceptUrl) => $"""
        <!DOCTYPE html>
        <html lang="tr">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
          <title>2gather Davet</title>
        </head>
        <body style="margin:0;padding:0;background:#f4f4f5;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
                  <tr>
                    <td style="background:#1a5c4a;padding:32px 40px;text-align:center;">
                      <h1 style="margin:0;color:#ffffff;font-size:28px;font-weight:700;letter-spacing:-0.5px;">2gather</h1>
                      <p style="margin:6px 0 0;color:#a8d5c2;font-size:13px;">Ortak Ev Planlama</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:40px 40px 24px;">
                      <h2 style="margin:0 0 16px;color:#1a1a1a;font-size:22px;font-weight:600;">Listeye Davet Edildiniz</h2>
                      <p style="margin:0 0 24px;color:#4b5563;font-size:15px;line-height:1.6;">
                        <strong>{inviterName}</strong>, sizi <strong>"{listName}"</strong> listesinde <strong>{role}</strong> rolüyle iş birliği yapmaya davet etti.
                      </p>
                      <table cellpadding="0" cellspacing="0" style="background:#f0faf6;border-left:4px solid #1a5c4a;border-radius:4px;margin-bottom:32px;">
                        <tr>
                          <td style="padding:16px 20px;">
                            <p style="margin:0;color:#1a5c4a;font-size:14px;font-weight:600;">Rol: {role}</p>
                          </td>
                        </tr>
                      </table>
                      <table cellpadding="0" cellspacing="0" width="100%">
                        <tr>
                          <td align="center">
                            <a href="{acceptUrl}" style="display:inline-block;background:#1a5c4a;color:#ffffff;text-decoration:none;font-size:16px;font-weight:600;padding:14px 40px;border-radius:8px;">Daveti Kabul Et</a>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:24px 40px 40px;border-top:1px solid #e5e7eb;">
                      <p style="margin:0 0 8px;color:#9ca3af;font-size:12px;">Bu butona tıklayamıyor musunuz? Aşağıdaki bağlantıyı tarayıcınıza yapıştırın:</p>
                      <p style="margin:0;color:#6b7280;font-size:12px;word-break:break-all;">{acceptUrl}</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="background:#f9fafb;padding:20px 40px;text-align:center;">
                      <p style="margin:0;color:#9ca3af;font-size:12px;">Bu e-postayı yanlışlıkla aldıysanız görmezden gelebilirsiniz.</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
