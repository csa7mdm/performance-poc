namespace TechNews.Core;

public record Article(
    int Id,
    string Title,
    string Description,
    string Url,
    string CoverImage,
    string PublishedAt,
    string AuthorName
);
