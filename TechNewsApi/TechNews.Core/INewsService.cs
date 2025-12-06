namespace TechNews.Core;

public interface INewsService
{
    Task<IEnumerable<Article>> GetLatestNewsAsync();
}
