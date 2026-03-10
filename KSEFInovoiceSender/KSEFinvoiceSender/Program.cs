using Dapper;
using KSEFinvoiceSender;
using KSEFinvoiceSender.Configuration;
using KSEFinvoiceSender.Database;
using MySqlConnector;
using KSeF.Client.Api;
using KSeF.Client.DI;
using KSEFinvoiceSender.Ksef.Infrastructure;
using KSEFinvoiceSender.Ksef.Interfaces;
using KSEFinvoiceSender.Workflows;
using Microsoft.Extensions.DependencyInjection;

Program1 program1 = new Program1();


var dbConfig = ConfigSingleton.Instance.dbConfig;

await program1.TestedConnectionEstablishing();


string year = DateTime.Now.Year.ToString();
string? invoiceNumber = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-y" when i + 1 < args.Length:
            year = args[++i];
            break;
        case "-f" when i + 1 < args.Length:
            invoiceNumber = args[++i];
            break;
    }
}

Console.WriteLine($"Year: {year}");
if (invoiceNumber is not null)
    Console.WriteLine($"Invoice filter: {invoiceNumber}");

// --- Configuration ---
var ksefConfig = ConfigSingleton.Instance.ksefConfig;

var (baseUrl, qrUrl) = ksefConfig.Environment.ToUpperInvariant() switch
{
    "DEMO" => (KsefEnvironmentsUris.DEMO, KsefQREnvironmentsUris.DEMO),
    "TEST" => (KsefEnvironmentsUris.TEST, KsefQREnvironmentsUris.TEST),
    "PROD" => (KsefEnvironmentsUris.PROD, KsefQREnvironmentsUris.PROD),
    _ => throw new InvalidOperationException(
        $"Unknown KSeF Environment: '{ksefConfig.Environment}'. Expected 'DEMO', 'TEST', or 'PROD'.")
};

// --- Build DI container ---
var services = new ServiceCollection();

services.AddKSeFClient(options =>
{
    options.BaseUrl = baseUrl;
    options.BaseQRUrl = qrUrl;
});
services.AddCryptographyClient();

services.AddSingleton<DbConnectionFactory>();
services.AddScoped<IInvoiceRepository, InvoiceRepository>();
services.AddScoped<IKsefConnectionManager, KsefConnectionManager>();
services.AddScoped<IKsefDocumentSender, KsefDocumentSender>();
services.AddScoped<IInvoiceXmlGenerator, InvoiceXmlGenerator>();
services.AddScoped<Orchestrator>();

var provider = services.BuildServiceProvider();

try
{
    var orchestrator = provider.GetRequiredService<Orchestrator>();

    Console.WriteLine($"KSeF Environment: {ksefConfig.Environment}");

    if (invoiceNumber is not null)
    {
        // -f was provided: process only the specified invoice
        await orchestrator.ProcessSingleInvoiceAsync(year, invoiceNumber);
    }
    else
    {
        // Default: process all pending invoices
        await orchestrator.ProcessAllPendingInvoicesAsync(year);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[FATAL] {ex.Message}");
    Console.WriteLine(ex.ToString());
}
finally
{
    if (provider is IDisposable d) d.Dispose();
}