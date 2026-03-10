using KSEFinvoiceSender.Configuration;
using KSEFinvoiceSender.Database;
using KSEFinvoiceSender.Domain.Entities;
using KSEFinvoiceSender.Ksef.Interfaces;
using KSEFinvoiceSender.Ksef.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Workflows;

public class Orchestrator
{
    private readonly IKsefConnectionManager _connectionManager;
    private readonly IKsefDocumentSender _documentSender;
    private readonly IInvoiceRepository _dbRepository;
    private readonly IInvoiceXmlGenerator _xmlGenerator;

    public Orchestrator(
        IKsefConnectionManager connectionManager,
        IKsefDocumentSender documentSender,
        IInvoiceRepository dbRepository,
        IInvoiceXmlGenerator xmlGenerator)
    {
        _connectionManager = connectionManager;
        _documentSender = documentSender;
        _dbRepository = dbRepository;
        _xmlGenerator = xmlGenerator;
    }

    /// <summary>
    /// The main entry point. Fetches all pending invoices, sorts them by company, 
    /// and processes them in two separate KSeF sessions.
    /// </summary>
    public async Task ProcessAllPendingInvoicesAsync(string year)
    {
        // 1. Fetch all pending invoices from your database
        List<InvoiceDBdata> rawInvoices = await _dbRepository.GetPendingInvoicesAsync(year);

        if (!rawInvoices.Any())
        {
            Console.WriteLine("No pending invoices to process.");
            return;
        }

        // 2. Transform DB data into XML bytes
        List<PendingInvoice> allPending = _xmlGenerator.Generate(rawInvoices);

        if (!allPending.Any())
        {
            Console.WriteLine("Failed to generate any valid XMLs from the pending DB records.");
            return;
        }

        // 3. Load the configuration
        var config = ConfigSingleton.Instance.ksefConfig;

        // 4. Prepare credentials for both companies
        var mlCredentials = new KsefCredentials(config.mlNIP, config.mlApiToken);
        var mrCredentials = new KsefCredentials(config.mrNIP, config.mrApiToken);

        // 5. Sort into batches based on the Seller NIP
        var mlBatch = allPending.Where(x => x.SellerNip == config.mlNIP).ToList();
        var mrBatch = allPending.Where(x => x.SellerNip == config.mrNIP).ToList();

        // 6. Process Company ML (if any invoices exist)
        if (mlBatch.Any())
        {
            Console.WriteLine($"\n--- Starting Batch for Company ML (NIP: {config.mlNIP}) ---");
            await ProcessCompanyBatchAsync(mlBatch, mlCredentials);
        }

        // 7. Process Company MR (if any invoices exist)
        if (mrBatch.Any())
        {
            Console.WriteLine($"\n--- Starting Batch for Company MR (NIP: {config.mrNIP}) ---");
            await ProcessCompanyBatchAsync(mrBatch, mrCredentials);
        }
    }

    /// <summary>
    /// Processes a single invoice identified by its NumerF.
    /// Opens a dedicated KSeF session for the invoice's seller, sends it, and downloads the UPO.
    /// </summary>
    public async Task ProcessSingleInvoiceAsync(string year, string invoiceID)
    {
        // 1. Fetch the specific invoice from the database
        InvoiceDBdata rawInvoice = await _dbRepository.GetSpecificInvoicesAsync(year, invoiceID);

        Console.WriteLine($"Found invoice: {rawInvoice.NumerF} (ID: {rawInvoice.Id})");

        // 2. Transform DB data into XML bytes (single-item list)
        List<PendingInvoice> pending = _xmlGenerator.Generate([rawInvoice]);

        if (!pending.Any())
        {
            Console.WriteLine($"Failed to generate XML for invoice '{invoiceID}'.");
            return;
        }

        // 3. Resolve credentials based on seller NIP
        var config = ConfigSingleton.Instance.ksefConfig;
        var invoice = pending[0];

        var credentials = invoice.SellerNip switch
        {
            var nip when nip == config.mlNIP => new KsefCredentials(config.mlNIP, config.mlApiToken),
            var nip when nip == config.mrNIP => new KsefCredentials(config.mrNIP, config.mrApiToken),
            _ => throw new InvalidOperationException(
                $"Unknown seller NIP '{invoice.SellerNip}' for invoice '{invoiceID}'.")
        };

        // 4. Process as a single-item batch
        Console.WriteLine($"\n--- Sending single invoice '{invoiceID}' for NIP: {credentials.Nip} ---");
        await ProcessCompanyBatchAsync(pending, credentials);
    }

    /// <summary>
    /// Handles the actual connection, sending, and UPO retrieval for a single company batch.
    /// </summary>
    private async Task ProcessCompanyBatchAsync(List<PendingInvoice> batch, KsefCredentials credentials)
    {
        KsefSession? activeSession = null;
        bool anyAccepted = false;

        try
        {
            // 1. Establish connection for this specific company
            activeSession = await _connectionManager.ConnectAsync(credentials);
            Console.WriteLine($"Session Opened. Reference: {activeSession.SessionReferenceNumber}");
                    
            // 2. Loop through and send the invoices
            foreach (var invoice in batch)
            {
                try
                {
                    var result = await _documentSender.SendInvoiceAsync(invoice.XmlBytes, activeSession);

                    if (result.IsAccepted && result.AcquisitionDate.HasValue)
                    {
                        Console.WriteLine($"[SUCCESS] ID: {invoice.LocalId} -> KSeF: {result.KsefNumber}");
                        await _dbRepository.MarkAsSentAsync(invoice.LocalId, result.KsefNumber!, result.AcquisitionDate.Value);
                        anyAccepted = true;
                    }
                    else
                    {
                        Console.WriteLine($"[FAILED] ID: {invoice.LocalId} -> {result.ErrorMessage}");
                        await _dbRepository.MarkAsFailedAsync(invoice.LocalId, result.ErrorMessage ?? "Unknown Error");
                    }
                }
                catch (Exception ex)
                {
                    // Catching here ensures one bad invoice doesn't crash the whole batch
                    Console.WriteLine($"[ERROR] Exception on ID: {invoice.LocalId} -> {ex.Message}");
                    await _dbRepository.MarkAsFailedAsync(invoice.LocalId, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Failed to establish session for NIP {credentials.Nip}: {ex.Message}");
        }
        finally
        {
            // 3. Always close the session, but only fetch UPO if at least one invoice was accepted
            if (activeSession != null)
            {
                try
                {
                    await _connectionManager.DisconnectAsync(activeSession);
                    Console.WriteLine("Session closed.");

                    if (anyAccepted)
                    {
                        Console.WriteLine("Waiting for UPO...");

                        string upoXml = await _connectionManager.DownloadSessionUpoAsync(
                            activeSession.SessionReferenceNumber,
                            activeSession.AccessToken);

                        await _dbRepository.SaveBatchUpoAsync(credentials.Nip, upoXml);
                        Console.WriteLine($"UPO downloaded and saved for NIP: {credentials.Nip}");
                    }
                    else
                    {
                        Console.WriteLine("No invoices were accepted — skipping UPO download.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Failed during session cleanup or UPO download: {ex.Message}");
                }
            }
        }
    }
}