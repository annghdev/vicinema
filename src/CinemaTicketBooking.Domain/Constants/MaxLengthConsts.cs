namespace CinemaTicketBooking.Domain;

public static class MaxLengthConsts
{
    public const int Name = 256;
    public const int ActorName = 256;
    public const int ScreenCode = 64;
    public const int SeatCode = 64;
    public const int TicketCode = 128;
    public const int SessionId = 128;
    public const int PhoneNumber = 32;
    public const int Email = 256;
    public const int Address = 512;
    public const int Geo = 128;
    public const int Url = 2048;
    public const int Description = 4000;
    public const int TicketDescription = 512;
    public const int QrCode = 2048;
    public const int ClaimType = 256;
    public const int ClaimValue = 2000;
    public const int IdentityProvider = 128;
    public const int IdentityProviderKey = 256;
    public const int TokenName = 256;
    public const int TokenValue = 2000;
    public const int PasswordHash = 512;
    public const int SecurityStamp = 128;
    public const int ConcurrencyStamp = 128;
    public const int GatewayTransactionId = 256;
    public const int PaymentUrl = 2048;
    public const int GatewayResponseRaw = 8000;

    // === Coupon ===
    public const int CouponCode = 64;
    public const int CouponDescription = 512;
}
