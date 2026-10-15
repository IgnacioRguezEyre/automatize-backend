using System.Buffers.Text;
using System.Text;
using Google.Apis.Gmail.v1.Data;
using Integraciones.Infrastructure.Gmail;

namespace Integraciones.Tests.Infrastructure;

public class GmailMessageMapperTests
{
    private static string Encode(string text) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(text));

    private static Message Sample() => new()
    {
        Id = "18f1",
        InternalDate = 1792051991000, // 2026-10-15 08:13:11 UTC
        Payload = new MessagePart
        {
            MimeType = "multipart/mixed",
            Headers =
            [
                new MessagePartHeader { Name = "From", Value = "Alertas de empleo de LinkedIn <jobalerts-noreply@linkedin.com>" },
                new MessagePartHeader { Name = "Subject", Value = "Nuevas ofertas" }
            ],
            Parts =
            [
                new MessagePart
                {
                    MimeType = "multipart/alternative",
                    Parts =
                    [
                        new MessagePart { MimeType = "text/plain", Body = new MessagePartBody { Data = Encode("Texto con tildes: Híbrido") } },
                        new MessagePart { MimeType = "text/html", Body = new MessagePartBody { Data = Encode("<p>Híbrido</p>") } }
                    ]
                },
                new MessagePart { MimeType = "text/plain", Filename = "adjunto.txt", Body = new MessagePartBody { AttachmentId = "a1" } }
            ]
        }
    };

    [Fact]
    public void Maps_headers_date_and_nested_bodies()
    {
        var mail = GmailMessageMapper.ToEmailMessage(Sample());

        Assert.Equal("18f1", mail.Id);
        Assert.Equal("jobalerts-noreply@linkedin.com", mail.From);
        Assert.Equal("Nuevas ofertas", mail.Subject);
        Assert.Equal(new DateTimeOffset(2026, 10, 15, 8, 13, 11, TimeSpan.Zero), mail.ReceivedAt);
        Assert.Equal("Texto con tildes: Híbrido", mail.TextBody);
        Assert.Equal("<p>Híbrido</p>", mail.HtmlBody);
    }

    [Fact]
    public void Single_part_html_email_has_no_text_body()
    {
        var message = new Message
        {
            Id = "1",
            Payload = new MessagePart
            {
                MimeType = "text/html",
                Headers = [new MessagePartHeader { Name = "From", Value = "noreply@glassdoor.com" }],
                Body = new MessagePartBody { Data = Encode("<b>Oferta</b>") }
            }
        };

        var mail = GmailMessageMapper.ToEmailMessage(message);

        Assert.Null(mail.TextBody);
        Assert.Equal("<b>Oferta</b>", mail.HtmlBody);
        Assert.Equal("noreply@glassdoor.com", mail.From);
    }

    [Theory]
    [InlineData("\"tecnoempleo.com\" <alertas@push.tecnoempleo.com>", "alertas@push.tecnoempleo.com")]
    [InlineData("ofertas@push.infojobs.net", "ofertas@push.infojobs.net")]
    [InlineData(null, "")]
    public void Extracts_the_sender_address(string? header, string expected) =>
        Assert.Equal(expected, GmailMessageMapper.AddressOf(header));
}
