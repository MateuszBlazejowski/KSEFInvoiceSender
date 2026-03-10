using KSEFinvoiceSender.Domain.Entities;
using KSEFinvoiceSender.Ksef.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Database;

public interface IInvoiceRepository
{
    Task<List<InvoiceDBdata>> GetPendingInvoicesAsync(string year);

    Task<InvoiceDBdata> GetSpecificInvoicesAsync(string year, string invoiceID);

    Task MarkAsSentAsync(int localId, string ksefNumber, DateTimeOffset acquisitionDate);
    Task MarkAsFailedAsync(int localId, string errorMessage);
    Task SaveBatchUpoAsync(string nip, string upoXmlContent);
}
