using KSeF.Client.Core.Models.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Ksef.Models;

/// <summary>
/// Represents the active, open "pipe" to the KSeF servers.
/// </summary>
public record KsefSession(
    string SessionReferenceNumber,
    string AccessToken,
    EncryptionData SessionKeys
);
