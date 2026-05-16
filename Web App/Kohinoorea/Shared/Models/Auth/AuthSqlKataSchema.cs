namespace Kohinoorea.Shared.Models.Auth;

public static class AuthSqlKataSchema
{
    public const string UsersTable = "users";
    public const string SignupSubmissionsTable = "signup_submissions";
    public const string ProductsTable = "products";
    public const string OrdersTable = "orders";
    public const string CartItemsTable = "cart_items";
    public const string SupportQueriesTable = "support_queries";
    public const string SupportQueryMessagesTable = "support_query_messages";

    public static class UserColumns
    {
        public const string Id = "id";
        public const string FullName = "full_name";
        public const string Email = "email";
        public const string Phone = "phone";
        public const string Mt4Broker = "mt4_broker";
        public const string PasswordHash = "password_hash";
        public const string Role = "role";
        public const string IsActive = "is_active";
        public const string CreatedAtUtc = "created_at_utc";
        public const string LastLoginAtUtc = "last_login_at_utc";
    }

    public static class SignupColumns
    {
        public const string Id = "id";
        public const string FullName = "full_name";
        public const string Email = "email";
        public const string Phone = "phone";
        public const string Mt4Broker = "mt4_broker";
        public const string AccessPlan = "access_plan";
        public const string Notes = "notes";
        public const string CreatedAtUtc = "created_at_utc";
    }

    public static class ProductColumns
    {
        public const string Id = "id";
        public const string Name = "name";
        public const string Description = "description";
        public const string ImageLink = "image_link";
        public const string Price = "price";
        public const string IsActive = "is_active";
        public const string IsMaster = "is_master";
        public const string CountryCode = "country_code";
        public const string CreatedAtUtc = "created_at_utc";
    }

    public static class OrderColumns
    {
        public const string Id = "id";
        public const string ProductId = "product_id";
        public const string UserId = "user_id";
        public const string Quantity = "quantity";
        public const string UnitPrice = "unit_price";
        public const string TotalAmount = "total_amount";
        public const string PaymentMethod = "payment_method";
        public const string Status = "status";
        public const string OrderedAtUtc = "ordered_at_utc";
    }

    public static class CartItemColumns
    {
        public const string Id = "id";
        public const string UserId = "user_id";
        public const string ProductId = "product_id";
        public const string Quantity = "quantity";
        public const string UnitPrice = "unit_price";
        public const string TotalAmount = "total_amount";
        public const string AddedAtUtc = "added_at_utc";
    }

    public static class SupportQueryColumns
    {
        public const string Id = "id";
        public const string UserId = "user_id";
        public const string Subject = "subject";
        public const string Category = "category";
        public const string Message = "message";
        public const string Status = "status";
        public const string CreatedAtUtc = "created_at_utc";
    }

    public static class SupportQueryMessageColumns
    {
        public const string Id = "id";
        public const string QueryId = "query_id";
        public const string SenderRole = "sender_role";
        public const string SenderUserId = "sender_user_id";
        public const string Message = "message";
        public const string CreatedAtUtc = "created_at_utc";
    }
}
