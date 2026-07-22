using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AR.Iec61850.SampledValues.Profiles;

public enum SvProfileManifestTrustLevel
{
    UntrustedExternal,
    ReviewedEngineering,
    TrustedRepository
}

public sealed record SvProfileManifestLoadOptions
{
    public int MaximumJsonBytes { get; init; } = 1_048_576;
    public int MaximumProfiles { get; init; } = 64;
    public int MaximumSourcesPerProfile { get; init; } = 16;
    public SvProfileManifestTrustLevel TrustLevel { get; init; } = SvProfileManifestTrustLevel.UntrustedExternal;
    public bool AllowBuiltInProfileReplacement { get; init; }

    public void Validate()
    {
        if (MaximumJsonBytes is < 1_024 or > 16_777_216)
            throw new InvalidOperationException("SV profile manifest JSON limit must be between 1 KiB and 16 MiB.");
        if (MaximumProfiles is < 1 or > 1_024)
            throw new InvalidOperationException("SV profile manifest profile limit must be between 1 and 1024.");
        if (MaximumSourcesPerProfile is < 1 or > 256)
            throw new InvalidOperationException("SV profile manifest source limit must be between 1 and 256.");
        if (AllowBuiltInProfileReplacement && TrustLevel != SvProfileManifestTrustLevel.TrustedRepository)
        {
            throw new InvalidOperationException(
                "Built-in profile replacement is allowed only for trusted-repository manifests.");
        }
    }
}

public sealed record SvProfileManifestDocument
{
    public const string CurrentSchemaVersion = "arsvin.sv-profile-manifest/v1";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string ManifestId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<SvProfileDefinition> Profiles { get; init; }
        = Array.Empty<SvProfileDefinition>();

    public void Validate(SvProfileManifestLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
            throw new InvalidDataException($"Unsupported SV profile manifest schema '{SchemaVersion}'.");
        if (string.IsNullOrWhiteSpace(ManifestId))
            throw new InvalidDataException("SV profile manifest requires a stable manifestId.");
        if (!IsSafeIdentifier(ManifestId))
            throw new InvalidDataException("SV profile manifestId may contain only letters, digits, '.', '-', and '_'.");
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new InvalidDataException($"SV profile manifest '{ManifestId}' requires a displayName.");
        if (Profiles.Count == 0)
            throw new InvalidDataException($"SV profile manifest '{ManifestId}' contains no profiles.");
        if (Profiles.Count > options.MaximumProfiles)
        {
            throw new InvalidDataException(
                $"SV profile manifest '{ManifestId}' contains {Profiles.Count} profiles; the configured limit is {options.MaximumProfiles}.");
        }

        var duplicateIds = Profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Id))
            .GroupBy(profile => profile.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (duplicateIds.Length > 0)
        {
            throw new InvalidDataException(
                $"SV profile manifest '{ManifestId}' contains duplicate profile IDs: {string.Join(", ", duplicateIds)}.");
        }

        foreach (var profile in Profiles)
        {
            try
            {
                profile.Validate();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException(
                    $"SV profile manifest '{ManifestId}' profile '{profile.Id}' is invalid: {ex.Message}",
                    ex);
            }

            if (!IsSafeIdentifier(profile.Id))
                throw new InvalidDataException($"SV profile ID '{profile.Id}' contains unsupported characters.");
            if (profile.Sources.Count > options.MaximumSourcesPerProfile)
            {
                throw new InvalidDataException(
                    $"SV profile '{profile.Id}' contains {profile.Sources.Count} evidence sources; the configured limit is {options.MaximumSourcesPerProfile}.");
            }
        }
    }

    private static bool IsSafeIdentifier(string value)
        => value.All(character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_');
}

public sealed record SvProfileManifestLoadResult
{
    public SvProfileManifestDocument Document { get; init; } = new();
    public SvProfileManifestTrustLevel TrustLevel { get; init; }
    public IReadOnlyList<SvProfileDefinition> Profiles { get; init; }
        = Array.Empty<SvProfileDefinition>();
    public IReadOnlyList<string> Diagnostics { get; init; }
        = Array.Empty<string>();
}

