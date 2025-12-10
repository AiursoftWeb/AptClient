# Aiursoft.AptClient Usage Guide

Aiursoft.AptClient is a .NET library for interacting with Debian-style APT repositories. It allows you to parse `sources.list` configurations, fetch package indices, and download Debian packages (`.deb`) with secure hash verification.

## Installation

Install the package via NuGet:

```bash
dotnet add package Aiursoft.AptClient
```

## 1. Parsing APT Sources

You can parse APT source configurations using `AptSourceExtractor`. The library supports:
1.  **Modern Deb822 Format** (`.sources`)
2.  **Legacy One-Line Format** (`.list`)
3.  **PPA Configurations** (including inline GPG keys)

### Example 1: Modern Deb822 Format
```csharp
var deb822 = @"
Types: deb
URIs: http://mirrors.aliyun.com/ubuntu/
Suites: questing
Components: main restricted universe multiverse
Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg
";

var sources = AptSourceExtractor.ExtractSources(deb822, "amd64");
```

### Example 2: Legacy One-Line Format
```csharp
var legacy = "deb [signed-by=/usr/share/keyrings/ubuntu-archive-keyring.gpg] http://mirrors.aliyun.com/ubuntu/ questing-updates main restricted";
var sources = AptSourceExtractor.ExtractSources(legacy, "amd64");
```

### Example 3: PPA with Inline Keys
```csharp
var ppa = @"Types: deb
URIs: https://mirror-ppa.aiursoft.com/mozillateam/ppa/ubuntu/
Suites: questing
Components: main
Signed-By:
 -----BEGIN PGP PUBLIC KEY BLOCK-----
 ... (key content) ...
 -----END PGP PUBLIC KEY BLOCK-----
";
var sources = AptSourceExtractor.ExtractSources(ppa, "amd64");
```

## 2. Fetching Package Lists

Once you have a list of `AptPackageSource` objects, you can fetch the available packages from the repository. This process includes fetching the `InRelease` file (verifying signatures if implemented) and downloading the compressed `Packages` indices.

```csharp
using Aiursoft.AptClient;
using System.Net.Http;

foreach (var source in sources)
{
    Console.WriteLine($"Fetching packages from {source.ServerUrl} ({source.Suite})...");
    
    try 
    {
        // Fetch packages with an optional progress callback
        var packages = await source.FetchPackagesAsync((url, size) => 
        {
            Console.WriteLine($"Downloading {url} ({size} bytes)");
        });

        Console.WriteLine($"Found {packages.Count} packages.");
        
        // Example: Find a specific package
        var myPackageInfo = packages.FirstOrDefault(p => p.Package.Package == "curl");
        if (myPackageInfo != null)
        {
            Console.WriteLine($"Found curl version: {myPackageInfo.Package.Version}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching from {source.ServerUrl}: {ex.Message}");
    }
}
```

## 3. Downloading Packages

You can download a specific package utilizing the `DownloadPackageAsync` method on the `AptPackageSource`. This method automatically validates the package's SHA256 checksum against the index to ensure strictly secure downloads.

```csharp
// Assuming you have a list of all packages found:
var allPackages = new List<DebianPackageFromApt>();
// ... fetch loop ... 
// allPackages.AddRange(packages);

// Find a specific package from the aggregated list
var myPackageInfo = allPackages.FirstOrDefault(p => p.Package.Package == "curl");

if (myPackageInfo != null)
{
    var package = myPackageInfo.Package;
    var source = myPackageInfo.Source;
    
    var destinationPath = Path.GetFullPath(Path.GetFileName(package.Filename));

    Console.WriteLine($"Downloading {package.Package} to {destinationPath}...");

    await source.DownloadPackageAsync(package, destinationPath, (downloaded, total) => 
    {
        var percent = (double)downloaded / total * 100;
        Console.Write($"\rProgress: {percent:F1}%");
    });

    Console.WriteLine("\nDownload complete and verified!");
}
```

## Summary

The `Aiursoft.AptClient` workflow typically involves:

1.  **Extracting Sources**: logic to convert raw configuration text into `AptPackageSource` objects.
2.  **Fetching Indices**: `FetchPackagesAsync` to get a catalog of all available packages.
3.  **Downloading**: `DownloadPackageAsync` to retrieve and verify actual `.deb` files.

## Running the Sample Application

For a complete, runnable example that demonstrates all these features together, check out the `Aiursoft.AptClient.SampleApp` project in the `src` directory.

```bash
cd src/Aiursoft.AptClient.SampleApp
dotnet run
```
