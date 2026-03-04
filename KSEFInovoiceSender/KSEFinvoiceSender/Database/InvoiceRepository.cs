using KSEFinvoiceSender.Domain.Entities;
using KSEFinvoiceSender.Ksef.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Database;

public class InvoiceRepository : IInvoiceRepository
{
    public Task<List<InvoiceDBdata>> GetPendingInvoicesAsync()
    {
        throw new NotImplementedException();
    }

    public Task MarkAsFailedAsync(int localId, string errorMessage)
    {
        throw new NotImplementedException();
    }

    public Task MarkAsSentAsync(int localId, string ksefNumber, DateTimeOffset acquisitionDate)
    {
        throw new NotImplementedException();
    }

    public Task SaveBatchUpoAsync(string nip, string upoXmlContent)
    {
        throw new NotImplementedException();
    }
}
