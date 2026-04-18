namespace GameShelf.Business.Services.Moderation
{
    public sealed class ModerationOutcome
    {
        public bool ContainsProfanity { get; init; }
        public bool IsTimedOut { get; init; }
        public string SanitizedText { get; init; } = string.Empty;
        public string? UserMessage { get; init; }
    }
}
