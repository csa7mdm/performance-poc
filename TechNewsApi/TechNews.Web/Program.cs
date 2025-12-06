using TechNews.Core;
using TechNews.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient<INewsService, DevToNewsService>();

// Add Swagger for easy testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/news", async (INewsService newsService) =>
{
    try
    {
        var news = await newsService.GetLatestNewsAsync();
        return Results.Ok(news);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetTechNews")
.WithOpenApi();

app.Run();
