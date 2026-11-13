namespace PostService.App;
public class PostingValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }
    public PostingValidationException(Dictionary<string, string[]> errors)
        : base("Сервер відхилив дані відправлення.")
    {
        Errors = errors;
    }
}
public class ValidationErrorResponse
{
    public Dictionary<string, string[]> Errors { get; set; } = new();
}
