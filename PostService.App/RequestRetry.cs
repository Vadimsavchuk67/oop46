using System.Net;
namespace PostService.App;
internal static class RequestRetry
{
    public static async Task<bool> RunAsync(Func<Task> action)
    {
        while (true)
        {
            try
            {
                await action();
                return true;
            }
            catch (HttpRequestException e) when (e.StatusCode == null || e.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                if (!AskRetry()) return false;
            }
            catch (TaskCanceledException)
            {
                if (!AskRetry()) return false;
            }
        }
    }
    private static bool AskRetry()
    {
        return MessageBox.Show("Сервер недоступний або не відповів вчасно. Повторити операцію?",
            "Помилка з’єднання", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry;
    }
}
