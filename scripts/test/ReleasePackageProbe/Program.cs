using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

const string expectedVersion = "9.8.7-audit.1";
var root = args.Single();
var artifacts = Path.Combine(root, "artifacts");
Require(Directory.GetFiles(artifacts, "*.nupkg").Length == 2, "Expected exactly two actual nupkg files");
foreach (var id in new[] { "FixPortal.FixAtdl", "FixPortal.FixAtdl.Contracts" })
{
    var file = Path.Combine(artifacts, $"{id}.{expectedVersion}.nupkg");
    Require(File.Exists(file), $"Missing tag-stamped package {file}");
    await using var package = new FileStream(
        file,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        4096,
        useAsync: true
    );
    using var archive = new ZipArchive(package, ZipArchiveMode.Read);
    VerifyMetadata(archive, id);
    VerifyContents(archive, id, root);
    VerifyBinary(archive, id);
    Console.WriteLine($"{id}: actual filename, nuspec, dependency, declared assets and DLL version verified");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static byte[] Read(ZipArchive archive, string name)
{
    using var stream = (
        archive.GetEntry(name) ?? throw new InvalidOperationException($"Missing root asset {name}")
    ).Open();
    using var bytes = new MemoryStream();
    stream.CopyTo(bytes);
    return bytes.ToArray();
}

static void VerifyMetadata(ZipArchive archive, string id)
{
    using var spec = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal)).Open();
    var xml = XDocument.Load(spec);
    string Value(string name) => xml.Descendants().Single(e => e.Name.LocalName == name).Value;
    Require(Value("id") == id && Value("version") == expectedVersion, $"{id}: nuspec id/version mismatch");
    Require(Value("readme") == "README.md" && Value("license") == "MIT", $"{id}: readme/license metadata mismatch");
    if (id.EndsWith("Contracts", StringComparison.Ordinal))
    {
        var dependency = xml.Descendants()
            .Single(e => e.Name.LocalName == "dependency" && (string?)e.Attribute("id") == "FixPortal.FixAtdl");
        Require(
            (string?)dependency.Attribute("version") == expectedVersion,
            "Contracts core dependency version mismatch"
        );
    }
}

static void VerifyContents(ZipArchive archive, string id, string root)
{
    var readme = id.EndsWith("Contracts", StringComparison.Ordinal)
        ? "src/FixPortal.FixAtdl.Contracts/README.md"
        : "README.md";
    foreach (var (entry, source) in new[] { ("README.md", readme), ("LICENSE", "LICENSE") })
    {
        Require(
            Read(archive, entry).SequenceEqual(File.ReadAllBytes(Path.Combine(root, source))),
            $"{id}: {entry} bytes differ from declared source"
        );
    }
    if (id == "FixPortal.FixAtdl")
    {
        Require(
            Read(archive, "NOTICE").SequenceEqual(File.ReadAllBytes(Path.Combine(root, "NOTICE"))),
            "Core NOTICE mismatch"
        );
    }
}

static void VerifyBinary(ZipArchive archive, string id)
{
    using var bytes = new MemoryStream(Read(archive, $"lib/net10.0/{id}.dll"));
    using var binary = new PEReader(bytes);
    var version = binary.GetMetadataReader().GetAssemblyDefinition().Version;
    Require(
        version == new Version(9, 8, 7, 0),
        $"{id}: packaged DLL version {version} is stale despite stamped nuspec"
    );
}
