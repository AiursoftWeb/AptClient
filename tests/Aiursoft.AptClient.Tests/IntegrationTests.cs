using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aiursoft.AptClient;
using Aiursoft.AptClient.Abstractions;

namespace Aiursoft.AptClient.Tests;

[TestClass]
public class IntegrationTests
{

    private readonly string IntegratedSigned =
            @"Types: deb
URIs: https://mirror-ppa.aiursoft.com/mozillateam/ppa/ubuntu/
Suites: questing
Components: main
Signed-By:
 -----BEGIN PGP PUBLIC KEY BLOCK-----
 .
 mQINBGYov84BEADSrLhiWvqL3JJ3fTxjCGD4+viIUBS4eLSc7+Q7SyHm/wWfYNwT
 EqEvMMM9brWQyC7xyE2JBlVk5/yYHkAQz3f8rbkv6ge3J8Z7G4ZwHziI45xJKJ0M
 9SgJH24WlGxmbbFfK4SGFNlg9x1Z0m5liU3dUSfhvTQdmBNqwRCAjJLZSiS03IA0
 56V9r3ACejwpNiXzOnTsALZC2viszGiI854kqhUhFIJ/cnWKSbAcg6cy3ZAsne6K
 vxJVPsdEl12gxU6zENZ/4a4DV1HkxIHtpbh1qub1lhpGR41ZBXv+SQhwuMLFSNeu
 UjAAClC/g1pJ0gzI0ko1vcQFv+Q486jYY/kv+k4szzcB++nLILmYmgzOH0NEqT57
 XtdiBWhlb6oNfF/nYZAaToBU/QjtWXq3YImG2NiCUrCj9zAKHdGUsBU0FxN7HkVB
 B8aF0VYwB0I2LRO4Af6Ry1cqMyCQnw3FVh0xw7Vz4gQ57acUYeAJpT68q8E2XcUx
 riEP65/MBPoFlANLVMSrnsePEXmVzdysmXKnFVefeQ4E3dIDufXUIhrfmL1pMdTG
 anhmDEjY7I3pQQQIaLpnNhhSDZKDSk9C/Ax/8gEUgnnmd6BwZxh8Q7oDXcm2tyeu
 n2m9wCZI/eJI9P9G8ON8AkKvG4xFR+eqhowwzu7TLDr3feliG+UN+mJ8jwARAQAB
 tB5MYXVuY2hwYWQgUFBBIGZvciBNb3ppbGxhIFRlYW2JAk4EEwEKADgWIQRzi+uT
 IdGq7BPqk5GuvfSBm+IYZwUCZii/zgIbAwULCQgHAgYVCgkICwIEFgIDAQIeAQIX
 gAAKCRCuvfSBm+IYZ38/D/46eEIyG7Gb65sxt3QnlIN0+90kUjz83QpCnIyALZDc
 H2wPYBCMbyJFMG+rqVE8Yoh6WF0Rqy76LG+Y/xzO9eKIJGxVcSU75ifoq/M7pI1p
 aiqA9T8QcFBmo83FFoPvnid67aqg/tFsHl+YF9rUxMZndGRE9Hk96lkH1Y2wHMEs
 mAa582RELVEDDD2ellOPmQr69fRPa5IdJHkXjqGtoNQy5hAp49ofMLmeQ82d2OA+
 kpzgiuSw8Nh1VrMZludcUArSQDCHoXuiPG/7Wn9Vy6fvKkTQK3mCW8i5HgCa0qxe
 vOKlDMz4virEEADMBs79iIyM6w1xm8JOD4734sgii2MPcQgmAlbu5LyBM5FfuO0u
 rTMvZM0btSWQX3nIsxQ3far9MJvUT4nebhTo59cED+1EjkD14mReTHwtWt1aye/b
 I8Rvor15RFiB8Ku6c41YmNKarSCzJDs4VEfsos4oMieEqA98J4ZOX67IT++ortcB
 uXmDJgvzGWEeyVOMoc/4oDJHNQjJg9XRGy8b/J3AVhk2BE/CD4lKhX3hWGbufrQz
 E8ENWuT4m3igQnBmOsrGlBPYIOKZvczQxri01vcKY95dKXb1jtnR9yR+JKgEP388
 1B/8dEohynhMnzEqR9TIMEEy9Y8RKZ+Jiy+/Lg2XGrChiLsouUetfMQww6BTK+++
 pw==
 =tIux
 -----END PGP PUBLIC KEY BLOCK-----
";

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
    [TestMethod]
    public async Task TestFetchFromIntegratedSigned()
    {
        // this.IntegratedSigned is already defined in the class
        var sources = AptSourceExtractor.ExtractSources(IntegratedSigned, "amd64");
        Assert.IsTrue(sources.Count > 0);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Aiursoft.AptClient.Tests");

        var allPackages = new List<DebianPackageFromApt>();
        foreach (var source in sources)
        {
            try
            {
                var packages = await source.FetchPackagesAsync(client);
                allPackages.AddRange(packages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching from {source.ServerUrl}: {ex.Message}");
                // PPAs might be unstable or require specific keys, but the parsing of the block should work.
                // If it fails to verify signature because of the key block, we expect the code to handle it?
                // Actually if Signed-By is present, it should try to use it.
                // The IntegratedSigned block contains a full PGP key block.
                // AptSourceExtractor should parse it and save it to a temp file, then use it.
                throw;
            }
        }

        // PPA usually has fewer packages
        Assert.IsTrue(allPackages.Count > 0, "No packages found in PPA");

        // Just verify any package exist
        var firstPkg = allPackages.First();
        Assert.IsFalse(string.IsNullOrWhiteSpace(firstPkg.Package.Package));
    }
}
