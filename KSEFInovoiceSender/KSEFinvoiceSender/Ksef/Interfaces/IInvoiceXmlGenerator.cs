using KSEFinvoiceSender.Domain.Entities;
using KSEFinvoiceSender.Ksef.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Ksef.Interfaces;

public interface IInvoiceXmlGenerator
{
    /// <summary>
    /// Transforms raw database records into KSeF-ready XML payloads.
    /// </summary>
    List<PendingInvoice> Generate(List<InvoiceDBdata> dbDataList);
}