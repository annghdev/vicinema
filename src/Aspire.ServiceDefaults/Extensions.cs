using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.ClearProviders();
        builder.ConfigureOpenTelemetry();
        builder.ConfigureStructuredLogging();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    private static TBuilder ConfigureStructuredLogging<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var lokiEndpoint = builder.Configuration["LOKI_ENDPOINT"];

        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.With<OpenTelemetryContextEnricher>()
                .Enrich.WithProperty("service_name", builder.Environment.ApplicationName)
                .Enrich.WithProperty("deployment_environment", builder.Environment.EnvironmentName)
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);

            if (builder.Environment.IsDevelopment())
            {
                loggerConfiguration.MinimumLevel.Debug();
            }

            if (!string.IsNullOrWhiteSpace(lokiEndpoint))
            {
                loggerConfiguration.WriteTo.GrafanaLoki(
                    lokiEndpoint,
                    labels:
                    [
                        new LokiLabel { Key = "app", Value = "cinema-ticket-booking" }
                    ],
                    propertiesAsLabels:
                    [
                        "service_name",
                        "deployment_environment"
                    ]);
            }
        }, writeToProviders: true);

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
                    new KeyValuePair<string, object>("service.namespace", "cinema-ticket-booking")
                ]))
            .WithLogging(logging =>
            {
                var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var otlpEndpointUri))
                {
                    var otlpProtocolValue = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
                    var otlpProtocol = string.Equals(otlpProtocolValue, "grpc", StringComparison.OrdinalIgnoreCase)
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;

                    logging.AddOtlpExporter(options =>
                    {
                        options.Endpoint = otlpEndpointUri;
                        options.Protocol = otlpProtocol;
                    });
                }

                var tempoEndpoint = builder.Configuration["TEMPO_OTLP_ENDPOINT"];
                if (Uri.TryCreate(tempoEndpoint, UriKind.Absolute, out var tempoEndpointUri))
                {
                    logging.AddOtlpExporter(options =>
                    {
                        options.Endpoint = tempoEndpointUri;
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddProcessInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Wolverine")
                    .AddPrometheusExporter();

                var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var otlpEndpointUri))
                {
                    var otlpProtocolValue = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
                    var otlpProtocol = string.Equals(otlpProtocolValue, "grpc", StringComparison.OrdinalIgnoreCase)
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;

                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = otlpEndpointUri;
                        options.Protocol = otlpProtocol;
                    });
                }
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();

                var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var otlpEndpointUri))
                {
                    var otlpProtocolValue = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
                    var otlpProtocol = string.Equals(otlpProtocolValue, "grpc", StringComparison.OrdinalIgnoreCase)
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;

                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = otlpEndpointUri;
                        options.Protocol = otlpProtocol;
                    });
                }

                var tempoEndpoint = builder.Configuration["TEMPO_OTLP_ENDPOINT"];
                if (Uri.TryCreate(tempoEndpoint, UriKind.Absolute, out var tempoEndpointUri))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = tempoEndpointUri;
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            });

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Prometheus metrics endpoint — Prometheus scrape at /metrics
        app.MapPrometheusScrapingEndpoint();

        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}

/// <summary>
/// Enriches Serilog log events with active OpenTelemetry TraceId and SpanId properties.
/// </summary>
public class OpenTelemetryContextEnricher : ILogEventEnricher
{
    /// <summary>
    /// Enriches the log event with active Activity TraceId and SpanId.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The factory to create properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToHexString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToHexString()));
        }
    }
}
