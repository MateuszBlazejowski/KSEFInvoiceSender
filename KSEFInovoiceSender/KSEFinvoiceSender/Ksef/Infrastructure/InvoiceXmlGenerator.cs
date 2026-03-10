using KSEFinvoiceSender.Domain;
using KSEFinvoiceSender.Domain.Aggregates;
using KSEFinvoiceSender.Domain.Entities;
using KSEFinvoiceSender.Ksef.Interfaces;
using KSEFinvoiceSender.Ksef.Mappers;
using KSEFinvoiceSender.Ksef.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace KSEFinvoiceSender.Ksef.Infrastructure;

public class InvoiceXmlGenerator : IInvoiceXmlGenerator
{
    public List<PendingInvoice> Generate(List<InvoiceDBdata> dbDataList)
    {
        var pendingInvoices = new List<PendingInvoice>();

        foreach (var dbData in dbDataList)
        {
            try
            {
                // --- STEP 1: Map DB Data to Domain Model ---
                KsefReadyInvoice readyInvoice = Aggregator.MapToReadyInvoice(dbData);

                // --- STEP 2: Map Domain Model to strict KSeF XSD class ---
                Faktura ksefFaktura = DbInvoiceToGeneratedKsefMapper.Map(readyInvoice);

                // --- STEP 3: Serialize to pure XML bytes ---
                byte[] xmlBytes = KsefInvoiceToXmlMapper.SerializeInvoiceToXml(ksefFaktura);

                // --- STEP 4: Package it for the Orchestrator ---
                var pendingInvoice = new PendingInvoice(dbData.Id, readyInvoice.Seller.Nip, xmlBytes);
                pendingInvoices.Add(pendingInvoice);
            }
            catch (Exception ex)
            {
                // A failure in mapping or serializing one invoice should not crash the whole batch.
                // It is safely caught here.
                Console.WriteLine($"[ERROR] Pipeline failed for DB Record ID {dbData.Id} ({dbData.NumerF}): {ex.Message}");
            }
        }

        return pendingInvoices;
    }
}
