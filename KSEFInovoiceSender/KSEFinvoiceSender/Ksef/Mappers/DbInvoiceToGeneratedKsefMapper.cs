using KSEFinvoiceSender.Domain.Entities;
using System;
using System.Collections.Generic;

namespace KSEFinvoiceSender.Ksef.Mappers;
using KSEFinvoiceSender.Domain.Aggregates;

public static class DbInvoiceToGeneratedKsefMapper
{
    public static Faktura Map(KsefReadyInvoice src)
    {
        var faktura = new Faktura();

        // --- 1. NAGŁÓWEK ---
        faktura.Naglowek = new TNaglowek
        {
            KodFormularza = new TNaglowekKodFormularza { Value = TKodFormularza.FA },
            WariantFormularza = 3,
            DataWytworzeniaFa = src.GenerationDate,
            SystemInfo = src.SystemInfo
        };

        // --- 2. PODMIOT 1 (Sprzedawca) ---
        faktura.Podmiot1 = new FakturaPodmiot1
        {
            PrefiksPodatnika = TKodyKrajowUE.PL,
            PrefiksPodatnikaSpecified = true,
            DaneIdentyfikacyjne = new TPodmiot1
            {
                NIP = src.Seller.Nip,
                Nazwa = src.Seller.Name
            },
            Adres = new TAdres
            {
                KodKraju = Enum.Parse<TKodKraju>(src.Seller.CountryCode),
                AdresL1 = src.Seller.AddressLine1
            }
        };

        // --- 3. PODMIOT 2 (Nabywca) ---
        faktura.Podmiot2 = new FakturaPodmiot2
        {
            DaneIdentyfikacyjne = new TPodmiot2
            {
                Nazwa = src.Buyer.Name,
                // Handling the <xs:choice> for Identification (NIP, NrVatUE, etc.)
                Items = new object[] { src.Buyer.Nip },
                ItemsElementName = new[] { ItemsChoiceType.NIP }
            },
            Adres = new TAdres
            {
                KodKraju = Enum.Parse<TKodKraju>(src.Buyer.CountryCode),
                AdresL1 = src.Buyer.AddressLine1
            },
            NrKlienta = src.Buyer.ClientNumber
        };

        // Handling optional enums
        if (src.Buyer.Jst.HasValue)
            faktura.Podmiot2.JST = src.Buyer.Jst.Value == 1 ? FakturaPodmiot2JST.Item1 : FakturaPodmiot2JST.Item2;

        if (src.Buyer.Gv.HasValue)
            faktura.Podmiot2.GV = src.Buyer.Gv.Value == 1 ? FakturaPodmiot2GV.Item1 : FakturaPodmiot2GV.Item2;

        // Correspondence address mapping
        if (!string.IsNullOrEmpty(src.Buyer.CorrespondenceAddressLine1))
        {
            faktura.Podmiot2.AdresKoresp = new TAdres
            {
                KodKraju = Enum.Parse<TKodKraju>(src.Buyer.CorrespondenceCountryCode),
                AdresL1 = src.Buyer.CorrespondenceAddressLine1,
                AdresL2 = src.Buyer.CorrespondenceAddressLine2
            };
        }

        // --- 4. FA (Szczegóły Faktury) ---
        faktura.Fa = new FakturaFA
        {
            KodWaluty = Enum.Parse<TKodWaluty>(src.InvoiceData.CurrencyCode),
            P_1 = src.InvoiceData.IssueDate,
            P_2 = src.InvoiceData.InvoiceNumber,
            RodzajFaktury = Enum.Parse<TRodzajFaktury>(src.InvoiceData.InvoiceType),

            // Financial Totals
            P_13_1 = src.InvoiceData.NetTotalRate23,
            P_14_1 = src.InvoiceData.VatTotalRate23,
            P_13_2 = src.InvoiceData.NetTotalRate8,
            P_14_2 = src.InvoiceData.VatTotalRate8,
            P_15 = src.InvoiceData.GrossTotal,

            // Adnotacje (Annotations) mapping
            Adnotacje = new FakturaFAAdnotacje
            {
                P_16 = (sbyte)src.InvoiceData.P_16,
                P_17 = (sbyte)src.InvoiceData.P_17,
                P_18 = (sbyte)src.InvoiceData.P_18,
                P_18A = (sbyte)src.InvoiceData.P_18A,
                P_23 = (sbyte)src.InvoiceData.P_23,

                Zwolnienie = new FakturaFAAdnotacjeZwolnienie
                {
                    Items = new object[] { (sbyte)src.InvoiceData.P_19N },
                    ItemsElementName = new[] { ItemsChoiceType2.P_19N }
                },
                NoweSrodkiTransportu = new FakturaFAAdnotacjeNoweSrodkiTransportu
                {
                    Items = new object[] { (sbyte)src.InvoiceData.P_22N },
                    ItemsElementName = new[] { ItemsChoiceType4.P_22N }
                },
                PMarzy = new FakturaFAAdnotacjePMarzy
                {
                    Items = new[] { (sbyte)src.InvoiceData.P_PMarzyN },
                    ItemsElementName = new[] { ItemsChoiceType5.P_PMarzyN }
                }
            }
        };

        // Handling <xs:choice> for Invoice Period vs Date
        if (src.InvoiceData.PeriodFrom.HasValue && src.InvoiceData.PeriodTo.HasValue)
        {
            faktura.Fa.Item = new FakturaFAOkresFa
            {
                P_6_Od = src.InvoiceData.PeriodFrom.Value,
                P_6_Do = src.InvoiceData.PeriodTo.Value
            };
        }

        // --- 5. WIERSZE (Invoice Lines) ---
        if (src.InvoiceData.Lines != null && src.InvoiceData.Lines.Count > 0)
        {
            var lines = new List<FakturaFAFaWiersz>();
            foreach (var line in src.InvoiceData.Lines)
            {
                lines.Add(new FakturaFAFaWiersz
                {
                    NrWierszaFa = line.LineNumber.ToString(),
                    P_7 = line.Name,
                    P_8A = line.UnitOfMeasure,

                    P_8B = line.Quantity,
                    P_8BSpecified = true, // REQUIRED: Tells XML serializer to output this decimal

                    P_9B = line.UnitGrossPrice,
                    P_9BSpecified = true,

                    P_11A = line.NetValue,
                    P_11ASpecified = true,

                    P_12 = ParseVatRate(line.VatRate),
                    P_12Specified = true
                });
            }
            faktura.Fa.FaWiersz = lines.ToArray();
        }

        return faktura;
    }

    /// <summary>
    /// Helper to convert string VAT rates into the strict Enum required by the XSD.
    /// </summary>
    private static TStawkaPodatku ParseVatRate(string rate)
    {
        return rate switch
        {
            "23" => TStawkaPodatku.Item23,
            "22" => TStawkaPodatku.Item22,
            "8" => TStawkaPodatku.Item8,
            "7" => TStawkaPodatku.Item7,
            "5" => TStawkaPodatku.Item5,
            "4" => TStawkaPodatku.Item4,
            "3" => TStawkaPodatku.Item3,
            "0 KR" => TStawkaPodatku.Item0KR,
            "0 WDT" => TStawkaPodatku.Item0WDT,
            "0 EX" => TStawkaPodatku.Item0EX,
            "zw" => TStawkaPodatku.zw,
            "oo" => TStawkaPodatku.oo,
            "np I" => TStawkaPodatku.npI,
            "np II" => TStawkaPodatku.npII,
            _ => throw new ArgumentException($"Unknown or unsupported VAT rate: {rate}")
        };
    }
}