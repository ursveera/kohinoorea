using Kohinoorea.Shared.Models.Commerce;

namespace Kohinoorea.Server.Services;

public interface ICommerceRepository
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(bool onlyActive, string? countryCode = null, CancellationToken cancellationToken = default);

    Task<ProductDto?> GetProductByIdAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateProductAsync(long productId, CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteProductAsync(long productId, CancellationToken cancellationToken = default);

    Task<bool> SetProductActiveAsync(long productId, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> ForceDeleteProductAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateOrderAsync(long userId, ProductDto product, int quantity, CancellationToken cancellationToken = default);

    Task<long> AddCartItemAsync(long userId, ProductDto product, int quantity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserOrderDto>> GetUserOrdersAsync(long userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqDto>> GetFaqsAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<FaqDto?> GetFaqByIdAsync(long faqId, CancellationToken cancellationToken = default);

    Task<long> CreateFaqAsync(CreateFaqRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateFaqAsync(long faqId, CreateFaqRequest request, CancellationToken cancellationToken = default);

    Task<bool> SetFaqActiveAsync(long faqId, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportQueryDto>> GetSupportQueriesAsync(long userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportQueryDto>> GetAllSupportQueriesAsync(CancellationToken cancellationToken = default);

    Task<SupportQueryDto?> GetSupportQueryByIdAsync(long queryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportQueryMessageDto>> GetSupportQueryMessagesAsync(long queryId, CancellationToken cancellationToken = default);

    Task<long> CreateSupportQueryAsync(long userId, CreateSupportQueryRequest request, CancellationToken cancellationToken = default);

    Task<long> CreateSupportQueryMessageAsync(long queryId, string senderRole, long? senderUserId, string message, CancellationToken cancellationToken = default);

    Task<bool> UpdateSupportQueryStatusAsync(long queryId, string status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderTraceDto>> GetOrderTraceAsync(CancellationToken cancellationToken = default);

    Task<bool> UpdateOrderStatusAsync(long orderId, string status, CancellationToken cancellationToken = default);
}
