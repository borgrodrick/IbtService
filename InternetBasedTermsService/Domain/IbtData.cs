namespace InternetBasedTermsService.Domain;


public record IbtData
{
    public string EventType { get; init; } = string.Empty;
    public string ProductNameFull { get; init; } = string.Empty;
    public string IbtTypeCode { get; init; } = string.Empty;
    public string Isin { get; init; } = string.Empty;
}