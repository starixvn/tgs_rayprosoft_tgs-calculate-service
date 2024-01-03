using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.EntityFrameworkCore;
using NATS.Client.Core;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using Tgs.Calculate;
using Tgs.CalculateWorker.MessageQueue;

var builder = WebApplication.CreateBuilder(args);
{
	_ = builder.Logging
		.AddOpenTelemetry(options =>
		{
			options.IncludeFormattedMessage = true;
			options.ParseStateValues = true;
			_ = options.AddFluentdForwardExporter(fluentOptions =>
			{
				var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
				fluentOptions.Host = nodeIp;
				fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
				fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
					"MachineInfo",
					() => new
					{
						Environment.MachineName
					}));
			});
		});

	_ = builder.Services
		.AddSingleton(TimeProvider.System)
		.AddKeyedSingleton<IConnectionMultiplexer>(
			"Redis",
			(sp, _) => ConnectionMultiplexer.Connect(
				builder.Configuration.GetConnectionString("Redis")!))
		.AddKeyedSingleton(
			"Redis",
			(sp, _) => sp.GetRequiredKeyedService<IConnectionMultiplexer>(
				"Redis").GetDatabase())
		.AddCalculateWorkerCore()
		.AddCalculateWorkerRepositories(
			dbBuilder => dbBuilder
				.UseNpgsql("name=Tgs"),
			sp => sp.GetRequiredKeyedService<IDatabase>("Redis"));

	_ = builder.Services
		.AddKeyedSingleton(
			"Nats",
			(sp, _) => new NatsConnection(NatsOpts.Default with
			{
				Url = builder.Configuration.GetConnectionString("Nats")!,
				SerializerRegistry = ProtobufSerializerRegistration.Default
			}))
		.AddMessageQueue()
		.AddNatsMessageQueue(natsConfig => natsConfig
			.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
			.AddProcessor(ProcessorTypes.BidProcessorType, Subjects.WorkerBidSubject)
			.AddProcessor<ExchangeRequestDispatchProcessor>("tgs.calculate.*", "tgs.calculate.worker")
			.AddProcessor(ProcessorTypes.CreateCalculateProcessorType, $"{Subjects.CreateSubject}.{Environment.MachineName}")
			.AddProcessor(ProcessorTypes.DeleteCalculateProcessorType, $"{Subjects.DeleteSubject}.{Environment.MachineName}")
			.AddProcessor(ProcessorTypes.ExchangeCalculateProcessorType, $"{Subjects.ExchangeSubject}.{Environment.MachineName}")
			.AddProcessor(ProcessorTypes.GetBalanceProcessorType, $"{Subjects.GetBalanceSubject}.{Environment.MachineName}"))
		.AddNatsGlobPatternExchange("*");

	var instrumentationExtenionFilters = new[] { "html", "ico", "png", "jpg" };
	_ = builder.Services
		.AddOpenTelemetry()
		.WithTracing(
			tracingBuilder => tracingBuilder
				.SetResourceBuilder(ResourceBuilder.CreateDefault()
					.AddEnvironmentVariableDetector())
				.AddSource("Tgs.*")
				.AddSource("TGS.*")
				.AddSource("Valhalla.*")
				.AddAspNetCoreInstrumentation(options => options.Filter =
					httpContext => !httpContext.Request.Path.HasValue
						|| !httpContext.Request.Path.StartsWithSegments("/swagger")
						&& !httpContext.Request.Path.StartsWithSegments("/healthz")
						&& !httpContext.Request.Path.StartsWithSegments("/metrics")
						&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
						&& !instrumentationExtenionFilters.Any(
							ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
				.AddProfileViewExporter()
				.AddOtlpExporter())
		.WithMetrics(meterBuilder => meterBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddAspNetCoreInstrumentation()
			.AddRuntimeInstrumentation()
			.AddOtlpExporter())
		.Services
		.AddHealthChecks()
		.Services
		.AddProfileViewer();
}

var app = builder.Build();
{
	app.MapHealthChecks("/healthz");
}

app.Run();
