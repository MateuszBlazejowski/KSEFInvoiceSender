using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Configuration;

public class KSeFConfig
{
    public string FormSystemCode { get; init; } = null!;
    public string FormSchemaVersion { get; init; } = null!;
    public string FormValue { get; init; } = null!;

    public int InvoicePollingTimeoutSeconds { get; init; }
    public int InvoicePollingIntervalSeconds { get; init; }

    public string mlNIP { get; init; } = null!;

    public string mlApiToken { get; init; } = null!;

    public string mrNIP { get; init; } = null!;

    public string mrApiToken { get; init; } = null!;
}
