using System;
using System.Collections.Generic;

namespace KSEFinvoiceSender.Domain.Entities;

/// <summary>
/// Raw database representation of an invoice (FS2026) with its line items (FS_pozycje2026).
/// </summary>
public class InvoiceDBdata
{
    // --- FS2026 header columns ---
    public int Id { get; set; }
    public string? RodzajF { get; set; }
    public string? NumerF { get; set; }
    public string? NabywcaId { get; set; }
    public string Nazwa1 { get; set; } = string.Empty;
    public string Nazwa2 { get; set; } = string.Empty;
    public string? Adres { get; set; }
    public string? Miasto { get; set; }
    public string? NIP { get; set; }
    public string? SprzedawcaId { get; set; }
    public DateTime? DataWystaw { get; set; }
    public string? Okres { get; set; }
    public string? Towar { get; set; }
    public decimal? Rabat { get; set; }
    public decimal? Netto { get; set; }
    public decimal VAT { get; set; }
    public decimal Brutto { get; set; }
    public decimal? Zaplacono { get; set; }
    public string? NrKoryg { get; set; }
    public int? IdRozlicz { get; set; }
    public byte? Status { get; set; }
    public byte? Platnosc { get; set; }
    public byte TerminPlat { get; set; }
    public string? Opis { get; set; }
    public byte IstniejePDF { get; set; }
    public short? Przylacze { get; set; }
    public DateTime? KSEF { get; set; }

    // --- FS_pozycje2026 line items ---
    public List<InvoiceLineDBdata> Lines { get; set; } = new();
}

/// <summary>
/// Raw database representation of an invoice line item (FS_pozycje2026).
/// </summary>
public class InvoiceLineDBdata
{
    public int Id { get; set; }
    public int IdFaktury { get; set; }
    public string? NumerF { get; set; }
    public int RokFaktury { get; set; }
    public int LpPozycji { get; set; }
    public string? Towar { get; set; }
    public string? Okres { get; set; }
    public decimal Netto { get; set; }
    public string? StVAT { get; set; }
    public decimal VAT { get; set; }
    public decimal Brutto { get; set; }
    public string? GTU { get; set; }
    public int Ilosc { get; set; }
    public int? Usluga { get; set; }
}
