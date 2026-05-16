using Kohinoorea.Shared.Models.Auth;
using Kohinoorea.Shared.Models.Commerce;
using SqlKata.Execution;

namespace Kohinoorea.Server.Services;

public sealed class CommerceRepository : ICommerceRepository
{
    private readonly QueryFactory _queryFactory;

    public CommerceRepository(QueryFactory queryFactory)
    {
        _queryFactory = queryFactory;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(bool onlyActive, string? countryCode = null, CancellationToken cancellationToken = default)
    {
        var query = _queryFactory.Query(AuthSqlKataSchema.ProductsTable)
            .Select(
                AuthSqlKataSchema.ProductColumns.Id,
                AuthSqlKataSchema.ProductColumns.Name,
                AuthSqlKataSchema.ProductColumns.Description,
                AuthSqlKataSchema.ProductColumns.ImageLink,
                AuthSqlKataSchema.ProductColumns.Price,
                AuthSqlKataSchema.ProductColumns.IsActive,
                AuthSqlKataSchema.ProductColumns.IsMaster,
                AuthSqlKataSchema.ProductColumns.CountryCode,
                AuthSqlKataSchema.ProductColumns.CreatedAtUtc)
            .OrderByDesc(AuthSqlKataSchema.ProductColumns.CreatedAtUtc);

        if (onlyActive)
        {
            query = query.Where(AuthSqlKataSchema.ProductColumns.IsActive, true);
        }

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            query = query.Where(q => q
                .Where(AuthSqlKataSchema.ProductColumns.CountryCode, countryCode.ToUpperInvariant())
                .OrWhereNull(AuthSqlKataSchema.ProductColumns.CountryCode));
        }

        var products = await query.GetAsync<ProductDto>(cancellationToken: cancellationToken);
        return products.ToList();
    }

    public async Task<ProductDto?> GetProductByIdAsync(long productId, CancellationToken cancellationToken = default)
    {
        return await _queryFactory
            .Query(AuthSqlKataSchema.ProductsTable)
            .Where(AuthSqlKataSchema.ProductColumns.Id, productId)
            .FirstOrDefaultAsync<ProductDto>(cancellationToken: cancellationToken);
    }

