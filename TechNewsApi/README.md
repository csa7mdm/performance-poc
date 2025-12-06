# TechNews Minimal API

A simple .NET Minimal API that fetches the latest .NET tech news from Dev.to.

## Structure

- **TechNews.Core**: Domain models and interfaces.
- **TechNews.Infrastructure**: Implementation of services (Dev.to API client).
- **TechNews.Web**: The Minimal API application.
- **TechNews.Tests**: Unit tests using xUnit and Moq.

## How to Run

1.  Navigate to the solution directory:
    ```bash
    cd TechNewsApi
    ```

2.  Run the Web API:
    ```bash
    dotnet run --project TechNews.Web
    ```

3.  Access the Swagger UI at `http://localhost:5000/swagger` (or the port shown in the console).

4.  Test the endpoint:
    ```bash
    curl http://localhost:5000/news
    ```

## Running Tests

```bash
dotnet test
```
