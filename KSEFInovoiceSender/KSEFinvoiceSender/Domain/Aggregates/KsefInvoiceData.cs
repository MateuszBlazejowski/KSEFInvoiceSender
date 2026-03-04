using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Domain.Aggregates;

/// <summary>
/// Model reprezentujący dane merytoryczne i finansowe faktury (węzeł Fa).
/// </summary>
public class KsefInvoiceData
{
    /// <summary>Kod waluty</summary>
    public string CurrencyCode { get; set; } = "PLN";

    /// <summary>P_1 - Data wystawienia</summary>
    public DateTime IssueDate { get; set; }

    /// <summary>P_2 - Numer faktury</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>RodzajFaktury (np. VAT, KOR, ZAL)</summary>
    public string InvoiceType { get; set; } = "VAT";

    // --- OKRES ---
    /// <summary>OkresFa.P_6_Od</summary>
    public DateTime? PeriodFrom { get; set; }

    /// <summary>OkresFa.P_6_Do</summary>
    public DateTime? PeriodTo { get; set; }

    // --- PODSUMOWANIE WARTOŚCI (Totals) ---
    /// <summary>P_13_1 - Suma netto dla stawki 23%</summary>
    public decimal NetTotalRate23 { get; set; }

    /// <summary>P_14_1 - Suma VAT dla stawki 23%</summary>
    public decimal VatTotalRate23 { get; set; }

    /// <summary>P_13_2 - Suma netto dla stawki 8%</summary>
    public decimal NetTotalRate8 { get; set; }

    /// <summary>P_14_2 - Suma VAT dla stawki 8%</summary>
    public decimal VatTotalRate8 { get; set; }

    /// <summary>P_15 - Kwota brutto ogółem</summary>
    public decimal GrossTotal { get; set; }

    // --- ADNOTACJE ---
    // W KSeF wartości te przyjmują najczęściej: 1 (Tak/Nie dotyczy - negacja) lub 2 (Nie)

    /// <summary>P_16 - Metoda kasowa</summary>
    public byte P_16 { get; set; } = 2;

    /// <summary>P_17 - Samofakturowanie</summary>
    public byte P_17 { get; set; } = 2;

    /// <summary>P_18 - Odwrotne obciążenie</summary>
    public byte P_18 { get; set; } = 2;

    /// <summary>P_18A - Mechanizm podzielonej płatności</summary>
    public byte P_18A { get; set; } = 2;

    /// <summary>P_19N - Zwolnienie z VAT nie dotyczy (1 = tak, 2 = nie)</summary>
    public byte P_19N { get; set; } = 1;

    /// <summary>P_22N - Nowe środki transportu nie dotyczą</summary>
    public byte P_22N { get; set; } = 1;

    /// <summary>P_23 - WDT nowych środków transportu</summary>
    public byte P_23 { get; set; } = 2;

    /// <summary>P_PMarzyN - Procedura marży nie dotyczy</summary>
    public byte P_PMarzyN { get; set; } = 1;

    // --- WIERSZE FAKTURY ---
    /// <summary>Pozycje (wiersze) na fakturze</summary>
    public List<KsefInvoiceLine> Lines { get; set; } = new List<KsefInvoiceLine>();
}
