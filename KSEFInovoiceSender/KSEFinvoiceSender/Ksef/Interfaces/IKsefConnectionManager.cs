using KSEFinvoiceSender.Ksef.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Ksef.Interfaces;

public interface IKsefConnectionManager
{
    /// <summary>
    /// Authenticates with KSeF, generates cryptographic keys, and opens an interactive session.
    /// </summary>
    Task<KsefSession> ConnectAsync(KsefCredentials credentials);

    /// <summary>
    /// Closes the active session. This action triggers KSeF to generate the batch UPO.
    /// </summary>
    Task DisconnectAsync(KsefSession session);

    /// <summary>
    /// Polls for and downloads the UPO XML document for a closed session.
    /// </summary>
    Task<string> DownloadSessionUpoAsync(string sessionReferenceNumber, string accessToken);
}