using KSeF.Client.Api;
using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.DI;
using KSEFinvoiceSender.Ksef.Infrastructure;
using KSEFinvoiceSender.Ksef.Interfaces;
using KSEFinvoiceSender.Ksef.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KSEFinvoiceSender.Configuration;
namespace KSEFinvoiceSender;

public class Program1
{
    public async Task TestedConnectionEstablishing()
    {
        // ── Configuration ─────────────────────────────────────────────────────────────
        string NIP = ConfigSingleton.Instance.ksefConfig.mlNIP;
        // Your NIP, 10 digits, no "PL"
        string KSEF_TOKEN = ConfigSingleton.Instance.ksefConfig.mlApiToken;// Token from KSeF portal
        // ── DI Setup ──────────────────────────────────────────────────────────────────
        IServiceCollection services = new ServiceCollection();

        services.AddKSeFClient(options =>
        {
            options.BaseUrl = KsefEnvironmentsUris.DEMO;
            options.BaseQRUrl = KsefQREnvironmentsUris.DEMO;
        });

        services.AddCryptographyClient();

        IServiceProvider provider = services.BuildServiceProvider();

        var authorizationClient = provider.GetRequiredService<IAuthorizationClient>();
        var cryptographyService = provider.GetRequiredService<ICryptographyService>();
        await cryptographyService.WarmupAsync();
        var ksefClient = provider.GetRequiredService<IKSeFClient>();

        // ── Authenticate with KSeF token ─────────────────────────────────────────────
        Console.WriteLine("Authenticating...");

        IAuthCoordinator authCoordinator = new AuthCoordinator(authorizationClient);

        AuthenticationOperationStatusResponse authResult = await authCoordinator.AuthKsefTokenAsync(
            AuthenticationTokenContextIdentifierType.Nip,  // contextIdentifierType
            NIP,                                            // contextIdentifierValue
            KSEF_TOKEN,                                     // tokenKsef
            cryptographyService,                            // cryptographyService
            EncryptionMethodEnum.Rsa                      // encryptionMethod (default)
        );

        if (authResult?.AccessToken?.Token is null)
        {
            Console.WriteLine("Authentication failed — no access token returned.");
            return;
        }

        string accessToken = authResult.AccessToken.Token;
        Console.WriteLine($"Authenticated. Token: {accessToken[..20]}...");

        // ── Call KSeF API with the JWT access token ───────────────────────────────────
        try
        {
            var result = await ksefClient.GetCertificateLimitsAsync(accessToken, CancellationToken.None);

            Console.WriteLine("Response:");
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            var refreshed = await authCoordinator.RefreshAccessTokenAsync(authResult.RefreshToken.Token);

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error calling KSeF:");
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (provider is IDisposable d) d.Dispose();
        }

    }

