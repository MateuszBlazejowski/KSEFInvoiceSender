using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSEFinvoiceSender.Domain.Aggregates;
using KSEFinvoiceSender.Ksef.Mappers;

namespace KSEFinvoiceSender.Tests;
public class KSEFreadyInvoiceToGeneratedMapperTest
{
    [Fact]
    public void Map_ShouldCorrectlyMapDtoToFakturaXmlObject()
    {
        // Arrange: Create a dummy invoice with known values
        var testDate = new DateTime(2026, 3, 2);
        var dto = new KsefReadyInvoice
        {
            GenerationDate = testDate,
            SystemInfo = "Test System",
            Seller = new KsefSeller
            {
                Nip = "1112223344",
                Name = "Test Seller Sp. z o.o.",
                CountryCode = "PL",
                AddressLine1 = "ul. Testowa 1, 00-001 Warszawa"
            },
            Buyer = new KsefBuyer
            {
                Nip = "5556667788",
                Name = "Test Buyer S.A.",
                CountryCode = "PL",
                AddressLine1 = "ul. Nabywcy 2, 02-002 Kraków",
                ClientNumber = "CUST-999",
                Jst = 2,
                Gv = 2
            },
            InvoiceData = new KsefInvoiceData
            {
                CurrencyCode = "PLN",
                IssueDate = testDate.AddDays(-1),
                InvoiceNumber = "FV/2026/03/001",
                InvoiceType = "VAT",
                PeriodFrom = new DateTime(2026, 2, 1),
                PeriodTo = new DateTime(2026, 2, 28),
                NetTotalRate23 = 1000.00m,
                VatTotalRate23 = 230.00m,
                GrossTotal = 1230.00m,
                Lines =
                {
                    new KsefInvoiceLine
                    {
                        LineNumber = 1,
                        Name = "Konsultacje IT",
                        UnitOfMeasure = "godz",
                        Quantity = 10m,
                        UnitGrossPrice = 123.00m,
                        NetValue = 1000.00m,
                        VatRate = "23"
                    }
                }
            }
        };

        // Act: Run the mapper
        var result = DbInvoiceToGeneratedKsefMapper.Map(dto);

        // Assert: Verify the generated structure

        // 1. Header
        Assert.Equal("FA", result.Naglowek.KodFormularza.Value.ToString());
        Assert.Equal(3, result.Naglowek.WariantFormularza);
        Assert.Equal("Test System", result.Naglowek.SystemInfo);

        // 2. Seller
        Assert.Equal("1112223344", result.Podmiot1.DaneIdentyfikacyjne.NIP);
        Assert.True(result.Podmiot1.PrefiksPodatnikaSpecified);
        Assert.Equal(TKodyKrajowUE.PL, result.Podmiot1.PrefiksPodatnika);

        // 3. Buyer (checking the tricky choice array)
        Assert.Equal("5556667788", result.Podmiot2.DaneIdentyfikacyjne.Items[0].ToString());
        Assert.Equal(ItemsChoiceType.NIP, result.Podmiot2.DaneIdentyfikacyjne.ItemsElementName[0]);
        Assert.Equal(FakturaPodmiot2JST.Item2, result.Podmiot2.JST);
        Assert.Equal("CUST-999", result.Podmiot2.NrKlienta);

        // 4. Invoice Data
        Assert.Equal(TKodWaluty.PLN, result.Fa.KodWaluty);
        Assert.Equal("FV/2026/03/001", result.Fa.P_2);
        Assert.Equal(1000.00m, result.Fa.P_13_1);
        Assert.Equal(1230.00m, result.Fa.P_15);

        // 5. Annotations
        Assert.Equal(2, result.Fa.Adnotacje.P_16);
        Assert.Equal(1, (sbyte)result.Fa.Adnotacje.Zwolnienie.Items[0]); // P_19N
        Assert.Equal(ItemsChoiceType2.P_19N, result.Fa.Adnotacje.Zwolnienie.ItemsElementName[0]);

        // 6. Lines
        Assert.Single(result.Fa.FaWiersz); // Ensures there is exactly 1 line
        var firstLine = result.Fa.FaWiersz.First();
        Assert.Equal("Konsultacje IT", firstLine.P_7);
        Assert.Equal(10m, firstLine.P_8B);
        Assert.True(firstLine.P_8BSpecified); // Crucial: ensures XML will render this!
        Assert.Equal(TStawkaPodatku.Item23, firstLine.P_12);
        Assert.True(firstLine.P_12Specified);
    }

