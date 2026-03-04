using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Domain.Aggregates;

/// <summary>
/// Model reprezentujący sprzedawcę (Podmiot1).
/// </summary>
public class KsefSeller
{
    /// <summary>NIP sprzedawcy</summary>
    public string Nip { get; set; } = string.Empty;

    /// <summary>Nazwa sprzedawcy</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Kod kraju sprzedawcy</summary>
    public string CountryCode { get; set; } = "PL";

    /// <summary>Adres - linia 1 (np. ulica, nr domu, miasto, kod pocztowy)</summary>
    public string AddressLine1 { get; set; } = string.Empty;
}
