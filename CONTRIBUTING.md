# Contributing to Performance POC

Thank you for your interest in contributing to this performance comparison project!

## How to Contribute

1.  **Fork the repository** on GitHub.
2.  **Clone your fork** locally.
3.  **Create a branch** for your feature or fix.
4.  **Make your changes**.
5.  **Run the Functional Tests** to ensure no regressions:
    ```bash
    dotnet run -c Release --project PerformancePoc.Benchmarks -- test
    ```
6.  **Run the Benchmarks** if your change affects performance:
    ```bash
    dotnet run -c Release --project PerformancePoc.Benchmarks
    ```
7.  **Commit and Push** your changes.
8.  **Submit a Pull Request**.

## Reporting Issues

If you find a bug or have a suggestion, please open an issue in the [Issues](https://github.com/csa7mdm/performance-poc/issues) tab.
