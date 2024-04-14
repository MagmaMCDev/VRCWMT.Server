using Microsoft.AspNetCore.Builder;

namespace ServerBackend;
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add controllers services
        services.AddControllers();
        // Other services configuration...
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseDeveloperExceptionPage();
        if (env.IsDevelopment())
        {
        }
        else
        {
            // Configure error handling for other environments
        }

        app.UseRouting();

        //app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers(); // This maps controllers to endpoints
        });
    }
}
