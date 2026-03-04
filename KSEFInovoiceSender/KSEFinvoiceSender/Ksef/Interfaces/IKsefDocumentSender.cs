using KSEFinvoiceSender.Ksef.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Ksef.Interfaces;

public interface IKsefDocumentSender
{
    /// <summary>
    /// Encrypts the raw XML bytes using the active session's keys, sends the payload, 
    /// and waits for the KSeF system to assign it a KSeF Number.
    /// </summary>
    Task<InvoiceSubmissionStatus> SendInvoiceAsync(byte[] invoiceXmlBytes, KsefSession activeSession);
}
