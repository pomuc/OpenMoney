using OpenMoney.TBank.Models;

namespace OpenMoney.TBank.Client;

public interface ITBankAcquiringClient
{
    Task<HttpPayInResponseInit> InitPayInAsync(RequestInitPaymentContext request, CancellationToken cancellationToken = default);
    Task<HttpPayInResponseCharge> ChargeAsync(RequestChargePaymentContext request, CancellationToken cancellationToken = default);
    Task<HttpPayInResponseConfirm> ConfirmAsync(RequestConfirmPaymentContext request, CancellationToken cancellationToken = default);
    Task<HttpPayInResponseCancel> CancelAsync(RequestCancelPaymentContext request, CancellationToken cancellationToken = default);
    Task<HttpPayInResponseStatus> GetStatusAsync(RequestGetStatePaymentContext request, CancellationToken cancellationToken = default);
    Task<HttpPayInResponseCheckOrder> CheckOrderAsync(RequestCheckOrderContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponseInit> InitPayoutAsync(RequestInitPayoutContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponsePayment> PaymentAsync(RequestPayoutPaymentContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponseInit> InitMomentPayoutAsync(RequestInitMomentPayoutContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponsePayment> MomentPaymentAsync(RequestMomentPayoutPaymentContext request, CancellationToken cancellationToken = default);
    Task<HttpPayInResponseAddCard> AddCardAsync(RequestAddCardContext request, CancellationToken cancellationToken = default);
    Task<HttpPayInResponseRemoveCard> RemoveCardAsync(RequestRemoveCardContext request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HttpPayCardResponse>> GetCardListAsync(RequestGetCardListContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponseAddCustomer> AddPayoutCustomerAsync(RequestAddPayoutCustomerContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponseGetCustomer> GetPayoutCustomerAsync(RequestGetPayoutCustomerContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponseRemoveCustomer> RemovePayoutCustomerAsync(RequestRemovePayoutCustomerContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponseAddCard> AddPayoutCardAsync(RequestAddPayoutCardContext request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HttpPayOutResponseCard>> GetPayoutCardsAsync(RequestGetPayoutCardsContext request, CancellationToken cancellationToken = default);
    Task<HttpPayOutResponseRemoveCard> RemovePayoutCardAsync(RequestRemovePayoutCardContext request, CancellationToken cancellationToken = default);
    Task<HttpPayInResponseCreateSecureTransaction> CreateSecureDealAsync(RequestCreateSecureDealContext request, CancellationToken cancellationToken = default);
    Task<HttpGetQr> CreateQrAsync(RequestGetQrContext request, CancellationToken cancellationToken = default);
    Task<HttpResponsePaycheck> MakePaycheckAsync(RequestPaycheckContext request, CancellationToken cancellationToken = default);
    Task<HttpResponsePaycheck> MakeReturnPaycheckAsync(RequestPaycheckContext request, CancellationToken cancellationToken = default);
    Task<HttpResponsePaycheck> MakeAgentPaycheckAsync(RequestAgentPaycheckContext request, CancellationToken cancellationToken = default);
    Task<HttpResponsePaycheck> MakeReturnAgentPaycheckAsync(RequestAgentPaycheckContext request, CancellationToken cancellationToken = default);
}
