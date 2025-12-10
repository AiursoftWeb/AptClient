# Aiursoft AptClient

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.com/aiursoft/aptClient/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/aiursoft/aptClient/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/aptClient/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/aiursoft/aptClient/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/aptClient/-/pipelines)
[![NuGet version (Aiursoft.AptClient)](https://img.shields.io/nuget/v/Aiursoft.AptClient.svg)](https://www.nuget.org/packages/Aiursoft.AptClient/)
[![Man hours](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/AptClient.svg)](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/AptClient.html)

A class lib that helps you manage apt packages from apt source.

## How to install

To install `Aiursoft.AptClient` to your project, just run:

```bash
dotnet add package Aiursoft.AptClient
```

## Basic Usage

```csharp
using Aiursoft.AptClient;

var source = "deb http://archive.ubuntu.com/ubuntu/ jammy main";
var sources = AptSourceExtractor.ExtractSources(source, "amd64");

using var http = new HttpClient();
foreach (var aptSource in sources)
{
    var packages = await aptSource.FetchPackagesAsync(http);
    Console.WriteLine($"Found {packages.Count} packages in {aptSource.Suite}");
}
```

For detailed usage, please refer to [usage.md](docs/usage.md).

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you with push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
