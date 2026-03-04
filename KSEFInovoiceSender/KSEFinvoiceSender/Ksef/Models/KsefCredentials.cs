using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Ksef.Models;

/// <summary>
/// Encapsulates the authentication details required to connect to KSeF.
/// </summary>
public record KsefCredentials(string Nip, string ApiToken);
