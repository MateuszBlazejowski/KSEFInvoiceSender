using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Domain.Aggregates;

/// <summary>
/// Model reprezentujący pojedynczy wiersz (pozycję) na fakturze.
/// </summary>
public class KsefInvoiceLine
{
    /// <summary>NrWierszaFa - Liczba porządkowa wiersza</summary>
    public int LineNumber { get; set; }

    /// <summary>P_7 - Nazwa towaru/usługi</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>P_8A - Miara (np. szt., usł.)</summary>
    public string UnitOfMeasure { get; set; } = "szt";

    /// <summary>P_8B - Ilość dostarczonych towarów/usług</summary>
    public decimal Quantity { get; set; }

    /// <summary>P_9B - Cena jednostkowa brutto (lub Netto w przypadku parametru P_9A)</summary>
    public decimal UnitGrossPrice { get; set; }

    /// <summary>P_11A - Wartość netto wiersza</summary>
    public decimal NetValue { get; set; }

    /// <summary>P_12 - Stawka VAT (np. "23", "8", "zw")</summary>
    public string VatRate { get; set; } = string.Empty;
}
