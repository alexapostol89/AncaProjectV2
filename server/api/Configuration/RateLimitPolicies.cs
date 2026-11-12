namespace api.Configuration;

/// <summary>Names of the rate-limit policies registered in Program.cs (all per client IP).</summary>
public static class RateLimitPolicies
{
    /// <summary>Contact form: 5 requests / minute.</summary>
    public const string Contact = "contact";

    /// <summary>Login, register, password reset: 10 requests / minute. Slows down password guessing and spam.</summary>
    public const string Auth = "auth";

    /// <summary>Booking reads/writes: 10 requests / minute per IP.</summary>
    public const string Booking = "booking";
}
