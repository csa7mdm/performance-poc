using System.Net;
using Moq;
using Moq.Protected;
using TechNews.Core;
using TechNews.Infrastructure;
using Xunit;

namespace TechNews.Tests;

public class NewsServiceTests
{
    [Fact]
    public async Task GetLatestNewsAsync_ReturnsArticles_WhenApiCallIsSuccessful()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"[
                {
                    ""id"": 1,
                    ""title"": ""Test Article"",
                    ""description"": ""Test Description"",
                    ""url"": ""http://test.com"",
                    ""cover_image"": ""http://test.com/image.png"",
                    ""published_at"": ""2023-10-27T10:00:00Z"",
                    ""user"": { ""name"": ""Test Author"" }
                }
            ]")
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new DevToNewsService(httpClient);

        // Act
        var result = await service.GetLatestNewsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var article = result.First();
        Assert.Equal("Test Article", article.Title);
        Assert.Equal("Test Author", article.AuthorName);
    }

    [Fact]
    public async Task GetLatestNewsAsync_ThrowsException_WhenApiCallFails()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new DevToNewsService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetLatestNewsAsync());
    }
}
