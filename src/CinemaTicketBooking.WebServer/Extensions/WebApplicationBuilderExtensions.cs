using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Common.PipelineMiddlewares;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Infrastructure.Persistence;
using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Configuration;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;

namespace CinemaTicketBooking.WebServer;

public static class WebApplicationBuilderExtensions
{
    public static void AddWolverine(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<MessagePipelineLoggingOptions>(
            builder.Configuration.GetSection(MessagePipelineLoggingOptions.SectionName));

        builder.Host.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(IRequest).Assembly);

            var dbConectionString = builder.Configuration.GetConnectionString("cinemadb")!;
            opts.PersistMessagesWithPostgresql(dbConectionString);

            opts.Services.AddDbContextWithWolverineIntegration<AppDbContext>(
                x => x.UseNpgsql(dbConectionString));

            opts.UseFluentValidation();

            opts.UseEntityFrameworkCoreTransactions();
            opts.Policies.Add(new CommandTransactionPolicy());
            opts.Policies.UseDurableLocalQueues();

            // Message pipeline: logging, optional query cache, exception logging (non-HTTP paths).
            opts.Policies.AddMiddleware(typeof(CorrelationIdWolverineMiddleware));
            opts.Policies.AddMiddleware(typeof(LoggingMiddleware));
            opts.Policies.ForMessagesOfType<ICachableQuery>().AddMiddleware(typeof(CachingMiddleware));

            opts.Policies
                .OnException<DbUpdateConcurrencyException>()
                .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
                .Then.MoveToErrorQueue();

            opts.Policies
            .OnException<ConcurrencyException>()
            .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
            .Then.MoveToErrorQueue();

            // Last: log any remaining handler failures (Publish, InvokeAsync, cron, scheduled) that are not matched above.
            // Uses MoveToErrorQueue as the primary action so .And runs as a side effect (same as default terminal outcome for poison messages).
            opts.Policies.OnException<Exception>()
                .MoveToErrorQueue()
                .And((runtime, _, ex) =>
                {
                    var lf = runtime.Services.GetRequiredService<ILoggerFactory>();
                    lf.CreateLogger("Wolverine.MessageFailures").LogError(
                        ex,
                        "Unhandled exception in Wolverine message handling");
                    return ValueTask.CompletedTask;
                });

            opts.AutoBuildMessageStorageOnStartup = AutoCreate.CreateOrUpdate;

            //opts.PublishAllMessages().Locally().MaximumParallelMessages(4).UseDurableInbox();

            opts.Publish(rule =>
            {
                rule.MessagesImplementing<IDomainEvent>();

                rule.ToLocalQueue("domain_events")
                    .MaximumParallelMessages(4)
                    .UseDurableInbox();
            });
        });
    }

    /// <summary>
    /// Applies Wolverine transactional middleware only to command handlers.
    /// </summary>
    private sealed class CommandTransactionPolicy : IChainPolicy
    {
        /// <summary>
        /// Configures command handler chains as transactional while leaving query handlers read-only.
        /// </summary>
        public void Apply(IReadOnlyList<IChain> chains, GenerationRules rules, IServiceContainer container)
        {
            var transactional = new TransactionalAttribute();

            foreach (var chain in chains)
            {
                var messageType = chain.InputType();
                if (messageType is null || !typeof(ICommand).IsAssignableFrom(messageType))
                {
                    continue;
                }

                if (chain.IsTransactional || chain.HasAttribute<NonTransactionalAttribute>())
                {
                    continue;
                }

                transactional.Modify(chain, rules, container);
            }
        }
    }
}
