namespace GameShelf.Business.Services.Moderation
{
    public sealed class ReviewModerationException : Exception
    {
        public int StatusCode { get; }

        public ReviewModerationException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