public static class SvProfileManifestSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static string ToJson(SvProfileManifestDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var options = new SvProfileManifestLoadOptions
        {
            TrustLevel = SvProfileManifestTrustLevel.TrustedRepository
        };
        document.Validate(options);
        return JsonSerializer.Serialize(document, JsonOptions);
    }

    public static SvProfileManifestLoadResult FromJson(
        string json,
        SvProfileManifestLoadOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("SV profile manifest JSON is empty.");

        options ??= new SvProfileManifestLoadOptions();
        options.Validate();
        var byteCount = Encoding.UTF8.GetByteCount(json);
        if (byteCount > options.MaximumJsonBytes)
        {
            throw new InvalidDataException(
                $"SV profile manifest is {byteCount:N0} bytes; the configured limit is {options.MaximumJsonBytes:N0} bytes.");
        }

        SvProfileManifestDocument document;
        try
        {
            document = JsonSerializer.Deserialize<SvProfileManifestDocument>(json, JsonOptions)
                ?? throw new InvalidDataException("SV profile manifest JSON did not contain a document.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"SV profile manifest JSON is invalid: {ex.Message}", ex);
        }

        document.Validate(options);
        var diagnostics = new List<string>();
        var profiles = document.Profiles
            .Select(profile => NormalizeProfile(document, profile, options.TrustLevel, diagnostics))
            .OrderBy(profile => profile.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SvProfileManifestLoadResult
        {
            Document = document,
            TrustLevel = options.TrustLevel,
            Profiles = profiles,
            Diagnostics = diagnostics
        };
    }

    private static SvProfileDefinition NormalizeProfile(
        SvProfileManifestDocument document,
        SvProfileDefinition profile,
        SvProfileManifestTrustLevel trustLevel,
        ICollection<string> diagnostics)
    {
        var maximumStatus = MaximumEvidenceStatus(trustLevel);
        var normalizedStatus = CapStatus(profile.EvidenceStatus, maximumStatus);
        if (normalizedStatus != profile.EvidenceStatus)
        {
            diagnostics.Add(
                $"Profile '{profile.Id}' evidence status was reduced from {profile.EvidenceStatus} to {normalizedStatus} for {trustLevel} trust.");
        }

        var normalizedSources = profile.Sources
            .Select(source =>
            {
                var status = CapStatus(source.Status, maximumStatus);
                if (status != source.Status)
                {
                    diagnostics.Add(
                        $"Profile '{profile.Id}' source '{source.SourceId}' status was reduced from {source.Status} to {status}.");
                }
                return source with { Status = status };
            })
            .Append(new SvProfileSourceEvidence(
                $"manifest:{document.ManifestId}",
                $"Loaded from declarative profile manifest '{document.DisplayName}'.",
                normalizedStatus))
            .GroupBy(source => source.SourceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(source => source.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var normalized = profile with
        {
            EvidenceStatus = normalizedStatus,
            Sources = normalizedSources
        };
        normalized.Validate();
        return normalized;
    }

    private static SvProfileEvidenceStatus MaximumEvidenceStatus(SvProfileManifestTrustLevel trustLevel)
        => trustLevel switch
        {
            SvProfileManifestTrustLevel.UntrustedExternal => SvProfileEvidenceStatus.ResearchCandidate,
            SvProfileManifestTrustLevel.ReviewedEngineering => SvProfileEvidenceStatus.ImplementedGeneric,
            SvProfileManifestTrustLevel.TrustedRepository => SvProfileEvidenceStatus.VerifiedLab,
            _ => SvProfileEvidenceStatus.ResearchCandidate
        };

    private static SvProfileEvidenceStatus CapStatus(
        SvProfileEvidenceStatus value,
        SvProfileEvidenceStatus maximum)
        => EvidenceRank(value) <= EvidenceRank(maximum) ? value : maximum;

    private static int EvidenceRank(SvProfileEvidenceStatus status)
        => status switch
        {
            SvProfileEvidenceStatus.ResearchCandidate => 0,
            SvProfileEvidenceStatus.ImplementedGeneric => 1,
            SvProfileEvidenceStatus.VerifiedStandard => 2,
            SvProfileEvidenceStatus.VerifiedCapture => 3,
            SvProfileEvidenceStatus.VerifiedLab => 4,
            _ => 0
        };

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            MaxDepth = 32
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}

public static class SvProfileCatalogComposer
{
    public static IReadOnlyList<SvProfileDefinition> Compose(
        IEnumerable<SvProfileDefinition> builtInProfiles,
        IEnumerable<SvProfileManifestLoadResult> manifests,
        SvProfileManifestLoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builtInProfiles);
        ArgumentNullException.ThrowIfNull(manifests);
        options ??= new SvProfileManifestLoadOptions();
        options.Validate();

        var catalog = new Dictionary<string, SvProfileDefinition>(StringComparer.OrdinalIgnoreCase);
        var builtInIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in builtInProfiles)
        {
            profile.Validate();
            if (!catalog.TryAdd(profile.Id, profile))
                throw new InvalidOperationException($"Built-in SV profile ID '{profile.Id}' is duplicated.");
            builtInIds.Add(profile.Id);
        }

        foreach (var manifest in manifests
                     .OrderBy(item => item.Document.ManifestId, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var profile in manifest.Profiles)
            {
                if (catalog.TryGetValue(profile.Id, out _))
                {
                    var mayReplace = builtInIds.Contains(profile.Id) &&
                                     options.AllowBuiltInProfileReplacement &&
                                     manifest.TrustLevel == SvProfileManifestTrustLevel.TrustedRepository;
                    if (!mayReplace)
                    {
                        throw new InvalidDataException(
                            $"SV profile ID '{profile.Id}' from manifest '{manifest.Document.ManifestId}' collides with an existing catalog profile.");
                    }
                }

                catalog[profile.Id] = profile;
            }
        }

        return catalog.Values
            .OrderBy(profile => builtInIds.Contains(profile.Id) ? 0 : 1)
            .ThenBy(profile => profile.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
