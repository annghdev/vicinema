using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Repositories;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Infrastructure.Persistence.Repositories;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Infrastructure.Cache;
using CinemaTicketBooking.Infrastructure.FileStorages;
using CinemaTicketBooking.Infrastructure.Notifications;
using CinemaTicketBooking.Infrastructure.Notifications.Brevo;
using CinemaTicketBooking.Infrastructure.Payments.Momo;
using CinemaTicketBooking.Infrastructure.Payments;
using CinemaTicketBooking.Infrastructure.Payments.Vnpay;
using CinemaTicketBooking.Infrastructure.Persistence;
using CinemaTicketBooking.Infrastructure.QrCodes;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("redis");
        services.Configure<TicketLockingOptions>(configuration.GetSection(TicketLockingOptions.SectionName));

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "CinemaTicketBooking:";
            });
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
                redisOptions.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(redisOptions);
            });
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<ITicketLocker, RedisTicketLocker>();
        }
        else
        {
            services.AddScoped<ITicketLocker, NoRedisTicketLocker>();
            services.AddScoped<ICacheService, NoOpCacheService>();
        }

        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ICinemaRepository, CinemaRepository>();
        services.AddScoped<IConcessionRepository, ConcessionRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IScreenRepository, ScreenRepository>();
        services.AddScoped<IPricingPolicyRepository, PricingPolicyRepository>();
        services.AddScoped<ISeatSelectionPolicyRepository, SeatSelectionPolicyRepository>();
        services.AddScoped<IShowTimeRepository, ShowTimeRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<ISlideRepository, SlideRepository>();
        services.AddScoped<ILoyaltyTierConfigurationRepository, LoyaltyTierConfigurationRepository>();
        services.AddScoped<ICouponTemplateRepository, CouponTemplateRepository>();
        services.AddScoped<ICustomerCouponRepository, CustomerCouponRepository>();
        services.AddScoped<ILoyaltyDiscountService, LoyaltyDiscountService>();
        services.AddScoped<IDiscountStrategy, LoyaltyTierDiscountStrategy>();
        services.AddScoped<IQueryService, QueryService>();
        services.AddScoped<IUnitOfWork, EFUnitOfWork>();
        services.AddScoped<DataSeeder>();
        services.AddHttpClient();

        // Email services
        services.Configure<BrevoOptions>(configuration.GetSection(BrevoOptions.SectionName));
        var brevoApiKey = configuration[$"{BrevoOptions.SectionName}:ApiKey"];
        if (!string.IsNullOrWhiteSpace(brevoApiKey))
        {
            services.AddHttpClient(nameof(BrevoEmailSender));
            services.AddScoped<IEmailSender, BrevoEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, LogEmailSender>();
        }

        // Payment services
        services.Configure<MomoOptions>(configuration.GetSection(MomoOptions.SectionName));
        services.Configure<VnpayOptions>(configuration.GetSection(VnpayOptions.SectionName));
        services.AddScoped<IPaymentService, NoPaymentGatewayService>();
        services.AddScoped<IPaymentService, MomoPaymentService>();
        services.AddScoped<IPaymentService, VnpayPaymentService>();
        services.AddScoped<IPaymentServiceFactory, PaymentServiceFactory>();
        services.AddSingleton<IPaymentRealtimePublisher, NoOpPaymentRealtimePublisher>();

        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

        // File storage (MinIO)
        // Priority: ConnectionStrings:minio (Aspire) > Minio section (manual config)
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        var minioOpts = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>() ?? new MinioOptions();
        var minioConnectionString = configuration.GetConnectionString("minio");

        if (!string.IsNullOrWhiteSpace(minioConnectionString))
        {
            // Parse Aspire format: "Endpoint=http://host:port;AccessKey=xxx;SecretKey=xxx"
            var parts = minioConnectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (parts.TryGetValue("Endpoint", out var endpoint))
            {
                // Strip scheme for MinIO SDK (expects "host:port" without http://)
                var uri = new Uri(endpoint);
                minioOpts.Endpoint = uri.Host + ":" + uri.Port;
                minioOpts.UseSsl = uri.Scheme == "https";
            }
            if (parts.TryGetValue("AccessKey", out var accessKey)) minioOpts.AccessKey = accessKey;
            if (parts.TryGetValue("SecretKey", out var secretKey)) minioOpts.SecretKey = secretKey;
        }

        // Re-publish resolved options so IOptions<MinioOptions> stays in sync
        services.PostConfigure<MinioOptions>(opts =>
        {
            opts.Endpoint = minioOpts.Endpoint;
            opts.AccessKey = minioOpts.AccessKey;
            opts.SecretKey = minioOpts.SecretKey;
            opts.UseSsl = minioOpts.UseSsl;
        });

        services.AddSingleton<IMinioClient>(_ =>
        {
            var client = new MinioClient()
                .WithEndpoint(minioOpts.Endpoint)
                .WithCredentials(minioOpts.AccessKey, minioOpts.SecretKey);

            if (minioOpts.UseSsl)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });
        services.AddScoped<IFileStorageService, MinioFileStorageService>();

        return services;
    }
}
