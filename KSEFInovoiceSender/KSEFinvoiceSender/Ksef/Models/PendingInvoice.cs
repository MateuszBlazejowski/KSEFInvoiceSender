using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Ksef.Models;

public record PendingInvoice(int LocalId, string SellerNip, byte[] XmlBytes);