    public async Task<long> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _queryFactory
            .Query(AuthSqlKataSchema.ProductsTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.ProductColumns.Name] = request.Name.Trim(),
                [AuthSqlKataSchema.ProductColumns.Description] = request.Description,
                [AuthSqlKataSchema.ProductColumns.ImageLink] = string.IsNullOrWhiteSpace(request.ImageLink) ? null : request.ImageLink.Trim(),
                [AuthSqlKataSchema.ProductColumns.Price] = request.Price,
                [AuthSqlKataSchema.ProductColumns.IsActive] = request.IsActive,
                [AuthSqlKataSchema.ProductColumns.IsMaster] = request.IsMaster,
                [AuthSqlKataSchema.ProductColumns.CountryCode] = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.ToUpperInvariant().Trim(),
                [AuthSqlKataSchema.ProductColumns.CreatedAtUtc] = now
            }, cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateProductAsync(long productId, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.ProductsTable)
            .Where(AuthSqlKataSchema.ProductColumns.Id, productId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.ProductColumns.Name] = request.Name.Trim(),
                [AuthSqlKataSchema.ProductColumns.Description] = request.Description,
                [AuthSqlKataSchema.ProductColumns.ImageLink] = string.IsNullOrWhiteSpace(request.ImageLink) ? null : request.ImageLink.Trim(),
                [AuthSqlKataSchema.ProductColumns.Price] = request.Price,
                [AuthSqlKataSchema.ProductColumns.IsActive] = request.IsActive,
                [AuthSqlKataSchema.ProductColumns.IsMaster] = request.IsMaster,
                [AuthSqlKataSchema.ProductColumns.CountryCode] = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.ToUpperInvariant().Trim()
            }, cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<bool> SoftDeleteProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.ProductsTable)
            .Where(AuthSqlKataSchema.ProductColumns.Id, productId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.ProductColumns.IsActive] = false
            }, cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<bool> SetProductActiveAsync(long productId, bool isActive, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.ProductsTable)
            .Where(AuthSqlKataSchema.ProductColumns.Id, productId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.ProductColumns.IsActive] = isActive
            }, cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<bool> ForceDeleteProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.ProductsTable)
            .Where(AuthSqlKataSchema.ProductColumns.Id, productId)
            .DeleteAsync(cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<long> CreateOrderAsync(long userId, ProductDto product, int quantity, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var totalAmount = product.Price * quantity;

        return await _queryFactory
            .Query(AuthSqlKataSchema.OrdersTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.OrderColumns.ProductId] = product.Id,
                [AuthSqlKataSchema.OrderColumns.UserId] = userId,
                [AuthSqlKataSchema.OrderColumns.Quantity] = quantity,
                [AuthSqlKataSchema.OrderColumns.UnitPrice] = product.Price,
                [AuthSqlKataSchema.OrderColumns.TotalAmount] = totalAmount,
                [AuthSqlKataSchema.OrderColumns.PaymentMethod] = "Card",
                [AuthSqlKataSchema.OrderColumns.Status] = "Pending",
                [AuthSqlKataSchema.OrderColumns.OrderedAtUtc] = now
            }, cancellationToken: cancellationToken);
    }

    public async Task<long> AddCartItemAsync(long userId, ProductDto product, int quantity, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var totalAmount = product.Price * quantity;

        return await _queryFactory
            .Query(AuthSqlKataSchema.CartItemsTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.CartItemColumns.UserId] = userId,
                [AuthSqlKataSchema.CartItemColumns.ProductId] = product.Id,
                [AuthSqlKataSchema.CartItemColumns.Quantity] = quantity,
                [AuthSqlKataSchema.CartItemColumns.UnitPrice] = product.Price,
                [AuthSqlKataSchema.CartItemColumns.TotalAmount] = totalAmount,
                [AuthSqlKataSchema.CartItemColumns.AddedAtUtc] = now
            }, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<UserOrderDto>> GetUserOrdersAsync(long userId, CancellationToken cancellationToken = default)
    {
        var rows = await _queryFactory
            .Query(AuthSqlKataSchema.OrdersTable + " as o")
            .Join(AuthSqlKataSchema.ProductsTable + " as p", "o." + AuthSqlKataSchema.OrderColumns.ProductId, "p." + AuthSqlKataSchema.ProductColumns.Id)
            .SelectRaw("o.id as OrderId")
            .SelectRaw("o.product_id as ProductId")
            .SelectRaw("p.name as ProductName")
            .SelectRaw("o.ordered_at_utc as OrderedAtUtc")
            .SelectRaw("o.total_amount as TotalAmount")
            .SelectRaw("o.payment_method as PaymentMethod")
            .SelectRaw("o.status as Status")
            .Where("o." + AuthSqlKataSchema.OrderColumns.UserId, userId)
            .OrderByDesc("o." + AuthSqlKataSchema.OrderColumns.OrderedAtUtc)
            .GetAsync<UserOrderDto>(cancellationToken: cancellationToken);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<SupportQueryDto>> GetSupportQueriesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var rows = await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueriesTable)
            .SelectRaw("id as Id")
            .SelectRaw("user_id as UserId")
            .SelectRaw("subject as Subject")
            .SelectRaw("category as Category")
            .SelectRaw("message as Message")
            .SelectRaw("status as Status")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .Where(AuthSqlKataSchema.SupportQueryColumns.UserId, userId)
            .OrderByDesc(AuthSqlKataSchema.SupportQueryColumns.CreatedAtUtc)
            .GetAsync<SupportQueryDto>(cancellationToken: cancellationToken);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<SupportQueryDto>> GetAllSupportQueriesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueriesTable)
            .SelectRaw("id as Id")
            .SelectRaw("user_id as UserId")
            .SelectRaw("subject as Subject")
            .SelectRaw("category as Category")
            .SelectRaw("message as Message")
            .SelectRaw("status as Status")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .OrderByDesc(AuthSqlKataSchema.SupportQueryColumns.CreatedAtUtc)
            .GetAsync<SupportQueryDto>(cancellationToken: cancellationToken);

        return rows.ToList();
    }

    public async Task<SupportQueryDto?> GetSupportQueryByIdAsync(long queryId, CancellationToken cancellationToken = default)
    {
        return await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueriesTable)
            .SelectRaw("id as Id")
            .SelectRaw("user_id as UserId")
            .SelectRaw("subject as Subject")
            .SelectRaw("category as Category")
            .SelectRaw("message as Message")
            .SelectRaw("status as Status")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .Where(AuthSqlKataSchema.SupportQueryColumns.Id, queryId)
            .FirstOrDefaultAsync<SupportQueryDto>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SupportQueryMessageDto>> GetSupportQueryMessagesAsync(long queryId, CancellationToken cancellationToken = default)
    {
        var rows = await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueryMessagesTable)
            .SelectRaw("id as Id")
            .SelectRaw("query_id as QueryId")
            .SelectRaw("sender_role as SenderRole")
            .SelectRaw("sender_user_id as SenderUserId")
            .SelectRaw("message as Message")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .Where(AuthSqlKataSchema.SupportQueryMessageColumns.QueryId, queryId)
            .OrderBy(AuthSqlKataSchema.SupportQueryMessageColumns.CreatedAtUtc)
            .GetAsync<SupportQueryMessageDto>(cancellationToken: cancellationToken);

        return rows.ToList();
    }

    public async Task<long> CreateSupportQueryAsync(long userId, CreateSupportQueryRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var queryId = await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueriesTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.SupportQueryColumns.UserId] = userId,
                [AuthSqlKataSchema.SupportQueryColumns.Subject] = request.Subject.Trim(),
                [AuthSqlKataSchema.SupportQueryColumns.Category] = request.Category,
                [AuthSqlKataSchema.SupportQueryColumns.Message] = request.Message.Trim(),
                [AuthSqlKataSchema.SupportQueryColumns.Status] = "Open",
                [AuthSqlKataSchema.SupportQueryColumns.CreatedAtUtc] = now
            }, cancellationToken: cancellationToken);

        await CreateSupportQueryMessageAsync(queryId, AuthRoles.User, userId, request.Message, cancellationToken);
        return queryId;
    }

    public async Task<long> CreateSupportQueryMessageAsync(long queryId, string senderRole, long? senderUserId, string message, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueryMessagesTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.SupportQueryMessageColumns.QueryId] = queryId,
                [AuthSqlKataSchema.SupportQueryMessageColumns.SenderRole] = senderRole,
                [AuthSqlKataSchema.SupportQueryMessageColumns.SenderUserId] = senderUserId,
                [AuthSqlKataSchema.SupportQueryMessageColumns.Message] = message.Trim(),
                [AuthSqlKataSchema.SupportQueryMessageColumns.CreatedAtUtc] = now
            }, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<OrderTraceDto>> GetOrderTraceAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _queryFactory
            .Query(AuthSqlKataSchema.OrdersTable + " as o")
            .Join(AuthSqlKataSchema.ProductsTable + " as p", "o." + AuthSqlKataSchema.OrderColumns.ProductId, "p." + AuthSqlKataSchema.ProductColumns.Id)
            .Join(AuthSqlKataSchema.UsersTable + " as u", "o." + AuthSqlKataSchema.OrderColumns.UserId, "u." + AuthSqlKataSchema.UserColumns.Id)
            .SelectRaw("o.id as OrderId")
            .SelectRaw("o.product_id as ProductId")
            .SelectRaw("p.name as ProductName")
            .SelectRaw("o.unit_price as UnitPrice")
            .SelectRaw("o.quantity as Quantity")
            .SelectRaw("o.total_amount as TotalAmount")
            .SelectRaw("o.user_id as UserId")
            .SelectRaw("u.email as UserEmail")
            .SelectRaw("u.full_name as UserFullName")
            .SelectRaw("o.ordered_at_utc as OrderedAtUtc")
            .SelectRaw("o.status as Status")
            .OrderByDesc("o." + AuthSqlKataSchema.OrderColumns.OrderedAtUtc)
            .GetAsync<OrderTraceDto>(cancellationToken: cancellationToken);

        return rows.ToList();
    }

    public async Task<bool> UpdateOrderStatusAsync(long orderId, string status, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.OrdersTable)
            .Where(AuthSqlKataSchema.OrderColumns.Id, orderId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.OrderColumns.Status] = status
            }, cancellationToken: cancellationToken);

        return affected > 0;
    }
}
