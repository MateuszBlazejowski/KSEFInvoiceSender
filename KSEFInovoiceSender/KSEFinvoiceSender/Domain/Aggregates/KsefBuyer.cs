using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Domain.Aggregates;

public class KsefBuyer
{
    /// <summary>NIP nabywcy</summary>
    public string Nip { get; set; } = string.Empty;

    /// <summary>Nazwa nabywcy</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Kod kraju nabywcy</summary>
    public string CountryCode { get; set; } = "PL";

    /// <summary>Adres - linia 1</summary>
    public string AddressLine1 { get; set; } = string.Empty;

    // --- Adres korespondencyjny ---

    public string CorrespondenceCountryCode { get; set; } = "PL";
    public string? CorrespondenceAddressLine1 { get; set; }
    public string? CorrespondenceAddressLine2 { get; set; }

    /// <summary>NrKlienta</summary>
    public string? ClientNumber { get; set; }

    /// <summary>Jednostka Samorządu Terytorialnego (zwykle 1 = brak, 2 = tak)</summary>
    public byte? Jst { get; set; }

    /// <summary>Grupa VAT (zwykle 1 = brak, 2 = tak)</summary>
    public byte? Gv { get; set; }
}
