using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using OpenMoney.TBank.Models;

namespace OpenMoney.TBank.Signing;

/// <summary>Creates certificate-backed signatures for E2C requests.</summary>
public static class SignatureHelper
{
    public static void SignRequest(
        IHasSignature request,
        string certificatePem,
        string privateKeyPem,
        string? privateKeyPassword = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(certificatePem))
            throw new ArgumentException("A signing certificate is required.", nameof(certificatePem));
        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("A signing private key is required.", nameof(privateKeyPem));

        using var certificate = CreateCertificate(certificatePem, privateKeyPem, privateKeyPassword);
        SignRequest(request, certificate);
    }

    public static void SignRequest(IHasSignature request, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(certificate);

        var digest = request.ToTinkoffHashTokenBytes();
        using var rsa = certificate.GetRSAPrivateKey()
            ?? throw new CryptographicException("The signing certificate does not contain an RSA private key.");

        request.DigestValue = Convert.ToBase64String(digest);
        request.SignatureValue = Convert.ToBase64String(
            rsa.SignData(digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        request.X509SerialNumber = certificate.GetSerialNumberString();
    }

    private static X509Certificate2 CreateCertificate(
        string certificatePem,
        string privateKeyPem,
        string? password) =>
        string.IsNullOrEmpty(password)
            ? X509Certificate2.CreateFromPem(certificatePem, privateKeyPem)
            : X509Certificate2.CreateFromEncryptedPem(certificatePem, privateKeyPem, password);
}
