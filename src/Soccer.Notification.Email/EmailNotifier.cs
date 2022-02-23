using MimeKit;
using MailKit.Net.Smtp;
using Soccer.Notification.Abstractions;
using Soccer.Notification.Email.Models;

namespace Soccer.Notification.Email;
public class EmailNotifier
    : INotifier
{
    private readonly SmtpConfiguration _smtpConfiguration;

    public EmailNotifier(SmtpConfiguration smtpConfiguration)
    {
        _smtpConfiguration = smtpConfiguration;
    }

    public void Notify(string subject, string message, params string[] destination)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress("OW", _smtpConfiguration.From));
        mimeMessage.To.Add(new MailboxAddress("Clubs", _smtpConfiguration.To));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("plain")
        {
            Text = message
        };

        using var smtpClient = new SmtpClient();
        smtpClient.Connect(_smtpConfiguration.Hostname, _smtpConfiguration.Port, false);
        smtpClient.Send(mimeMessage);
        smtpClient.Disconnect(true);
    }
}
