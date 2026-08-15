// Reference host flow adapted from the production PaymentController pattern.
// Persistence types below are illustrative and intentionally domain-neutral.

using OpenMoney.VTB;
using OpenMoney.VTB.Models;

public sealed class VtbPaymentFlowExample
{
    private readonly VtbAcquiringClient _client;
    private readonly IVtbCallbackVerifier _verifier;
    private readonly IVtbPaymentStore _payments;

    public VtbPaymentFlowExample(
        VtbAcquiringClient client,
        IVtbCallbackVerifier verifier,
        IVtbPaymentStore payments)
    {
        _client = client;
        _verifier = verifier;
        _payments = payments;
    }

    public async Task<StartPaymentResponse> StartAsync(
        StartPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount));
        }

        var merchantOrderId = Guid.NewGuid();
        var (redirectOrPayload, bankOrderId) = await _client.StartPaymentAsync(
            merchantOrderId,
            request.Amount,
            request.ByCard,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(redirectOrPayload) || bankOrderId is null)
        {
            throw new InvalidOperationException("VTB did not create the payment.");
        }

        // Persist before returning to the caller. Keep both ids, amount, type and "created" status.
        await _payments.CreateAsync(
            merchantOrderId,
            bankOrderId.Value,
            request.Amount,
            request.ByCard ? "card" : "qr",
            cancellationToken);

        return new StartPaymentResponse
        {
            OrderId = merchantOrderId,
            RedirectUrl = redirectOrPayload
        };
    }

    public async Task HandleCallbackAsync(
        string formUrlEncodedBody,
        CancellationToken cancellationToken = default)
    {
        var callback = VtbCallbackParser.Parse(formUrlEncodedBody);
        if (!_verifier.Verify(callback))
        {
            throw new UnauthorizedAccessException("VTB callback checksum verification failed.");
        }

        var payment = await _payments.FindByBankOrderIdAsync(callback.MdOrder, cancellationToken)
            ?? throw new KeyNotFoundException("The callback does not match a known payment.");
        if (payment.Amount != callback.Amount)
        {
            throw new InvalidOperationException("The callback amount does not match the stored payment.");
        }

        // Update and fulfillment must be one atomic, idempotent host operation. VTB retries callbacks.
        var successful = callback.Operation is "approved" or "deposited";
        await _payments.ApplyVerifiedCallbackOnceAsync(
            payment.Id,
            callback.ProcessingId,
            callback.Operation!,
            callback.PaymentState!,
            successful,
            cancellationToken);
    }
}

public sealed record StoredVtbPayment(Guid Id, long Amount);

public interface IVtbPaymentStore
{
    Task CreateAsync(
        Guid merchantOrderId,
        Guid bankOrderId,
        long amount,
        string paymentType,
        CancellationToken cancellationToken);

    Task<StoredVtbPayment?> FindByBankOrderIdAsync(
        Guid bankOrderId,
        CancellationToken cancellationToken);

    Task ApplyVerifiedCallbackOnceAsync(
        Guid paymentId,
        long processingId,
        string operation,
        string paymentState,
        bool fulfill,
        CancellationToken cancellationToken);
}
