using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RecursionExplorer.Desktop;

public class Startup
{
    // (Optional) Configuration can be added here (e.g. through JSON files, environment variables, etc.)
    public IConfiguration ConfigureConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();

        var config = configBuilder.Build();
        return config;
    }
    
    // Add services here
    public IServiceCollection ConfigureServices(IConfiguration config)
    {
        var services = new ServiceCollection();
        
        services.AddTransient<IWindowActivator, WindowActivator>();
        
        return services;
    }
    
    public IServiceProvider BuildServiceProvider(IServiceCollection services)
    {
        return services.BuildServiceProvider();
    }
}