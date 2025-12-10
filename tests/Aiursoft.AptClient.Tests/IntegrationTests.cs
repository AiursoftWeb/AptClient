using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aiursoft.AptClient;
using Aiursoft.AptClient.Abstractions;

namespace Aiursoft.AptClient.Tests;

[TestClass]
public class IntegrationTests
{
    [TestMethod]
    public async Task TestFetchFromAliyunDeb822()
    {
        // Arrange
        // Using "jammy" (Ubuntu 22.04) as a stable target
        var deb822 = @"
Types: deb
URIs: http://mirrors.aliyun.com/ubuntu/
Suites: jammy
Components: main
Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg
";
        var sources = AptSourceExtractor.ExtractSources(deb822, "amd64");
        Assert.IsTrue(sources.Count > 0);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Aiursoft.AptClient.Tests");

        // Act
        var allPackages = new List<DebianPackageFromApt>();
        foreach (var source in sources)
        {
            // We pass null for progress to reduce noise, or we could verify it matches
            var packages = await source.FetchPackagesAsync(client);
            allPackages.AddRange(packages);
        }

        // Assert
        Assert.IsTrue(allPackages.Count > 1000, $"Expected > 1000 packages, found {allPackages.Count}");

        var bash = allPackages.FirstOrDefault(p => p.Package.Package == "bash");
        Assert.IsNotNull(bash, "Should find bash package");
        Assert.IsFalse(string.IsNullOrWhiteSpace(bash.Package.Version));
        Assert.IsFalse(string.IsNullOrWhiteSpace(bash.Package.Filename));
        Assert.IsFalse(string.IsNullOrWhiteSpace(bash.Package.SHA256));
    }

    [TestMethod]
    public void TestParseLegacyFormat()
    {
        var line = "deb [signed-by=/usr/share/keyrings/ubuntu-archive-keyring.gpg] http://mirrors.aliyun.com/ubuntu/ jammy main restricted";
        var sources = AptSourceExtractor.ExtractSources(line, "amd64");

        Assert.AreEqual(2, sources.Count); // main and restricted
        Assert.AreEqual("jammy", sources[0].Suite);
        Assert.AreEqual("http://mirrors.aliyun.com/ubuntu/", sources[0].ServerUrl);
        // Unable to access SignedBy directly without reflection, skipping for now
    }

    [TestMethod]
    public async Task TestDownloadPackage()
    {
        // Arrange
        var deb822 = @"
Types: deb
URIs: http://mirrors.aliyun.com/ubuntu/
Suites: jammy
Components: main
Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg
";
        var sources = AptSourceExtractor.ExtractSources(deb822, "amd64");
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Aiursoft.AptClient.Tests");

        // Use the first source
        var source = sources.First();
        var packages = await source.FetchPackagesAsync(client);

        // Find a small package
        var pkgInfo = packages.FirstOrDefault(p => p.Package.Package == "hostname");
        Assert.IsNotNull(pkgInfo, "Should find hostname package");

        var tempFile = Path.GetTempFileName();

        // Act
        try
        {
            await source.DownloadPackageAsync(pkgInfo.Package, tempFile, client);

            // Assert
            Assert.IsTrue(File.Exists(tempFile));
            Assert.IsTrue(new FileInfo(tempFile).Length > 0);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
