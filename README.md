# Aiursoft AptClient

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.com/aiursoft/aptClient/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/aiursoft/aptClient/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/aptClient/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/aiursoft/aptClient/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/aptClient/-/pipelines)
[![NuGet version (Aiursoft.AptClient)](https://img.shields.io/nuget/v/Aiursoft.AptClient.svg)](https://www.nuget.org/packages/Aiursoft.AptClient/)
[![Man hours](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/AptClient.svg)](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/AptClient.html)

Aiursoft.AptClient is a professional .NET library designed for interacting with Debian-style APT repositories. It provides a comprehensive set of tools to parse repository configurations, fetch package indices, and securely download Debian packages with integrity verification.

## Features

- **Source Parsing**: Supports both modern Deb822 (`.sources`) and legacy one-line (`.list`) formats.
- **PPA Support**: Handles PPA configurations, including those with inline GPG keys.
- **Package Index Fetching**: Efficiently retrieves and parses package catalogs from remote repositories.
- **Secure Downloads**: Downloads `.deb` files with automatic SHA256 checksum verification.
- **Extensible**: Designed with abstractions to allow for easy integration and customization.

## Installation

To install `Aiursoft.AptClient` to your project, run the following command:

```bash
dotnet add package Aiursoft.AptClient
```

## Detailed Usage

### 1. Parsing APT Sources

You can parse APT source configurations using `AptSourceExtractor`. The library supports multiple formats:

#### Modern Deb822 Format
```csharp
var deb822 = @"
Types: deb
URIs: http://archive.ubuntu.com/ubuntu/
Suites: jammy
Components: main restricted
Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg
";

var sources = AptSourceExtractor.ExtractSources(deb822, "amd64");
```

#### Legacy One-Line Format
```csharp
var legacy = "deb http://archive.ubuntu.com/ubuntu/ jammy main restricted";
var sources = AptSourceExtractor.ExtractSources(legacy, "amd64");
```

### 2. Fetching Package Lists

Once you have `AptPackageSource` objects, you can fetch the available packages:

```csharp
using Aiursoft.AptClient;

foreach (var source in sources)
{
    // Fetch packages with an optional progress callback
    var packages = await source.FetchPackagesAsync((url, size) => 
    {
        Console.WriteLine($"Downloading {url} ({size} bytes)");
    });

    Console.WriteLine($"Found {packages.Count} packages in {source.Suite}");
}
```

### 3. Downloading and Verifying Packages

Download packages securely with automatic integrity checks:

```csharp
var myPackageInfo = packages.FirstOrDefault(p => p.Package.Package == "curl");

if (myPackageInfo != null)
{
    var package = myPackageInfo.Package;
    var source = myPackageInfo.Source;
    var destinationPath = "curl.deb";

    await source.DownloadPackageAsync(package, destinationPath, (downloaded, total) => 
    {
        var percent = (double)downloaded / total * 100;
        Console.Write($"\rProgress: {percent:F1}%");
    });

    Console.WriteLine("\nDownload complete and verified!");
}
```

## How to Contribute

We welcome contributions! You can help by:
- Logging bugs and reporting issues.
- Submitting pull requests for new features or bug fixes.
- Suggesting improvements or new features through the issue tracker.

Please create a personal fork and use feature branches for your contributions.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.