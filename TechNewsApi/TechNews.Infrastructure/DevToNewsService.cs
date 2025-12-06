using System.Text.Json;
using System.Text.Json.Serialization;
using TechNews.Core;

namespace TechNews.Infrastructure;

public class DevToNewsService : INewsService
{
    private readonly HttpClient _httpClient;

    public DevToNewsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Article>> GetLatestNewsAsync()
    {
        // Dev.to API requires a User-Agent header
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TechNewsApi");
        }

        var response = await _httpClient.GetAsync("https://dev.to/api/articles?tag=dotnet&top=1");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var dtos = JsonSerializer.Deserialize<List<DevToArticleDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return dtos?.Select(d => new Article(
            d.Id,
            d.Title,
            d.Description,
            d.Url,
            d.CoverImage,
            d.PublishedAt,
            d.User.Name
        )) ?? Enumerable.Empty<Article>();
    }

    private class DevToArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        [JsonPropertyName("cover_image")]
        public string CoverImage { get; set; }
        [JsonPropertyName("published_at")]
        public string PublishedAt { get; set; }
        public DevToUserDto User { get; set; }
    }

    private class DevToUserDto
    {
        public string Name { get; set; }
    }
}
