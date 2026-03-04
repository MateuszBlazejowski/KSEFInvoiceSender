using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Ksef.Models;
/// <summary>
/// The result of a single invoice upload attempt within an active session.
/// (Notice: No UPO property here, because it doesn't exist yet!)
/// </summary>
public class InvoiceSubmissionStatus
{
    public bool IsAccepted { get; private init; }
    public string? KsefNumber { get; private init; }
    public DateTimeOffset? AcquisitionDate { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static InvoiceSubmissionStatus Success(string ksefNumber, DateTimeOffset acquisitionDate)
    {
        return new InvoiceSubmissionStatus
        {
            IsAccepted = true,
            KsefNumber = ksefNumber,
            AcquisitionDate = acquisitionDate
        };
    }

    public static InvoiceSubmissionStatus Failure(string errorMessage)
    {
        return new InvoiceSubmissionStatus
        {
            IsAccepted = false,
            ErrorMessage = errorMessage
        };
    }
}