    [Fact]
    public void Map_WithInvalidVatRate_ShouldThrowArgumentException()
    {
        // Arrange: Inject a VAT rate that doesn't exist in the KSeF specification
        var dto = new KsefReadyInvoice
        {
            Seller = new KsefSeller { Nip = "123", Name = "Seller" },
            Buyer = new KsefBuyer { Nip = "456", Name = "Buyer" },
            InvoiceData = new KsefInvoiceData
            {
                Lines =
            {
                new KsefInvoiceLine
                {
                    LineNumber = 1,
                    Name = "Buggy Item",
                    Quantity = 1m,
                    VatRate = "99%" // Invalid!
                }
            }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DbInvoiceToGeneratedKsefMapper.Map(dto));

        // Verify the exception message is helpful
        Assert.Contains("Unknown or unsupported VAT rate: 99%", exception.Message);
    }
    [Fact]
    public void Map_WithMissingOptionalFields_ShouldNotGenerateOptionalNodes()
    {
        // Arrange: Leave optional fields like PeriodFrom/To, Jst, Gv, and CorrespondenceAddress null
        var dto = new KsefReadyInvoice
        {
            Seller = new KsefSeller { Nip = "123", Name = "Minimal Seller" },
            Buyer = new KsefBuyer
            {
                Nip = "456",
                Name = "Minimal Buyer",
                CorrespondenceAddressLine1 = null // Explicitly null
            },
            InvoiceData = new KsefInvoiceData
            {
                PeriodFrom = null, // Explicitly null
                PeriodTo = null
            }
        };

        // Act
        var result = DbInvoiceToGeneratedKsefMapper.Map(dto);

        // Assert
        // 1. Correspondence Address should be null
        Assert.Null(result.Podmiot2.AdresKoresp);

        // 2. Invoice Period (OkresFa) should not be created since dates are missing
        Assert.Null(result.Fa.Item);

        // 3. Lines should be null (since the initialized empty list was passed, 
        // the mapper correctly avoids creating the array if Count == 0)
        Assert.Null(result.Fa.FaWiersz);
    }

    [Fact]
    public void Map_WithMultipleLinesAndVariousVatRates_ShouldMapAllCorrectly()
    {
        // Arrange
        var dto = new KsefReadyInvoice
        {
            Seller = new KsefSeller { Nip = "111", Name = "A" },
            Buyer = new KsefBuyer { Nip = "222", Name = "B" },
            InvoiceData = new KsefInvoiceData
            {
                Lines =
            {
                new KsefInvoiceLine { LineNumber = 1, Name = "Item A", Quantity = 1m, VatRate = "23" },
                new KsefInvoiceLine { LineNumber = 2, Name = "Item B", Quantity = 2m, VatRate = "8" },
                new KsefInvoiceLine { LineNumber = 3, Name = "Export Item", Quantity = 5m, VatRate = "0 EX" },
                new KsefInvoiceLine { LineNumber = 4, Name = "Exempt Item", Quantity = 10m, VatRate = "zw" }
            }
            }
        };

        // Act
        var result = DbInvoiceToGeneratedKsefMapper.Map(dto);

        // Assert
        Assert.NotNull(result.Fa.FaWiersz);
        Assert.Equal(4, result.Fa.FaWiersz.Length);

        // Verify specific VAT enum parsing
        Assert.Equal(TStawkaPodatku.Item23, result.Fa.FaWiersz[0].P_12);
        Assert.Equal(TStawkaPodatku.Item8, result.Fa.FaWiersz[1].P_12);
        Assert.Equal(TStawkaPodatku.Item0EX, result.Fa.FaWiersz[2].P_12);
        Assert.Equal(TStawkaPodatku.zw, result.Fa.FaWiersz[3].P_12);

        // Ensure the "Specified" flag is set for all of them so they actually serialize
        Assert.All(result.Fa.FaWiersz, wiersz => Assert.True(wiersz.P_12Specified));
    }

}