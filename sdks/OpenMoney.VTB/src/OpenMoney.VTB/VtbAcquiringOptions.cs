namespace OpenMoney.VTB;

/// <summary>
/// Configuration for VTB RBS acquiring (production or UAT).
/// Never commit real tokens — load from environment / secret store.
/// </summary>
public sealed class VtbAcquiringOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "VtbAcquiring";

    /// <summary>
    /// Production: https://platezh.vtb24.ru/payment
    /// UAT/sandbox: https://vtb.rbsuat.com/payment
    /// </summary>
    public string BaseUrl { get; set; } = "https://vtb.rbsuat.com/payment";

    /// <summary>Merchant API token issued by VTB.</summary>
    public string Token { get; set; } = "";

    /// <summary>Return URL after card payment form.</summary>
    public string ReturnUrl { get; set; } = "https://example.com/payment/return";

    /// <summary>QR matrix height for SBP dynamic QR.</summary>
    public int QrHeight { get; set; } = 10;

    /// <summary>QR matrix width for SBP dynamic QR.</summary>
    public int QrWidth { get; set; } = 10;
}
