using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Technical;

public static class PathStrings
{
    public static readonly string GetAllInvoicesNotInKsefQueryPath = Path.Combine("queries", "PodajWszystkieFakturyNieWKsef.sql");
    public static readonly string GetSpecificInvoiceNotInKsefQueryPath = Path.Combine("queries", "PodajWskazanaFaktureNieWKsef.sql");
}