    public async Task NotTestedInvoiceSending()
    {


    
        // ── Configuration ─────────────────────────────────────────────────────────────
        string NIP = ConfigSingleton.Instance.ksefConfig.mlNIP;
        // Your NIP, 10 digits, no "PL"
        string KSEF_TOKEN = ConfigSingleton.Instance.ksefConfig.mlApiToken;
        //const string INVOICE_PATH_ABSOLUTE = @"C:\Users\matbl\Desktop\everything\Studia\Inter-Semester\DlaTaty\ksef\ksef-client-csharp\KSeF.Client.Tests\Templates\invoice-template-fa-3.xml";
        const string INVOICE_PATH_ABSOLUTE = @"C:\Users\matbl\Desktop\everything\Studia\Inter-Semester\DlaTaty\ksef\KSEFinvoiceSender\Miscellaneous\test.xml";

        const string UPO_PATH = @"C:\Users\matbl\Desktop\everything\Studia\Inter-Semester\DlaTaty\ksef\KSEFinvoiceSender\Miscellaneous\RandomUPOs";

        // FA(3) form code — adjust if your invoice uses a different schema version
        const string FORM_SYSTEM_CODE = "FA (3)";
        const string FORM_SCHEMA_VERSION = "1-0E";
        const string FORM_VALUE = "FA";

        const int INVOICE_STATUS_OK = 200;

        // ── DI Setup ──────────────────────────────────────────────────────────────────
        IServiceCollection services = new ServiceCollection();
        services.AddKSeFClient(options =>
        {
            options.BaseUrl = KsefEnvironmentsUris.DEMO;
            options.BaseQRUrl = KsefQREnvironmentsUris.DEMO;
        });
        services.AddCryptographyClient();
        IServiceProvider provider = services.BuildServiceProvider();

        var authorizationClient = provider.GetRequiredService<IAuthorizationClient>();
        var cryptographyService = provider.GetRequiredService<ICryptographyService>();
        var ksefClient = provider.GetRequiredService<IKSeFClient>();

        try
        {
            // ── Warmup ────────────────────────────────────────────────────────────────
            Console.WriteLine("[1] Warming up cryptography...");
            await cryptographyService.WarmupAsync();

            // ── Authenticate ──────────────────────────────────────────────────────────
            Console.WriteLine("[2] Authenticating...");
            IAuthCoordinator authCoordinator = new AuthCoordinator(authorizationClient);
            AuthenticationOperationStatusResponse authResult = await authCoordinator.AuthKsefTokenAsync(
                AuthenticationTokenContextIdentifierType.Nip,
                NIP,
                KSEF_TOKEN,
                cryptographyService,
                EncryptionMethodEnum.Rsa
            );
            string accessToken = authResult.AccessToken.Token;
            Console.WriteLine("Authenticated successfully.");

            // ── Open online session ───────────────────────────────────────────────────
            Console.WriteLine("[3] Opening session...");
            EncryptionData encryptionData = cryptographyService.GetEncryptionData();

            OpenOnlineSessionRequest openRequest = OpenOnlineSessionRequestBuilder
                .Create()
                .WithFormCode(FORM_SYSTEM_CODE, FORM_SCHEMA_VERSION, FORM_VALUE)
                .WithEncryption(
                    encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                    encryptionData.EncryptionInfo.InitializationVector)
                .Build();

            OpenOnlineSessionResponse session = await ksefClient.OpenOnlineSessionAsync(
                openRequest, accessToken);
            Console.WriteLine($"Session opened: {session.ReferenceNumber}");

            // ── Verify session is active ──────────────────────────────────────────────
            Console.WriteLine("[4] Checking session status...");
            SessionStatusResponse sessionStatus = await ksefClient.GetSessionStatusAsync(
                session.ReferenceNumber, accessToken);

            if (sessionStatus.ValidUntil.HasValue && sessionStatus.ValidUntil < DateTimeOffset.UtcNow)
                throw new InvalidOperationException(
                    $"Session expired. Status: {sessionStatus.Status.Code} - {sessionStatus.Status.Description}");

            Console.WriteLine($"Session active. Valid until: {sessionStatus.ValidUntil}");

            // ── Encrypt and send invoice ──────────────────────────────────────────────
            Console.WriteLine("[5] Preparing and sending invoice...");
            byte[] invoiceBytes = await File.ReadAllBytesAsync(INVOICE_PATH_ABSOLUTE);

            FileMetadata plainMetadata = cryptographyService.GetMetaData(invoiceBytes);
            byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(
                invoiceBytes, encryptionData.CipherKey, encryptionData.CipherIv);
            FileMetadata encryptedMetadata = cryptographyService.GetMetaData(encryptedInvoice);

            SendInvoiceRequest sendRequest = SendInvoiceOnlineSessionRequestBuilder
                .Create()
                .WithInvoiceHash(plainMetadata.HashSHA, plainMetadata.FileSize)
                .WithEncryptedDocumentHash(encryptedMetadata.HashSHA, encryptedMetadata.FileSize)
                .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
                .Build();

            SendInvoiceResponse sendResponse = await ksefClient.SendOnlineSessionInvoiceAsync(
                sendRequest, session.ReferenceNumber, accessToken);
            Console.WriteLine($"Invoice sent. Reference: {sendResponse.ReferenceNumber}");

            // ── Poll invoice status until accepted ────────────────────────────────────
            Console.WriteLine("[7] Waiting for invoice to be processed...");
            SessionInvoice invoiceStatus = null;
            DateTime timeout = DateTime.UtcNow.AddMinutes(2);
            while (DateTime.UtcNow < timeout)
            {
                SessionInvoice inv = await ksefClient.GetSessionInvoiceAsync(
                    session.ReferenceNumber, sendResponse.ReferenceNumber, accessToken);

                if (inv.Status.Code == INVOICE_STATUS_OK)
                {
                    invoiceStatus = inv;
                    break;
                }
                if (inv.Status.Code >= 400)
                    throw new InvalidOperationException(
                        $"Invoice rejected. Code: {inv.Status.Code} - {inv.Status.Description}");

                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            if (invoiceStatus is null)
                throw new TimeoutException("Invoice was not processed within 2 minutes.");

            Console.WriteLine("[8] Invoice accepted by KSeF.");
            Console.WriteLine($"    KSeF Number:      {invoiceStatus.KsefNumber}");
            Console.WriteLine($"    Acquisition Date: {invoiceStatus.AcquisitionDate}");

            // ── Close session ─────────────────────────────────────────────────────────
            Console.WriteLine("[9] Closing session...");
            await ksefClient.CloseOnlineSessionAsync(session.ReferenceNumber, accessToken);
            Console.WriteLine("Session closed.");

            // ── Poll for UPO ─────────────────────────────────────────────────────────
            Console.WriteLine("[10] Waiting for UPO generation...");
            UpoResponse upoPages = null;
            timeout = DateTime.UtcNow.AddMinutes(5);
            while (DateTime.UtcNow < timeout)
            {
                SessionStatusResponse status = await ksefClient.GetSessionStatusAsync(
                    session.ReferenceNumber, accessToken);

                if (status.Upo?.Pages != null && status.Upo.Pages.Count > 0)
                {
                    upoPages = status.Upo;
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            if (upoPages is null)
                throw new TimeoutException("UPO was not generated within 5 minutes.");

            // ── Download and save UPO ─────────────────────────────────────────────────
            Console.WriteLine("[11] Downloading UPO...");
            UpoPageResponse firstPage = upoPages.Pages.First();
            string upoXml = await ksefClient.GetUpoAsync(
                firstPage.DownloadUrl, CancellationToken.None);

            string fileName = $"UPO_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
         
            string fullFilePath = Path.Combine(UPO_PATH, fileName);
            await File.WriteAllTextAsync(fullFilePath, upoXml);
            Console.WriteLine($"UPO saved to: {fullFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (provider is IDisposable d) d.Dispose();
        }

    }


    public async Task RunIntegrationTestAsync()
    {
        // 1. Setup Dependency Injection (Combining your original setup with our new classes)
        var services = new ServiceCollection();

        services.AddKSeFClient(options =>
        {
            // Set these using the actual enums from your NuGet package
             options.BaseUrl = KsefEnvironmentsUris.DEMO;
             options.BaseQRUrl = KsefQREnvironmentsUris.DEMO;
        });
        services.AddCryptographyClient();

        // Register our new custom managers
        services.AddScoped<IKsefConnectionManager, KsefConnectionManager>();
        services.AddScoped<IKsefDocumentSender, KsefDocumentSender>();

        var provider = services.BuildServiceProvider();

        // 2. Resolve the required services
        var connectionManager = provider.GetRequiredService<IKsefConnectionManager>();
        var documentSender = provider.GetRequiredService<IKsefDocumentSender>();

        // 3. Prepare Test Data
        string xmlPath = @"C:\Users\matbl\Desktop\everything\Studia\Inter-Semester\DlaTaty\ksef\KSEFinvoiceSender\Miscellaneous\test.xml";
        byte[] invoiceBytes = await File.ReadAllBytesAsync(xmlPath);

        // Put your actual test credentials here
        var credentials = new KsefCredentials(
            Nip: ConfigSingleton.Instance.ksefConfig.mlNIP,
            ApiToken: ConfigSingleton.Instance.ksefConfig.mlApiToken
        );

        Console.WriteLine("Starting KSeF Integration Test...");

        KsefSession? activeSession = null;

        try
        {
            // STEP 1: Connect
            Console.WriteLine("\n[1] Authenticating and Opening Session...");
            activeSession = await connectionManager.ConnectAsync(credentials);
            Console.WriteLine($"    Session Opened! Ref: {activeSession.SessionReferenceNumber}");

            // STEP 2: Send Invoice
            Console.WriteLine("\n[2] Encrypting, Sending, and Polling Invoice...");
            var sendResult = await documentSender.SendInvoiceAsync(invoiceBytes, activeSession);

            if (sendResult.IsAccepted)
            {
                Console.WriteLine($"    [SUCCESS] Invoice Accepted by KSeF!");
                Console.WriteLine($"    KSeF Number: {sendResult.KsefNumber}");
                Console.WriteLine($"    Acquisition Date: {sendResult.AcquisitionDate}");
            }
            else
            {
                Console.WriteLine($"    [FAILED] Invoice Rejected: {sendResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] A fatal exception occurred: {ex.Message}");
        }
        finally
        {
            // STEP 3 & 4: Disconnect and get UPO (Always runs, even if sending fails)
            if (activeSession != null)
            {
                try
                {
                    Console.WriteLine("\n[3] Closing Session...");
                    await connectionManager.DisconnectAsync(activeSession);
                    Console.WriteLine("    Session Closed.");

                    Console.WriteLine("\n[4] Waiting for UPO to be generated...");
                    string upoXml = await connectionManager.DownloadSessionUpoAsync(
                        activeSession.SessionReferenceNumber,
                        activeSession.AccessToken);

                    Console.WriteLine("    [SUCCESS] UPO Downloaded!");

                    // Save UPO to disk
                    string upoDirectory = @"C:\Users\matbl\Desktop\everything\Studia\Inter-Semester\DlaTaty\ksef\KSEFinvoiceSender\Miscellaneous\RandomUPOs";
                    Directory.CreateDirectory(upoDirectory); // Ensure the folder exists

                    string upoPath = Path.Combine(upoDirectory, $"UPO_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                    await File.WriteAllTextAsync(upoPath, upoXml);
                    Console.WriteLine($"    Saved UPO to: {upoPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[ERROR] Failed during cleanup or UPO retrieval: {ex.Message}");
                }
            }
        }

        Console.WriteLine("\nTest complete.");
    }
}
