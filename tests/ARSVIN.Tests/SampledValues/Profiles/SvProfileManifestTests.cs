using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvProfileManifestTests
{
    [Fact]
    public void TrustedManifestRoundTripsAndBuildsDeterministicCatalog()
    {
        var document = BuildDocument("lab-pack", "profile-lab", SvProfileEvidenceStatus.VerifiedLab);

        var json = SvProfileManifestSerializer.ToJson(document);
        var loaded = SvProfileManifestSerializer.FromJson(
            json,
            new SvProfileManifestLoadOptions
            {
                TrustLevel = SvProfileManifestTrustLevel.TrustedRepository
            });
        var catalog = SvProfileCatalogComposer.Compose(
            SvProfileCatalog.BuiltIn,
            [loaded],
            new SvProfileManifestLoadOptions
            {
                TrustLevel = SvProfileManifestTrustLevel.TrustedRepository
            });

        Assert.Equal("lab-pack", loaded.Document.ManifestId);
        Assert.Empty(loaded.Diagnostics);
        Assert.Contains(loaded.Profiles[0].Sources, source => source.SourceId == "manifest:lab-pack");
        Assert.Equal("generic-scl-layer2", catalog[0].Id);
        Assert.Equal("profile-lab", catalog[1].Id);
    }

    [Fact]
    public void UntrustedManifestCannotSelfAssertVerifiedEvidence()
    {
        var json = SvProfileManifestSerializer.ToJson(
            BuildDocument("external-pack", "external-profile", SvProfileEvidenceStatus.VerifiedLab));

        var loaded = SvProfileManifestSerializer.FromJson(json);
        var profile = Assert.Single(loaded.Profiles);

        Assert.Equal(SvProfileEvidenceStatus.ResearchCandidate, profile.EvidenceStatus);
        Assert.All(profile.Sources, source =>
            Assert.Equal(SvProfileEvidenceStatus.ResearchCandidate, source.Status));
        Assert.Contains(loaded.Diagnostics, diagnostic =>
            diagnostic.Contains("reduced from VerifiedLab", StringComparison.Ordinal));
    }

    [Fact]
    public void ManifestRejectsDuplicateProfileIdsCaseInsensitively()
    {
        var first = BuildProfile("duplicate", SvProfileEvidenceStatus.ResearchCandidate);
        var document = new SvProfileManifestDocument
        {
            ManifestId = "duplicate-pack",
            DisplayName = "Duplicate pack",
            Profiles = [first, first with { Id = "DUPLICATE" }]
        };

        var error = Assert.Throws<InvalidDataException>(() =>
            SvProfileManifestSerializer.ToJson(document));

        Assert.Contains("duplicate profile IDs", error.Message);
    }

    [Fact]
    public void ExternalManifestCannotReplaceBuiltInProfile()
    {
        var document = BuildDocument(
            "collision-pack",
            SvProfileCatalog.GenericSclLayer2.Id,
            SvProfileEvidenceStatus.ResearchCandidate);
        var loaded = SvProfileManifestSerializer.FromJson(
            SvProfileManifestSerializer.ToJson(document));

        var error = Assert.Throws<InvalidDataException>(() =>
            SvProfileCatalogComposer.Compose(SvProfileCatalog.BuiltIn, [loaded]));

        Assert.Contains("collides with an existing catalog profile", error.Message);
    }

    [Fact]
    public void ManifestProfileCanBeEvaluatedWithoutVendorSpecificLogic()
    {
        var loaded = SvProfileManifestSerializer.FromJson(
            SvProfileManifestSerializer.ToJson(
                BuildDocument("candidate-pack", "candidate-4i4u", SvProfileEvidenceStatus.VerifiedCapture)));
        var profile = Assert.Single(loaded.Profiles);
        var facts = new SvObservedStreamFacts
        {
            EtherType = 0x88BA,
            AsduPerFrame = 1,
            PayloadBytesPerAsdu = 64,
            ObservedSamplesPerSecond = 4_000,
            NominalFrequencyHz = 50,
            ObservedCounterWrap = 4_000,
            DataSetSignature = BuildSignature()
        };

        var result = new SvProfileDetector().Evaluate(facts, profile);

        Assert.Equal(100, result.ScorePercent);
        Assert.Equal(SvProfileConfidence.Confirmed, result.RawConfidence);
        Assert.Equal(SvProfileConfidence.Possible, result.Confidence);
        Assert.Equal(SvProfileEvidenceStatus.ResearchCandidate, result.Profile.EvidenceStatus);
    }

    [Fact]
    public void ManifestRejectsUnsupportedSchemaAndExcessiveInput()
    {
        var document = BuildDocument("schema-pack", "schema-profile", SvProfileEvidenceStatus.ResearchCandidate)
            with { SchemaVersion = "arsvin.sv-profile-manifest/v999" };
        var json = System.Text.Json.JsonSerializer.Serialize(document);

        var schemaError = Assert.Throws<InvalidDataException>(() =>
            SvProfileManifestSerializer.FromJson(json));
        Assert.Contains("Unsupported SV profile manifest schema", schemaError.Message);

        var sizeError = Assert.Throws<InvalidDataException>(() =>
            SvProfileManifestSerializer.FromJson(
                new string('x', 2_048),
                new SvProfileManifestLoadOptions { MaximumJsonBytes = 1_024 }));
        Assert.Contains("configured limit", sizeError.Message);
    }

    private static SvProfileManifestDocument BuildDocument(
        string manifestId,
        string profileId,
        SvProfileEvidenceStatus status)
        => new()
        {
            ManifestId = manifestId,
            DisplayName = $"Manifest {manifestId}",
            Description = "Deterministic test manifest.",
            CreatedAt = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero),
            Profiles = [BuildProfile(profileId, status)]
        };

    private static SvProfileDefinition BuildProfile(
        string id,
        SvProfileEvidenceStatus status)
        => new()
        {
            Id = id,
            DisplayName = $"Profile {id}",
            Family = "Fixed 4I + 4V candidate",
            SamplingBasis = SvSamplingBasis.SamplesPerCycle,
            ExpectedEtherType = 0x88BA,
            AllowedAsduPerFrame = [1],
            ExpectedPayloadBytesPerAsdu = 64,
            ExpectedDataSetElementCount = 16,
            ExpectedDataSetSignature = BuildSignature(),
            ExpectedSamplesPerCycle = 80,
            AllowedNominalFrequenciesHz = [50, 60],
            ExpectedCounterWrap = 4_000,
            RateTolerancePercent = 1,
            EvidenceStatus = status,
            Sources =
            [
                new SvProfileSourceEvidence(
                    "test-evidence",
                    "Deterministic regression evidence for manifest parsing.",
                    status)
            ]
        };

    private static IReadOnlyList<SvDatasetElementSignature> BuildSignature()
    {
        var signature = new List<SvDatasetElementSignature>();
        for (var index = 0; index < 8; index++)
        {
            signature.Add(new SvDatasetElementSignature
            {
                BType = "INT32",
                Cdc = index < 4 ? "SAV-Current" : "SAV-Voltage"
            });
            signature.Add(new SvDatasetElementSignature
            {
                BType = "Quality",
                Cdc = index < 4 ? "SAV-Current" : "SAV-Voltage",
                IsQuality = true
            });
        }
        return signature;
    }
}
