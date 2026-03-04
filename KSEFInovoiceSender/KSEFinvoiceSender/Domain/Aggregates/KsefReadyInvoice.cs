using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Domain.Aggregates;

using System;
using System.Collections.Generic;

/// <summary>
/// Główny model DTO zawierający wszystkie dane potrzebne do wygenerowania faktury KSeF.
/// </summary>
public class KsefReadyInvoice
{
    /// <summary>DataWytworzeniaFa</summary>
    public DateTime GenerationDate { get; set; } 

    /// <summary>SystemInfo</summary>
    public string SystemInfo { get; set; } = "Aplikacja Podatnika KSeF";

    /// <summary>Podmiot1 - Sprzedawca</summary>
    public KsefSeller Seller { get; set; } = new KsefSeller();

    /// <summary>Podmiot2 - Nabywca</summary>
    public KsefBuyer Buyer { get; set; } = new KsefBuyer();

    /// <summary>Fa - Szczegóły faktury</summary>
    public KsefInvoiceData InvoiceData { get; set; } = new KsefInvoiceData();
}
