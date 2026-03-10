using KSEFinvoiceSender.Configuration;
using KSEFinvoiceSender.Domain.Aggregates;
using KSEFinvoiceSender.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Domain;

public static class Aggregator
{
    public static KsefReadyInvoice MapToReadyInvoice(InvoiceDBdata dbData)
    {
        var invoice = new KsefReadyInvoice
        {
            GenerationDate = DateTime.Now,
            SystemInfo = "Aplikacja Podatnika KSeF",

            Seller = ResolveSeller(dbData.SprzedawcaId),

            Buyer = new KsefBuyer
            {
                Nip = dbData.NIP?.Trim() ?? string.Empty,
                Name = BuildBuyerName(dbData.Nazwa1, dbData.Nazwa2),
                CountryCode = "PL",
                AddressLine1 = BuildBuyerAddress(dbData.Adres, dbData.Miasto),
                ClientNumber = dbData.NabywcaId?.Trim()
            },

            // --- Invoice data ---
            InvoiceData = MapInvoiceData(dbData)
        };

        return invoice;
    }

    private static KsefSeller ResolveSeller(string? sprzedawcaId)
    {
        var config = ConfigSingleton.Instance.ksefConfig;
        var id = sprzedawcaId?.Trim().ToUpperInvariant();

        return id switch
        {
            "ML" => new KsefSeller
            {
                Nip = config.mlNIP,
                Name = config.mlNAZWA,
                CountryCode = "PL",
                AddressLine1 = config.mlADRES
            },
            "MR" => new KsefSeller
            {
                Nip = config.mrNIP,
                Name = config.mrNAZWA,
                CountryCode = "PL",
                AddressLine1 = config.mrADRES
            },
            _ => throw new InvalidOperationException(
                $"Unknown SprzedawcaId: '{sprzedawcaId}'. Expected 'ML' or 'MR'.")
        };
    }

    private static KsefInvoiceData MapInvoiceData(InvoiceDBdata dbData)
    {
        var data = new KsefInvoiceData
        {
            CurrencyCode = "PLN",
            IssueDate = dbData.DataWystaw ?? DateTime.Today,
            InvoiceNumber = dbData.NumerF?.Trim() ?? string.Empty,
            InvoiceType = MapInvoiceType(dbData.RodzajF),
            GrossTotal = dbData.Brutto
        };

        // --- Period (Okres format: "dd.MM.yyyy-dd.MM.yyyy") ---
        ParsePeriod(dbData.Okres, out var periodFrom, out var periodTo);
        data.PeriodFrom = periodFrom;
        data.PeriodTo = periodTo;

        // --- Aggregate VAT totals from line items ---
        foreach (var line in dbData.Lines)
        {
            var rate = NormalizeVatRate(line.StVAT);

            switch (rate)
            {
                case "23":
                    data.NetTotalRate23 += line.Netto;
                    data.VatTotalRate23 += line.VAT;
                    break;
                case "8":
                    data.NetTotalRate8 += line.Netto;
                    data.VatTotalRate8 += line.VAT;
                    break;

            }
        }

        // --- Map line items ---
        data.Lines = dbData.Lines
            .Select((l, idx) => new KsefInvoiceLine
            {
                LineNumber = l.LpPozycji > 0 ? l.LpPozycji : idx + 1,
                Name = MapTowarName(l.Towar),
                UnitOfMeasure = l.Usluga.HasValue ? "usł." : "szt.",
                Quantity = l.Ilosc,
                UnitGrossPrice = l.Ilosc > 0 ? Math.Round(l.Brutto / l.Ilosc, 2) : l.Brutto,
                NetValue = l.Netto,
                VatRate = NormalizeVatRate(l.StVAT)
            })
            .ToList();

        return data;
    }

    private static string MapTowarName(string? towar)
    {
        return towar?.Trim().ToLowerInvariant() switch
        {
            "inst" => "Instalacja",
            "net" => "Internet",
            "serw" => "Serwis",
            "tv" => "Telewizja",
            _ => towar?.Trim() ?? string.Empty
        };
    }

    private static string MapInvoiceType(string? rodzajF)
    {
        return rodzajF?.Trim() switch
        {
            "0" => "VAT",   // zwykła
            "1" => "KOR",   // korekta
            "2" => "VAT",   // duplikat — treated as VAT
            _ => "VAT"
        };
    }

    private static void ParsePeriod(string? okres, out DateTime? from, out DateTime? to)
    {
        from = null;
        to = null;

        if (string.IsNullOrWhiteSpace(okres))
            return;

        // Expected format: "dd.MM.yyyy-dd.MM.yyyy" (e.g. "01.02.2026-28.02.2026")
        var parts = okres.Split('-');
        if (parts.Length == 2)
        {
            if (DateTime.TryParseExact(parts[0].Trim(), "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var f))
                from = f;

            if (DateTime.TryParseExact(parts[1].Trim(), "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                to = t;
        }
    }

    private static string NormalizeVatRate(string? stVat)
    {
        if (string.IsNullOrWhiteSpace(stVat))
            return "23";

        var trimmed = stVat.Trim().TrimEnd('%');

        return trimmed.ToLowerInvariant() switch
        {
            "23" or "23%" => "23",
            "8" or "8%" => "8",
            "5" or "5%" => "5",
            "0" or "0%" => "0",
            "zw" => "zw",
            _ => trimmed
        };
    }

    private static string BuildBuyerName(string nazwa1, string nazwa2)
    {
        var n1 = nazwa1.Trim();
        var n2 = nazwa2.Trim();

        if (string.IsNullOrEmpty(n2))
            return n1;

        return $"{n1} {n2}";
    }

    private static string BuildBuyerAddress(string? adres, string? miasto)
    {
        var a = adres?.Trim() ?? string.Empty;
        var m = miasto?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(m))
            return string.Empty;

        if (string.IsNullOrEmpty(a))
            return m;

        if (string.IsNullOrEmpty(m))
            return a;

        return $"{a}, {m}";
    }
}
