using PostService.Dtos;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PostService.App;

public class PostServiceClient : IPostServiceClient, IDisposable
{
    private readonly HttpClient client = new()
    {
        BaseAddress = new Uri("http://localhost:5050"),
        Timeout = TimeSpan.FromSeconds(10)
    };
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<PostingGetDto[]> GetPostingsAsync()
    {
        using var response = await client.GetAsync("/postings");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PostingGetDto[]>(options)
            ?? throw new JsonException("Порожня відповідь сервера.");
    }

    public async Task<PostingGetDto> GetPostingByIdAsync(int id)
    {
        using var response = await client.GetAsync($"/postings/{id}");
        await EnsureSuccessAsync(response);
        return await ReadPostingAsync(response);
    }

    public async Task<PostingGetDto> CreatePostingAsync(PostingPostDto posting)
    {
        using var response = await client.PostAsJsonAsync("/postings", posting, options);
        await EnsureSuccessAsync(response);
        return await ReadPostingAsync(response);
    }

    public async Task<PostingGetDto> UpdatePostingAsync(int id, PostingPutDto posting)
    {
        using var response = await client.PutAsJsonAsync($"/postings/{id}", posting, options);
        await EnsureSuccessAsync(response);
        return await ReadPostingAsync(response);
    }

    public async Task DeletePostingAsync(int id)
    {
        using var response = await client.DeleteAsync($"/postings/{id}");
        await EnsureSuccessAsync(response);
    }

    private async Task<PostingGetDto> ReadPostingAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<PostingGetDto>(options)
            ?? throw new JsonException("Порожня відповідь сервера.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            if (problem == null || problem.Errors.Count == 0)
                throw new JsonException("Відсутній перелік помилок валідації.");
            throw new PostingValidationException(problem.Errors);
        }
        throw new HttpRequestException($"Сервер повернув код {(int)response.StatusCode}.", null, response.StatusCode);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
