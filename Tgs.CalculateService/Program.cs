using FluentdForward.OpenTelemetry.MessagePack;
using NATS.Client.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Calculate.MessageSerializers;

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
		.AddRouting(routeOptions => routeOptions.LowercaseUrls = true)
		.AddControllers()
		.Services
		.AddProblemDetails()
		.AddKeyedSingleton(
			"Nats",
			(sp, _) => new NatsConnection(NatsOpts.Default with
			{
				Url = builder.Configuration.GetConnectionString("Nats")!,
				SerializerRegistry = ProtobufSerializerRegistration.Default
			}))
		.AddMessageQueue()
		.AddNatsMessageQueue(natsConfig => natsConfig
			.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats")))
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

	_ = builder.Services
		.AddSwaggerGen(c =>
		{
			var xmlFileNames = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml");
			foreach (var xmlFileName in xmlFileNames)
				c.IncludeXmlComments(xmlFileName);
		});

}

var app = builder.Build();
{
	app.MapControllers();
	app.UseSwaggerUI();
	app.MapSwagger();
	app.MapHealthChecks("/healthz");
}

app.Run();
