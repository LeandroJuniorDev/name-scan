namespace NameScan.Platforms;

public sealed record DomainLookupResult(bool Exists, bool IsConclusive, string Note);
