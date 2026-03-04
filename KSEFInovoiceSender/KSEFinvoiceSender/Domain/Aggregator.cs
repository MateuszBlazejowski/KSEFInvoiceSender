using KSEFinvoiceSender.Domain.Aggregates;
using KSEFinvoiceSender.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Domain;

public static class Aggregator
{
    public static KsefReadyInvoice MapToReadyInvoice(InvoiceDBdata dbData)
    {
        throw new NotImplementedException("The mapping from InvoiceDBdata to KsefReadyInvoice is not yet implemented.");
    }
}
