using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using global::KSEFinvoiceSender.Configuration;
using global::KSEFinvoiceSender.Ksef.Interfaces;
using global::KSEFinvoiceSender.Ksef.Models;
using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using System;
using System.Threading.Tasks;


namespace KSEFinvoiceSender.Ksef.Infrastructure;

public class KsefDocumentSender : IKsefDocumentSender
{
    private readonly ICryptographyService _cryptographyService;
    private readonly IKSeFClient _ksefClient;

    private readonly int _pollingTimeoutSeconds;
    private readonly int _pollingIntervalSeconds;

    public KsefDocumentSender(
        ICryptographyService cryptographyService,
        IKSeFClient ksefClient)
    {
        _cryptographyService = cryptographyService;
        _ksefClient = ksefClient;

        // Load polling settings from your configuration singleton.
        // Fallback to 120s timeout and 2s interval if missing in config.
        var ksefConfig = ConfigSingleton.Instance.ksefConfig;
        _pollingTimeoutSeconds = ksefConfig.InvoicePollingTimeoutSeconds > 0
            ? ksefConfig.InvoicePollingTimeoutSeconds
            : 120;

        _pollingIntervalSeconds = ksefConfig.InvoicePollingIntervalSeconds > 0
            ? ksefConfig.InvoicePollingIntervalSeconds
            : 2;
    }

    public async Task<InvoiceSubmissionStatus> SendInvoiceAsync(byte[] invoiceXmlBytes, KsefSession activeSession)
    {
        try
        {
            // 1. Get metadata (hash and size) for the plain XML
            FileMetadata plainMetadata = _cryptographyService.GetMetaData(invoiceXmlBytes);

            // 2. Encrypt the invoice using the keys from our active session!
            byte[] encryptedInvoice = _cryptographyService.EncryptBytesWithAES256(
                invoiceXmlBytes,
                activeSession.SessionKeys.CipherKey,
                activeSession.SessionKeys.CipherIv);

            // 3. Get metadata (hash and size) for the encrypted payload
            FileMetadata encryptedMetadata = _cryptographyService.GetMetaData(encryptedInvoice);

            // 4. Build the Send Request
            SendInvoiceRequest sendRequest = SendInvoiceOnlineSessionRequestBuilder
                .Create()
                .WithInvoiceHash(plainMetadata.HashSHA, plainMetadata.FileSize)
                .WithEncryptedDocumentHash(encryptedMetadata.HashSHA, encryptedMetadata.FileSize)
                .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
                .Build();

            // 5. Send the encrypted invoice payload to the KSeF Session
            SendInvoiceResponse sendResponse = await _ksefClient.SendOnlineSessionInvoiceAsync(
                sendRequest,
                activeSession.SessionReferenceNumber,
                activeSession.AccessToken);

            // 6. Poll for the final status
            return await WaitForInvoiceAcceptanceAsync(
                sendResponse.ReferenceNumber,
                activeSession.SessionReferenceNumber,
                activeSession.AccessToken);
        }
        catch (Exception ex)
        {
            // If the process fails locally (e.g., encryption error or network drop)
            return InvoiceSubmissionStatus.Failure($"Local processing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Repeatedly checks the KSeF system to see if the uploaded invoice has been processed and accepted.
    /// </summary>
    private async Task<InvoiceSubmissionStatus> WaitForInvoiceAcceptanceAsync(
        string invoiceReference,
        string sessionReference,
        string accessToken)
    {
        SessionInvoice? invoiceStatus = null;
        DateTime timeout = DateTime.UtcNow.AddSeconds(_pollingTimeoutSeconds);

        while (DateTime.UtcNow < timeout)
        {
            SessionInvoice inv = await _ksefClient.GetSessionInvoiceAsync(
                sessionReference,
                invoiceReference,
                accessToken);

            // HTTP 200 means KSeF has successfully processed and assigned a KSeF Number
            if (inv.Status.Code == 200)
            {
                invoiceStatus = inv;
                break;
            }

            // HTTP 400 or above means KSeF rejected the invoice (e.g., schema validation failed, wrong NIP)
            if (inv.Status.Code >= 400)
            {
                return InvoiceSubmissionStatus.Failure($"KSeF Rejected Invoice. Code: {inv.Status.Code} - {inv.Status.Description}");
            }

            // Still processing (Code 315, etc.). Wait before polling again to avoid API rate-limits.
            await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds));
        }

        if (invoiceStatus == null)
        {
            return InvoiceSubmissionStatus.Failure($"Timeout: Invoice was not processed by KSeF within {_pollingTimeoutSeconds} seconds.");
        }

        return InvoiceSubmissionStatus.Success(
            invoiceStatus.KsefNumber,
            invoiceStatus.AcquisitionDate ?? DateTimeOffset.UtcNow);
    }
}
