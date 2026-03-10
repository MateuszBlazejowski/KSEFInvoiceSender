using Dapper;
using KSEFinvoiceSender.Domain.Entities;
using KSEFinvoiceSender.Ksef.Models;
using KSEFinvoiceSender.Technical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Database;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public InvoiceRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<List<InvoiceDBdata>> GetPendingInvoicesAsync(string year)
    {
        var sqlTemplate = SQLloader.LoadFromFile(PathStrings.GetAllInvoicesNotInKsefQueryPath);
        var sql = sqlTemplate.Replace("@Year", year);

        using var connection = _dbConnectionFactory.CreateConnection();
        await connection.OpenAsync();

        var invoiceDictionary = new Dictionary<int, InvoiceDBdata>();

        await connection.QueryAsync<InvoiceDBdata, InvoiceLineDBdata, InvoiceDBdata>(
            sql,
            (header, line) =>
            {
                if (!invoiceDictionary.TryGetValue(header.Id, out var invoice))
                {
                    invoice = header;
                    invoice.Lines = new List<InvoiceLineDBdata>();
                    invoiceDictionary.Add(invoice.Id, invoice);
                }

                if (line is not null)
                {
                    invoice.Lines.Add(line);
                }

                return invoice;
            },
            splitOn: "Id"
        );

        return invoiceDictionary.Values.ToList();
    }

    public async Task<InvoiceDBdata> GetSpecificInvoicesAsync(string year, string invoiceID)
    {
        var sqlTemplate = SQLloader.LoadFromFile(PathStrings.GetSpecificInvoiceNotInKsefQueryPath);
        var sql = sqlTemplate.Replace("@Year", year);

        using var connection = _dbConnectionFactory.CreateConnection();
        await connection.OpenAsync();

        InvoiceDBdata? result = null;

        await connection.QueryAsync<InvoiceDBdata, InvoiceLineDBdata, InvoiceDBdata>(
            sql,
            (header, line) =>
            {
                if (result is null)
                {
                    result = header;
                    result.Lines = new List<InvoiceLineDBdata>();
                }

                if (line is not null)
                {
                    result.Lines.Add(line);
                }

                return result;
            },
            param: new { InvoiceID = invoiceID },
            splitOn: "Id"
        );

        return result ?? throw new InvalidOperationException(
            $"Invoice with NumerF '{invoiceID}' not found in FS{year}.");
    }

    public Task MarkAsFailedAsync(int localId, string errorMessage)
    {
        throw new NotImplementedException();
    }

    public Task MarkAsSentAsync(int localId, string ksefNumber, DateTimeOffset acquisitionDate)
    {
        throw new NotImplementedException();
    }

    public Task SaveBatchUpoAsync(string nip, string upoXmlContent)
    {
        throw new NotImplementedException();
    }
}
