using System.Security.Cryptography;
using System.Text;
using skipper_api.Data;
using skipper_api.Domain.Animals;

namespace HowellSync.Synchronization;

public static class CatalogIssueRecorder
{
    // Hash selected identity/condition fields, never a serialized provider response.
    public static void Add(ProfessorDbContext db, int? runId, string provider, string? dataset, string? identifier,
        string type, string description, string condition = "", SpeciesExternalReference? reference = null,
        CatalogIssueSeverity severity = CatalogIssueSeverity.Warning)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(new[] { provider, dataset, identifier, type, condition }))));
        if (db.CatalogSyncIssues.Local.Any(x => x.Fingerprint == fingerprint) ||
            db.CatalogSyncIssues.Any(x => x.Fingerprint == fingerprint &&
                (x.Status == CatalogIssueStatus.Open || x.Status == CatalogIssueStatus.Investigating))) return;
        db.CatalogSyncIssues.Add(new CatalogSyncIssue {
            CatalogSyncRunId = runId, Provider = provider, DatasetIdentifier = dataset, ExternalIdentifier = identifier,
            Fingerprint = fingerprint, IssueType = type, Description = description, CreatedAt = DateTime.UtcNow,
            SpeciesId = reference?.SpeciesId, SpeciesExternalReferenceId = reference?.Id, Severity = severity
        });
    }
}
