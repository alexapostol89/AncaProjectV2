using System.Net;
using System.Text.Json;
using api.Configuration;
using api.Services;
using api.Tests.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace api.Tests.Services;

/// <summary>The emails sent at each booking step, checked at the Resend API level (recipients, subject, content).</summary>
public sealed class EmailServiceBookingTests
{
    private const string PracticeInbox = "practice@example.com";
    private readonly FakeHttpHandler _resend = new();
    private readonly DateTimeOffset _start = TestTime.At(TestTime.FutureWorkday(), 14, 10);

    private EmailService CreateService(params string[] bookingRequestTo) => new(
        new HttpClient(_resend),
        Options.Create(new EmailSettings
        {
            ApiKey = "re_test", From = "contact@site.test", FromName = "Cabinet", To = PracticeInbox,
            BookingRequestTo = [.. bookingRequestTo]
        }),
        NullLogger<EmailService>.Instance,
        new StubEnvironment());

    private List<ResendEmail> SentEmails() =>
        _resend.To("emails").Select(r => JsonSerializer.Deserialize<ResendEmail>(r.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!).ToList();

    [Fact]
    public async Task Request_sends_the_practice_a_review_email_and_the_client_an_acknowledgement()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingRequestedAsync("Ioana", "ioana@example.com", _start, "https://site.test/admin/programari");

        var emails = SentEmails();
        Assert.Equal(2, emails.Count);
        Assert.Equal([PracticeInbox], emails[0].To);
        Assert.Equal("ioana@example.com", emails[0].Reply_To);
        Assert.Contains("cerere nouă de programare", emails[0].Subject);
        Assert.Contains("https://site.test/admin/programari", emails[0].Html);
        Assert.Equal(["ioana@example.com"], emails[1].To);
        Assert.Contains("așteaptă confirmarea", emails[1].Html);
    }

    [Fact]
    public async Task New_request_email_goes_to_every_configured_admin_address()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService("admin@clinic.test", "psihologancaalexandra@gmail.com")
            .SendBookingRequestedAsync("Ioana", "ioana@example.com", _start, "https://site.test/admin/programari");

        var practiceEmail = SentEmails()[0];
        Assert.Equal(["admin@clinic.test", "psihologancaalexandra@gmail.com"], practiceEmail.To);
        Assert.DoesNotContain(PracticeInbox, practiceEmail.To);
    }

    [Fact]
    public async Task New_request_email_skips_blank_and_duplicate_addresses()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService("admin@clinic.test", " ", "ADMIN@CLINIC.TEST")
            .SendBookingRequestedAsync("Ioana", "ioana@example.com", _start, "https://site.test/x");

        Assert.Equal(["admin@clinic.test"], SentEmails()[0].To);
    }

    [Fact]
    public async Task New_request_email_falls_back_to_the_practice_inbox_when_no_admin_address_is_set()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingRequestedAsync("Ioana", "ioana@example.com", _start, "https://site.test/x");

        Assert.Equal([PracticeInbox], SentEmails()[0].To);
    }

    [Fact]
    public async Task New_request_subject_names_the_client_without_html_escaping()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingRequestedAsync("Ștefania Mureșan", "s@example.com", _start, "https://site.test/x");

        Assert.EndsWith("Ștefania Mureșan", SentEmails()[0].Subject);
    }

    [Fact]
    public async Task Client_acknowledgement_never_goes_to_the_admin_addresses()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService("admin@clinic.test").SendBookingRequestedAsync("Ioana", "ioana@example.com", _start, "https://site.test/x");

        Assert.Equal(["ioana@example.com"], SentEmails()[1].To);
    }

    [Fact]
    public async Task Emails_show_the_session_time_in_romania_time()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingRequestedAsync("Ioana", "ioana@example.com", _start, "https://site.test/x");

        Assert.All(SentEmails(), e => Assert.Contains(_start.ToString("dd.MM.yyyy") + " 14:10", e.Html));
    }

    [Fact]
    public async Task Request_email_html_encodes_the_client_name()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingRequestedAsync("<script>alert(1)</script>", "x@example.com", _start, "https://site.test/x");

        Assert.All(SentEmails(), e => Assert.DoesNotContain("<script>", e.Html));
    }

    [Fact]
    public async Task Request_throws_when_the_practice_email_fails()
    {
        _resend.On("emails", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().SendBookingRequestedAsync("Ioana", "ioana@example.com", _start, "https://site.test/x"));
    }

    [Fact]
    public async Task Request_does_not_throw_when_only_the_client_acknowledgement_fails()
    {
        var calls = 0;
        _resend.On(_ => ++calls == 1, HttpStatusCode.OK)          // practice email
               .On(_ => true, HttpStatusCode.InternalServerError); // client email

        await CreateService().SendBookingRequestedAsync("Ioana", "ioana@example.com", _start, "https://site.test/x");

        Assert.Equal(2, _resend.Requests.Count);
    }

    [Fact]
    public async Task Approved_email_goes_to_the_client_and_mentions_the_calendar_invitation()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingApprovedAsync("Ioana", "ioana@example.com", _start);

        var email = Assert.Single(SentEmails());
        Assert.Equal(["ioana@example.com"], email.To);
        Assert.Contains("confirmată", email.Subject);
        Assert.Contains("Google Calendar", email.Html);
    }

    [Fact]
    public async Task Rejected_email_includes_the_encoded_reason()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingRejectedAsync("Ioana", "ioana@example.com", _start, "Concediu <3 zile>");

        var email = Assert.Single(SentEmails());
        Assert.Equal(["ioana@example.com"], email.To);
        Assert.Contains("Concediu &lt;3 zile&gt;", email.Html);
    }

    [Fact]
    public async Task Rejected_email_without_reason_has_no_reason_box()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingRejectedAsync("Ioana", "ioana@example.com", _start, null);

        Assert.DoesNotContain("background:#f7f5f0", Assert.Single(SentEmails()).Html);
    }

    [Fact]
    public async Task Emails_are_sent_with_the_resend_api_key()
    {
        _resend.On("emails", HttpStatusCode.OK);

        await CreateService().SendBookingApprovedAsync("Ioana", "ioana@example.com", _start);

        Assert.Equal("Bearer re_test", _resend.Requests[0].Authorization);
    }

    private sealed record ResendEmail(string From, string[] To, string? Reply_To, string Subject, string Html);

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
