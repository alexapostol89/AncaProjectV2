using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Chat;

public sealed class ChatRequest
{
    [Required, StringLength(1000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
    [StringLength(5)]
    public string Language { get; set; } = "en";
    [MaxLength(8)]
    public List<ChatTurn> History { get; set; } = [];
}

public sealed class ChatTurn
{
    [Required]
    public string Role { get; set; } = string.Empty;
    [Required, StringLength(1000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public sealed record ChatResponse(string Reply);
