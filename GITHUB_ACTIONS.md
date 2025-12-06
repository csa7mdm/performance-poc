# Understanding the GitHub Actions Workflow

**GitHub Actions** is a CI/CD (Continuous Integration / Continuous Deployment) platform built directly into GitHub. It allows you to automate your build, test, and deployment workflows.

In this project, the workflow file located at `.github/workflows/ci.yml` defines exactly what happens every time code is pushed. Here is a detailed breakdown of how it works.

## 1. The Trigger (`on`)

```yaml
on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]
```

*   **What it does:** This section tells GitHub to run this workflow *only* when:
    *   Code is pushed directly to the `main` branch.
    *   A Pull Request is opened targeting the `main` branch.
*   **Why:** This ensures that every proposed change is automatically tested *before* it gets merged or becomes official, preventing broken code from entering the codebase.

## 2. The Job Environment (`jobs` & `runs-on`)

```yaml
jobs:
  build-and-test:
    runs-on: ubuntu-latest
```

*   **What it does:** Defines a job named `build-and-test`. GitHub spins up a fresh, isolated virtual machine running **Ubuntu Linux** to execute your commands.

## 3. Service Containers (The "Magic" Part)

This section is critical because the tests verify functionality against real PostgreSQL and RabbitMQ instances.

```yaml
    services:
      postgres:
        image: postgres:alpine
        env:
          POSTGRES_PASSWORD: postgres
        ports:
          - 5432:5432
        # ... health checks ...
      
      rabbitmq:
        image: rabbitmq:3-management-alpine
        ports:
          - 5672:5672
        # ... health checks ...
```

*   **What it does:** Before running any of the project code, GitHub pulls these Docker images and starts them up as "service containers".
*   **Why:** This gives the Ubuntu runner "sidecar" databases. When the C# code tries to connect to `localhost:5432` (Postgres) or `localhost:5672` (RabbitMQ), these services are already running and listening, replicating the environment created by `docker compose up` on a local machine.

## 4. The Steps

The steps run sequentially on the Ubuntu machine after the services are ready.

### Step A: Checkout Code
```yaml
    - uses: actions/checkout@v4
```
*   Downloads the code from the repository onto the runner.

### Step B: Setup .NET
```yaml
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.0.x
```
*   Installs the .NET 9 SDK so that `dotnet` commands can be executed.

### Step C: Restore & Build
```yaml
    - name: Restore dependencies
      run: dotnet restore PerformancePoc.sln

    - name: Build
      run: dotnet build PerformancePoc.sln --no-restore --configuration Release
```
*   Downloads NuGet packages and compiles the C# code. If there are syntax errors or missing dependencies, the workflow fails here.

### Step D: Run Verification Tests
```yaml
    - name: Run Functional Tests
      run: dotnet run --project PerformancePoc.Benchmarks --configuration Release -- test
```
*   This executes the `FunctionalTest.cs` logic.
*   It attemps to insert data into Postgres and publish to RabbitMQ.
*   Because of the `services` section configuration, the tests successfully connect to `localhost` and pass.

## Summary of Benefits

1.  **Immediate Feedback**: If a commit breaks the message retry logic, the author receives a notification immediately.
2.  **Consistency**: Tests run in a clean, standardized environment every time, avoiding "it works on my machine" issues.
3.  **Confidence**: The "Passing" badge on the README provides immediate visual confirmation that the codebase is currently healthy.
