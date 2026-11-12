using System.Net;
using System.Text;

namespace api.Tests.Infrastructure;

/// <summary>
/// Fake HTTP backend for services that call external APIs (Google, Resend).
/// Every request is recorded with its body and answered by the first matching route.
/// </summary>
public sealed class FakeHttpHandler : HttpMessageHandler
{
    public record Recorded(HttpMethod Method, Uri Uri, string Body, string? Authorization);

    private readonly List<(Func<HttpRequestMessage, bool> Match, HttpStatusCode Status, string Json)> _routes = [];

    public List<Recorded> Requests { get; } = [];

    /// <summary>Answers requests whose URL contains <paramref name="urlPart"/>.</summary>
    public FakeHttpHandler On(string urlPart, HttpStatusCode status, string json = "{}") =>
        On(r => r.RequestUri!.ToString().Contains(urlPart), status, json);

    public FakeHttpHandler On(Func<HttpRequestMessage, bool> match, HttpStatusCode status, string json = "{}")
    {
        _routes.Add((match, status, json));
        return this;
    }

    public List<Recorded> To(string urlPart) => Requests.Where(r => r.Uri.ToString().Contains(urlPart)).ToList();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new Recorded(request.Method, request.RequestUri!, body, request.Headers.Authorization?.ToString()));
        foreach (var (match, status, json) in _routes)
        {
            if (match(request))
                return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
        return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent($"No fake route for {request.RequestUri}") };
    }
}
