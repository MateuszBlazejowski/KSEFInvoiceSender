using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Configuration;

public class KSeFConfig
{
    /// <summary>
    /// KSeF environment: "DEMO", "TEST", or "PROD"
    /// </summary>
    public string Environment { get; init; } = "DEMO";

    public string FormSystemCode { get; init; } = null!;
    public string FormSchemaVersion { get; init; } = null!;
    public string FormValue { get; init; } = null!;

    public int InvoicePollingTimeoutSeconds { get; init; }
    public int InvoicePollingIntervalSeconds { get; init; }
    public string mlNAZWA { get; init; } = null!;

    public string mlADRES { get; init; } = null!;
    public string mlNIP { get; init; } = null!;

    public string mlApiToken { get; init; } = null!;

    public string mrNAZWA { get; init; } = null!;
    public string mrADRES { get; init; } = null!;
    public string mrNIP { get; init; } = null!;

    public string mrApiToken { get; init; } = null!;
}
