using HowellSync.Commands;
using HowellSync.Execution;
using HowellSync.Providers;
using HowellSync.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

CatalogCommand command;
try { command = CatalogCommand.Parse(args); }
catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
if (command.Name == "help") { Console.WriteLine(CatalogCommand.Help); return 0; }
try
{
    // Parse operational arguments ourselves; standard host configuration still loads JSON and environment variables.
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [], ContentRootPath = AppContext.BaseDirectory });
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
    builder.Services.AddScoped<ProviderRegistry>();
    builder.Services.AddScoped<CatalogOperationRunner>();
    builder.Services.AddScoped<CatalogRunRepository>();
    builder.Services.AddSingleton<CatalogSessionFactory>();
    builder.Services.AddHttpClient("gbif", client => { client.Timeout = Timeout.InfiniteTimeSpan; client.MaxResponseContentBufferSize = 4 * 1024 * 1024; })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
    builder.Services.AddScoped<ICatalogProvider, HowellSync.Providers.Gbif.GbifCatalogProvider>();
    using var host = builder.Build();
    await host.StartAsync();
    var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    await using var scope = host.Services.CreateAsyncScope();
    var result = await scope.ServiceProvider.GetRequiredService<CatalogOperationRunner>().RunAsync(command, lifetime.ApplicationStopping);
    using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await host.StopAsync(shutdown.Token);
    return result;
}
catch (OperationCanceledException) { return 130; }
catch (Exception error)
{
    Console.Error.WriteLine($"Howell Sync startup/shutdown failed ({error.GetType().Name}); check configuration.");
    return 2;
}
