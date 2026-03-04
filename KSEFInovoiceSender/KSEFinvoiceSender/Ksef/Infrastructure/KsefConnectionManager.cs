using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSEFinvoiceSender.Ksef.Interfaces;
using KSEFinvoiceSender.Ksef.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSEFinvoiceSender.Configuration;

namespace KSEFinvoiceSender.Ksef.Infrastructure;

public class KsefConnectionManager : IKsefConnectionManager
{
    private readonly IAuthorizationClient _authorizationClient;
    private readonly ICryptographyService _cryptographyService;
    private readonly IKSeFClient _ksefClient;

    // Fields populated from configuration
    private readonly string _formSystemCode;
    private readonly string _formSchemaVersion;
    private readonly string _formValue;

    public KsefConnectionManager(
        IAuthorizationClient authorizationClient,
        ICryptographyService cryptographyService,
        IKSeFClient ksefClient)
    {
        _authorizationClient = authorizationClient;
        _cryptographyService = cryptographyService;
        _ksefClient = ksefClient;

        var ksefConfig = ConfigSingleton.Instance.ksefConfig;
        _formSystemCode = ksefConfig.FormSystemCode;
        _formSchemaVersion = ksefConfig.FormSchemaVersion;
        _formValue = ksefConfig.FormValue;
    }
    public async Task<KsefSession> ConnectAsync(KsefCredentials credentials)
    {
        await _cryptographyService.WarmupAsync();

        IAuthCoordinator authCoordinator = new AuthCoordinator(_authorizationClient);

        AuthenticationOperationStatusResponse authResult = await authCoordinator.AuthKsefTokenAsync(
            AuthenticationTokenContextIdentifierType.Nip,
            credentials.Nip,
            credentials.ApiToken,
            _cryptographyService,
            EncryptionMethodEnum.Rsa
        );

        string accessToken = authResult.AccessToken.Token;

        EncryptionData encryptionData = _cryptographyService.GetEncryptionData();

        // Build the Open Session request using the config-driven fields
        OpenOnlineSessionRequest openRequest = OpenOnlineSessionRequestBuilder
            .Create()
            .WithFormCode(_formSystemCode, _formSchemaVersion, _formValue)
            .WithEncryption(
                encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                encryptionData.EncryptionInfo.InitializationVector)
            .Build();

        OpenOnlineSessionResponse sessionResponse = await _ksefClient.OpenOnlineSessionAsync(openRequest, accessToken);

        return new KsefSession(sessionResponse.ReferenceNumber, accessToken, encryptionData);
    }

    public async Task DisconnectAsync(KsefSession session)
    {
        try
        {
            await _ksefClient.CloseOnlineSessionAsync(session.SessionReferenceNumber, session.AccessToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to gracefully close KSeF session. {ex.Message}");
        }
    }

    public async Task<string> DownloadSessionUpoAsync(string sessionReferenceNumber, string accessToken)
    {
        UpoResponse? upoPages = null;
        DateTime timeout = DateTime.UtcNow.AddMinutes(5);

        while (DateTime.UtcNow < timeout)
        {
            SessionStatusResponse status = await _ksefClient.GetSessionStatusAsync(sessionReferenceNumber, accessToken);

            if (status.Upo?.Pages != null && status.Upo.Pages.Count > 0)
            {
                upoPages = status.Upo;
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        if (upoPages == null)
        {
            throw new TimeoutException("The KSeF system did not generate the UPO within the 5-minute timeout window.");
        }

        UpoPageResponse firstPage = upoPages.Pages.First();

        string upoXml = await _ksefClient.GetUpoAsync(firstPage.DownloadUrl, CancellationToken.None);

        return upoXml;
    }
}