using System.Security.Claims;
using Kohinoorea.Server.Services;
using Kohinoorea.Shared.Models.Auth;
using Kohinoorea.Shared.Models.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kohinoorea.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CommerceController : ControllerBase
{
    private readonly ICommerceRepository _commerceRepository;
    private readonly IAuthRepository _authRepository;
    private readonly IEmailDeliveryService _emailDeliveryService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CommerceController(ICommerceRepository commerceRepository, IAuthRepository authRepository, IEmailDeliveryService emailDeliveryService, IWebHostEnvironment webHostEnvironment)
    {
        _commerceRepository = commerceRepository;
        _authRepository = authRepository;
        _emailDeliveryService = emailDeliveryService;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts([FromQuery] bool all = false, CancellationToken cancellationToken = default)
    {
        var products = await _commerceRepository.GetProductsAsync(onlyActive: !all, cancellationToken: cancellationToken);
        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("pricing-plans")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetPricingPlans([FromQuery] string? country = null, CancellationToken cancellationToken = default)
    {
        var products = await _commerceRepository.GetProductsAsync(onlyActive: true, countryCode: country, cancellationToken: cancellationToken);
        var plans = products.Where(p => p.IsMaster).ToList();
        return Ok(plans);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPost("products")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var productId = await _commerceRepository.CreateProductAsync(request, cancellationToken);
        var created = await _commerceRepository.GetProductByIdAsync(productId, cancellationToken);

        if (created is not null && created.IsActive)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var recipients = await _authRepository.GetActiveUserEmailsAsync(cancellationToken);
                    if (recipients.Count == 0)
                    {
                        return;
                    }

                    var subject = $"New product: {created.Name}";
                    var body = ProductNotificationHtmlRenderer.Render(created);

                    foreach (var email in recipients)
                    {
                        await _emailDeliveryService.SendHtmlEmailAsync(email, subject, body, cancellationToken);
                    }
                }
                catch
                {
                }
            }, CancellationToken.None);
        }

        return created is null ? NotFound() : Ok(created);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPost("products/{productId:long}/notify")]
    public async Task<ActionResult> NotifyProduct([FromRoute] long productId, CancellationToken cancellationToken)
    {
        var product = await _commerceRepository.GetProductByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var recipients = await _authRepository.GetActiveUserEmailsAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            return Ok(new { success = true, message = "No active users to notify." });
        }

        var subject = $"New product: {product.Name}";
        var body = ProductNotificationHtmlRenderer.Render(product);

        var sent = 0;
        foreach (var email in recipients)
        {
            var (success, _) = await _emailDeliveryService.SendHtmlEmailAsync(email, subject, body, cancellationToken);
            if (success)
            {
                sent++;
            }
        }

        return Ok(new { success = true, message = $"Notification sent to {sent} user(s)." });
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPut("products/{productId:long}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct([FromRoute] long productId, [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _commerceRepository.UpdateProductAsync(productId, request, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        var product = await _commerceRepository.GetProductByIdAsync(productId, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPatch("products/{productId:long}/soft-delete")]
    public async Task<ActionResult<ProductDto>> SoftDeleteProduct([FromRoute] long productId, CancellationToken cancellationToken)
    {
        var deleted = await _commerceRepository.SoftDeleteProductAsync(productId, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        var product = await _commerceRepository.GetProductByIdAsync(productId, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPut("products/{productId:long}/active/{isActive:bool}")]
    public async Task<ActionResult<ProductDto>> SetProductActive([FromRoute] long productId, [FromRoute] bool isActive, CancellationToken cancellationToken)
    {
        var updated = await _commerceRepository.SetProductActiveAsync(productId, isActive, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        var product = await _commerceRepository.GetProductByIdAsync(productId, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpDelete("products/{productId:long}/force")]
    public async Task<ActionResult> ForceDeleteProduct([FromRoute] long productId, CancellationToken cancellationToken)
    {
        var deleted = await _commerceRepository.ForceDeleteProductAsync(productId, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPost("products/upload-image")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<object>> UploadProductImage([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Please provide an image file.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Only jpg, jpeg, png, webp, and gif files are allowed.");
        }

        var rootPath = string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath)
            ? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot")
            : _webHostEnvironment.WebRootPath;
        var uploadDirectory = Path.Combine(rootPath, "uploads", "products");
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var imageUrl = $"/uploads/products/{fileName}";
        return Ok(new { imageUrl });
    }

    [HttpPost("orders")]
    public async Task<ActionResult<OrderResultDto>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var product = await _commerceRepository.GetProductByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return NotFound("Product not found.");
        }

        var orderId = await _commerceRepository.CreateOrderAsync(userId.Value, product, request.Quantity, paymentMethod: "Card", cancellationToken: cancellationToken);

        return Ok(new OrderResultDto
        {
            OrderId = orderId,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = request.Quantity,
            UnitPrice = product.Price,
            TotalAmount = product.Price * request.Quantity,
            OrderedAtUtc = DateTime.UtcNow
        });
    }

    [HttpPost("cart-items")]
    public async Task<ActionResult<CartItemResultDto>> AddCartItem([FromBody] CreateCartItemRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var product = await _commerceRepository.GetProductByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return NotFound("Product not found.");
        }

        var cartItemId = await _commerceRepository.AddCartItemAsync(userId.Value, product, request.Quantity, cancellationToken);

        return Ok(new CartItemResultDto
        {
            CartItemId = cartItemId,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = request.Quantity,
            UnitPrice = product.Price,
            TotalAmount = product.Price * request.Quantity,
            AddedAtUtc = DateTime.UtcNow
        });
    }

    [HttpGet("orders/my")]
    public async Task<ActionResult<IReadOnlyList<UserOrderDto>>> GetMyOrders(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var orders = await _commerceRepository.GetUserOrdersAsync(userId.Value, cancellationToken);
        return Ok(orders);
    }

    [AllowAnonymous]
    [HttpGet("faqs")]
    public async Task<ActionResult<IReadOnlyList<FaqDto>>> GetFaqs(CancellationToken cancellationToken)
    {
        var faqs = await _commerceRepository.GetFaqsAsync(onlyActive: true, cancellationToken);
        return Ok(faqs);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpGet("faqs/admin")]
    public async Task<ActionResult<IReadOnlyList<FaqDto>>> GetAdminFaqs(CancellationToken cancellationToken)
    {
        var faqs = await _commerceRepository.GetFaqsAsync(onlyActive: false, cancellationToken);
        return Ok(faqs);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPost("faqs")]
    public async Task<ActionResult<FaqDto>> CreateFaq([FromBody] CreateFaqRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var faqId = await _commerceRepository.CreateFaqAsync(request, cancellationToken);
        var created = await _commerceRepository.GetFaqByIdAsync(faqId, cancellationToken);
        return created is null ? NotFound() : Ok(created);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPut("faqs/{faqId:long}")]
    public async Task<ActionResult<FaqDto>> UpdateFaq([FromRoute] long faqId, [FromBody] CreateFaqRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _commerceRepository.UpdateFaqAsync(faqId, request, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        var faq = await _commerceRepository.GetFaqByIdAsync(faqId, cancellationToken);
        return faq is null ? NotFound() : Ok(faq);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPut("faqs/{faqId:long}/active/{isActive:bool}")]
    public async Task<ActionResult<FaqDto>> SetFaqActive([FromRoute] long faqId, [FromRoute] bool isActive, CancellationToken cancellationToken)
    {
        var updated = await _commerceRepository.SetFaqActiveAsync(faqId, isActive, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        var faq = await _commerceRepository.GetFaqByIdAsync(faqId, cancellationToken);
        return faq is null ? NotFound() : Ok(faq);
    }

    [AllowAnonymous]
    [HttpPost("contact-messages")]
    public async Task<ActionResult<AuthResponse>> CreateContactMessage([FromBody] CreateContactMessageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _commerceRepository.CreateContactMessageAsync(request, cancellationToken);

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Thanks for reaching out. Your message is now in our support inbox."
        });
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpGet("contact-messages")]
    public async Task<ActionResult<IReadOnlyList<ContactMessageDto>>> GetContactMessages(CancellationToken cancellationToken)
    {
        var messages = await _commerceRepository.GetContactMessagesAsync(cancellationToken);
        return Ok(messages);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPost("contact-messages/{contactMessageId:long}/reply")]
    public async Task<ActionResult<AuthResponse>> ReplyToContactMessage([FromRoute] long contactMessageId, [FromBody] SendContactReplyRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var contactMessage = await _commerceRepository.GetContactMessageByIdAsync(contactMessageId, cancellationToken);
        if (contactMessage is null)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "That contact message could not be found."
            });
        }

        var result = await _emailDeliveryService.SendPlainTextEmailAsync(contactMessage.Email, request.Subject, request.Message, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = result.Message
            });
        }

        await _commerceRepository.MarkContactMessageRepliedAsync(contactMessageId, DateTime.UtcNow, cancellationToken);

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Reply email sent successfully."
        });
    }

    [HttpGet("support/my")]
    public async Task<ActionResult<IReadOnlyList<SupportQueryDto>>> GetMySupportQueries(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var queries = await _commerceRepository.GetSupportQueriesAsync(userId.Value, cancellationToken);
        return Ok(queries);
    }

    [HttpPost("support/my")]
    public async Task<ActionResult<SupportQueryDto>> CreateMySupportQuery([FromBody] CreateSupportQueryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var queryId = await _commerceRepository.CreateSupportQueryAsync(userId.Value, request, cancellationToken);
        var queries = await _commerceRepository.GetSupportQueriesAsync(userId.Value, cancellationToken);
        var created = queries.FirstOrDefault(x => x.Id == queryId);
        return created is null ? NotFound() : Ok(created);
    }

    [HttpGet("support/my/{queryId:long}/messages")]
    public async Task<ActionResult<IReadOnlyList<SupportQueryMessageDto>>> GetMySupportQueryMessages([FromRoute] long queryId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var query = await _commerceRepository.GetSupportQueryByIdAsync(queryId, cancellationToken);
        if (query is null)
        {
            return NotFound();
        }

        if (query.UserId != userId.Value)
        {
            return Forbid();
        }

        var messages = await _commerceRepository.GetSupportQueryMessagesAsync(queryId, cancellationToken);
        return Ok(messages);
    }

    [HttpPost("support/my/{queryId:long}/messages")]
    public async Task<ActionResult<SupportQueryMessageDto>> CreateMySupportQueryMessage([FromRoute] long queryId, [FromBody] CreateSupportQueryMessageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var query = await _commerceRepository.GetSupportQueryByIdAsync(queryId, cancellationToken);
        if (query is null)
        {
            return NotFound();
        }

        if (query.UserId != userId.Value)
        {
            return Forbid();
        }

        if (string.Equals(query.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            await _commerceRepository.UpdateSupportQueryStatusAsync(queryId, "Open", cancellationToken);
        }

        var messageId = await _commerceRepository.CreateSupportQueryMessageAsync(queryId, AuthRoles.User, userId, request.Message, cancellationToken);
        var messages = await _commerceRepository.GetSupportQueryMessagesAsync(queryId, cancellationToken);
        var created = messages.FirstOrDefault(m => m.Id == messageId);
        return created is null ? NotFound() : Ok(created);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpGet("support/trace")]
    public async Task<ActionResult<IReadOnlyList<SupportQueryDto>>> GetSupportTrace(CancellationToken cancellationToken)
    {
        var trace = await _commerceRepository.GetAllSupportQueriesAsync(cancellationToken);
        return Ok(trace);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpGet("support/{queryId:long}/messages")]
    public async Task<ActionResult<IReadOnlyList<SupportQueryMessageDto>>> GetSupportMessages([FromRoute] long queryId, CancellationToken cancellationToken)
    {
        var query = await _commerceRepository.GetSupportQueryByIdAsync(queryId, cancellationToken);
        if (query is null)
        {
            return NotFound();
        }

        var messages = await _commerceRepository.GetSupportQueryMessagesAsync(queryId, cancellationToken);
        return Ok(messages);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPost("support/{queryId:long}/messages")]
    public async Task<ActionResult<SupportQueryMessageDto>> CreateAdminSupportQueryMessage([FromRoute] long queryId, [FromBody] CreateSupportQueryMessageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var query = await _commerceRepository.GetSupportQueryByIdAsync(queryId, cancellationToken);
        if (query is null)
        {
            return NotFound();
        }

        var adminUserId = ResolveUserId();
        var messageId = await _commerceRepository.CreateSupportQueryMessageAsync(queryId, AuthRoles.Admin, adminUserId, request.Message, cancellationToken);
        var messages = await _commerceRepository.GetSupportQueryMessagesAsync(queryId, cancellationToken);
        var created = messages.FirstOrDefault(m => m.Id == messageId);
        return created is null ? NotFound() : Ok(created);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPatch("support/{queryId:long}/status")]
    public async Task<ActionResult> UpdateSupportQueryStatus([FromRoute] long queryId, [FromBody] UpdateSupportQueryStatusRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _commerceRepository.UpdateSupportQueryStatusAsync(queryId, request.Status.Trim(), cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpGet("orders/trace")]
    public async Task<ActionResult<IReadOnlyList<OrderTraceDto>>> GetOrderTrace(CancellationToken cancellationToken)
    {
        var trace = await _commerceRepository.GetOrderTraceAsync(cancellationToken);
        return Ok(trace);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPatch("orders/{orderId:long}/status")]
    public async Task<ActionResult> UpdateOrderStatus([FromRoute] long orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var newStatus = request.Status.Trim();
        var existing = await _commerceRepository.GetOrderTraceByIdAsync(orderId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var updated = await _commerceRepository.UpdateOrderStatusAsync(orderId, newStatus, cancellationToken);
        if (updated &&
            string.Equals(newStatus, "Completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(existing.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(existing.UserEmail))
        {
            var finalized = existing;
            finalized.Status = newStatus;
            var html = InvoiceHtmlRenderer.RenderOrderInvoiceHtml(finalized, currencyCountryCode: "US");
            var subject = $"Your Kohinoorea Invoice (ORD-{finalized.OrderId})";
            await _emailDeliveryService.SendHtmlEmailAsync(finalized.UserEmail, subject, html, cancellationToken);
        }

        return updated ? NoContent() : NotFound();
    }

    private long? ResolveUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(claimValue, out var userId) ? userId : null;
    }
}
