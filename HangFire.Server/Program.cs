using BookingApp_Backend.Helpers.Jobs;
using Hangfire;
using HangFire.Server.Jobs;
using HangFire.Server.Options;


var host = Host.CreateDefaultBuilder(args);
var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();


host.ConfigureServices(services =>
{
    services.AddHangfire(opt =>
    {
        opt.UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"))
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();
    });

    services.AddHangfireServer();
    services.AddScoped<IDeleteDataJob, DeleteDataJob>();
    services.AddScoped<IDeleteOldData, DeleteOldData>();
    services.Configure<ServerOptions>(configuration.GetSection(ServerOptions.ServerOptionsKey));

});

host.Build().Run();

