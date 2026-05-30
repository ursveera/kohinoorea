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
        var normalizedCountry = string.IsNullOrWhiteSpace(countryCode)
            ? null
            : countryCode.Trim().ToUpperInvariant();

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
                AuthSqlKataSchema.ProductColumns.ValidFromUtc,
                AuthSqlKataSchema.ProductColumns.ValidToUtc,
                AuthSqlKataSchema.ProductColumns.CreatedAtUtc)
            .OrderByDesc(AuthSqlKataSchema.ProductColumns.CreatedAtUtc);

        if (onlyActive)
        {
            query = query.Where(AuthSqlKataSchema.ProductColumns.IsActive, true);
        }

        if (!string.IsNullOrWhiteSpace(normalizedCountry))
        {
            query = query.Where(q => q
                .Where(AuthSqlKataSchema.ProductColumns.CountryCode, normalizedCountry)
                .OrWhereNull(AuthSqlKataSchema.ProductColumns.CountryCode));
        }

        var products = (await query.GetAsync<ProductDto>(cancellationToken: cancellationToken)).ToList();

        if (string.IsNullOrWhiteSpace(normalizedCountry))
        {
            return products;
        }

        // Prefer country-specific rows over global (NULL country_code) defaults.
        // This avoids returning duplicates when both exist for the same logical product.
        return products
            .OrderByDescending(p => string.Equals(p.CountryCode, normalizedCountry, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(p => p.CreatedAtUtc)
            .GroupBy(p => $"{(p.IsMaster ? "M" : "P")}:{p.Name}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToList();
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
                [AuthSqlKataSchema.ProductColumns.ValidFromUtc] = request.ValidFromUtc,
                [AuthSqlKataSchema.ProductColumns.ValidToUtc] = request.ValidToUtc,
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
                [AuthSqlKataSchema.ProductColumns.CountryCode] = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.ToUpperInvariant().Trim(),
                [AuthSqlKataSchema.ProductColumns.ValidFromUtc] = request.ValidFromUtc,
                [AuthSqlKataSchema.ProductColumns.ValidToUtc] = request.ValidToUtc
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

    public async Task<long> CreateOrderAsync(long userId, ProductDto product, int quantity, string paymentMethod = "Card", CancellationToken cancellationToken = default)
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
                [AuthSqlKataSchema.OrderColumns.PaymentMethod] = string.IsNullOrWhiteSpace(paymentMethod) ? "Card" : paymentMethod.Trim(),
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
            .SelectRaw("o.unit_price as UnitPrice")
            .SelectRaw("o.quantity as Quantity")
            .SelectRaw("o.ordered_at_utc as OrderedAtUtc")
            .SelectRaw("o.total_amount as TotalAmount")
            .SelectRaw("o.payment_method as PaymentMethod")
            .SelectRaw("o.status as Status")
            .Where("o." + AuthSqlKataSchema.OrderColumns.UserId, userId)
            .OrderByDesc("o." + AuthSqlKataSchema.OrderColumns.OrderedAtUtc)
            .GetAsync<UserOrderDto>(cancellationToken: cancellationToken);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<FaqDto>> GetFaqsAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _queryFactory
            .Query(AuthSqlKataSchema.FaqsTable)
            .SelectRaw("id as Id")
            .SelectRaw("question as Question")
            .SelectRaw("answer as Answer")
            .SelectRaw("display_order as DisplayOrder")
            .SelectRaw("is_active as IsActive")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .OrderBy(AuthSqlKataSchema.FaqColumns.DisplayOrder)
            .OrderByDesc(AuthSqlKataSchema.FaqColumns.CreatedAtUtc);

        if (onlyActive)
        {
            query = query.Where(AuthSqlKataSchema.FaqColumns.IsActive, true);
        }

        var rows = await query.GetAsync<FaqDto>(cancellationToken: cancellationToken);
        return rows.ToList();
    }

    public async Task<FaqDto?> GetFaqByIdAsync(long faqId, CancellationToken cancellationToken = default)
    {
        return await _queryFactory
            .Query(AuthSqlKataSchema.FaqsTable)
            .SelectRaw("id as Id")
            .SelectRaw("question as Question")
            .SelectRaw("answer as Answer")
            .SelectRaw("display_order as DisplayOrder")
            .SelectRaw("is_active as IsActive")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .Where(AuthSqlKataSchema.FaqColumns.Id, faqId)
            .FirstOrDefaultAsync<FaqDto>(cancellationToken: cancellationToken);
    }

    public async Task<long> CreateFaqAsync(CreateFaqRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _queryFactory
            .Query(AuthSqlKataSchema.FaqsTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.FaqColumns.Question] = request.Question.Trim(),
                [AuthSqlKataSchema.FaqColumns.Answer] = request.Answer.Trim(),
                [AuthSqlKataSchema.FaqColumns.DisplayOrder] = request.DisplayOrder,
                [AuthSqlKataSchema.FaqColumns.IsActive] = request.IsActive,
                [AuthSqlKataSchema.FaqColumns.CreatedAtUtc] = now
            }, cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateFaqAsync(long faqId, CreateFaqRequest request, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.FaqsTable)
            .Where(AuthSqlKataSchema.FaqColumns.Id, faqId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.FaqColumns.Question] = request.Question.Trim(),
                [AuthSqlKataSchema.FaqColumns.Answer] = request.Answer.Trim(),
                [AuthSqlKataSchema.FaqColumns.DisplayOrder] = request.DisplayOrder,
                [AuthSqlKataSchema.FaqColumns.IsActive] = request.IsActive
            }, cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<bool> SetFaqActiveAsync(long faqId, bool isActive, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.FaqsTable)
            .Where(AuthSqlKataSchema.FaqColumns.Id, faqId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.FaqColumns.IsActive] = isActive
            }, cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<bool> DeleteFaqAsync(long faqId, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.FaqsTable)
            .Where(AuthSqlKataSchema.FaqColumns.Id, faqId)
            .DeleteAsync(cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<IReadOnlyList<ContactMessageDto>> GetContactMessagesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _queryFactory
            .Query(AuthSqlKataSchema.ContactMessagesTable)
            .SelectRaw("id as Id")
            .SelectRaw("name as Name")
            .SelectRaw("email as Email")
            .SelectRaw("subject as Subject")
            .SelectRaw("message as Message")
            .SelectRaw("is_replied as IsReplied")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .SelectRaw("last_replied_at_utc as LastRepliedAtUtc")
            .OrderByDesc(AuthSqlKataSchema.ContactMessageColumns.CreatedAtUtc)
            .GetAsync<ContactMessageDto>(cancellationToken: cancellationToken);

        return rows.ToList();
    }

    public async Task<ContactMessageDto?> GetContactMessageByIdAsync(long contactMessageId, CancellationToken cancellationToken = default)
    {
        return await _queryFactory
            .Query(AuthSqlKataSchema.ContactMessagesTable)
            .SelectRaw("id as Id")
            .SelectRaw("name as Name")
            .SelectRaw("email as Email")
            .SelectRaw("subject as Subject")
            .SelectRaw("message as Message")
            .SelectRaw("is_replied as IsReplied")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .SelectRaw("last_replied_at_utc as LastRepliedAtUtc")
            .Where(AuthSqlKataSchema.ContactMessageColumns.Id, contactMessageId)
            .FirstOrDefaultAsync<ContactMessageDto>(cancellationToken: cancellationToken);
    }

    public async Task<long> CreateContactMessageAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _queryFactory
            .Query(AuthSqlKataSchema.ContactMessagesTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.ContactMessageColumns.Name] = request.Name.Trim(),
                [AuthSqlKataSchema.ContactMessageColumns.Email] = request.Email.Trim().ToLowerInvariant(),
                [AuthSqlKataSchema.ContactMessageColumns.Subject] = request.Subject.Trim(),
                [AuthSqlKataSchema.ContactMessageColumns.Message] = request.Message.Trim(),
                [AuthSqlKataSchema.ContactMessageColumns.IsReplied] = false,
                [AuthSqlKataSchema.ContactMessageColumns.CreatedAtUtc] = now,
                [AuthSqlKataSchema.ContactMessageColumns.LastRepliedAtUtc] = null
            }, cancellationToken: cancellationToken);
    }

    public async Task<bool> MarkContactMessageRepliedAsync(long contactMessageId, DateTime repliedAtUtc, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.ContactMessagesTable)
            .Where(AuthSqlKataSchema.ContactMessageColumns.Id, contactMessageId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.ContactMessageColumns.IsReplied] = true,
                [AuthSqlKataSchema.ContactMessageColumns.LastRepliedAtUtc] = repliedAtUtc
            }, cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<bool> DeleteContactMessageAsync(long contactMessageId, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.ContactMessagesTable)
            .Where(AuthSqlKataSchema.ContactMessageColumns.Id, contactMessageId)
            .DeleteAsync(cancellationToken: cancellationToken);

        return affected > 0;
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

    public async Task<bool> UpdateSupportQueryStatusAsync(long queryId, string status, CancellationToken cancellationToken = default)
    {
        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueriesTable)
            .Where(AuthSqlKataSchema.SupportQueryColumns.Id, queryId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.SupportQueryColumns.Status] = status
            }, cancellationToken: cancellationToken);

        return affected > 0;
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

    public async Task<OrderTraceDto?> GetOrderTraceByIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await _queryFactory
            .Query(AuthSqlKataSchema.OrdersTable + " as o")
            .Join(AuthSqlKataSchema.ProductsTable + " as p", "o." + AuthSqlKataSchema.OrderColumns.ProductId, "p." + AuthSqlKataSchema.ProductColumns.Id)
            .Join(AuthSqlKataSchema.UsersTable + " as u", "o." + AuthSqlKataSchema.OrderColumns.UserId, "u." + AuthSqlKataSchema.UserColumns.Id)
            .SelectRaw("o.id as OrderId")
            .SelectRaw("o.product_id as ProductId")
            .SelectRaw("p.name as ProductName")
            .SelectRaw("o.payment_method as PaymentMethod")
            .SelectRaw("o.unit_price as UnitPrice")
            .SelectRaw("o.quantity as Quantity")
            .SelectRaw("o.total_amount as TotalAmount")
            .SelectRaw("o.user_id as UserId")
            .SelectRaw("u.email as UserEmail")
            .SelectRaw("u.full_name as UserFullName")
            .SelectRaw("o.ordered_at_utc as OrderedAtUtc")
            .SelectRaw("o.status as Status")
            .Where("o." + AuthSqlKataSchema.OrderColumns.Id, orderId)
            .FirstOrDefaultAsync<OrderTraceDto>(cancellationToken: cancellationToken);
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

    public async Task<bool> SupportQueryHasAdminMessagesAsync(long queryId, CancellationToken cancellationToken = default)
    {
        var exists = await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueryMessagesTable)
            .Where(AuthSqlKataSchema.SupportQueryMessageColumns.QueryId, queryId)
            .Where(AuthSqlKataSchema.SupportQueryMessageColumns.SenderRole, AuthRoles.Admin)
            .ExistsAsync(cancellationToken: cancellationToken);

        return exists;
    }

    public async Task<bool> DeleteSupportQueryAsync(long queryId, CancellationToken cancellationToken = default)
    {
        // Delete messages first (FK safety), then delete the query.
        await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueryMessagesTable)
            .Where(AuthSqlKataSchema.SupportQueryMessageColumns.QueryId, queryId)
            .DeleteAsync(cancellationToken: cancellationToken);

        var affected = await _queryFactory
            .Query(AuthSqlKataSchema.SupportQueriesTable)
            .Where(AuthSqlKataSchema.SupportQueryColumns.Id, queryId)
            .DeleteAsync(cancellationToken: cancellationToken);

        return affected > 0;
    }

    public async Task<IReadOnlyList<ActivePlanDto>> GetUserPlansForAdminAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _queryFactory
            .Query(AuthSqlKataSchema.OrdersTable + " as o")
            .Join(AuthSqlKataSchema.ProductsTable + " as p", "o." + AuthSqlKataSchema.OrderColumns.ProductId, "p." + AuthSqlKataSchema.ProductColumns.Id)
            .Join(AuthSqlKataSchema.UsersTable + " as u", "o." + AuthSqlKataSchema.OrderColumns.UserId, "u." + AuthSqlKataSchema.UserColumns.Id)
            .SelectRaw("o.id as OrderId")
            .SelectRaw("o.user_id as UserId")
            .SelectRaw("u.email as UserEmail")
            .SelectRaw("u.full_name as UserFullName")
            .SelectRaw("o.product_id as ProductId")
            .SelectRaw("p.name as ProductName")
            .SelectRaw("o.payment_method as PaymentMethod")
            .SelectRaw("o.unit_price as UnitPrice")
            .SelectRaw("o.quantity as Quantity")
            .SelectRaw("o.ordered_at_utc as OrderedAtUtc")
            .SelectRaw("p.valid_from_utc as ValidFromUtc")
            .SelectRaw("p.valid_to_utc as ValidToUtc")
            .Where("o." + AuthSqlKataSchema.OrderColumns.Status, "Completed")
            .OrderByDesc("o." + AuthSqlKataSchema.OrderColumns.OrderedAtUtc)
            .GetAsync<ActivePlanDto>(cancellationToken: cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.PlanState = ResolvePlanState(now, row.ValidFromUtc, row.ValidToUtc);
            var (eligible, reminderType) = ResolveReminder(now, row.ValidToUtc);
            row.IsEligibleForReminder = eligible;
            row.ReminderType = reminderType;
        }

        return rows.ToList();
    }

    private static string ResolvePlanState(DateTime nowUtc, DateTime? validFromUtc, DateTime? validToUtc)
    {
        if (validFromUtc is null && validToUtc is null) return "Unknown";
        if (validFromUtc is not null && nowUtc < validFromUtc.Value) return "Upcoming";
        if (validToUtc is not null && nowUtc > validToUtc.Value) return "Expired";
        return "Active";
    }

    private static (bool Eligible, string ReminderType) ResolveReminder(DateTime nowUtc, DateTime? validToUtc)
    {
        if (validToUtc is null) return (false, "None");

        var end = validToUtc.Value;

        // Expiring soon: within next 30 days (including today)
        if (end >= nowUtc && end <= nowUtc.AddDays(30))
        {
            return (true, "ExpiringSoon");
        }

        // Expired: within last 30 days
        if (end < nowUtc && end >= nowUtc.AddDays(-30))
        {
            return (true, "Expired");
        }

        return (false, "None");
    }
}
