using Microsoft.OpenApi.Models;
using Soccer.Application.Models;

namespace Soccer.WebApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        var mainAssemblyName = typeof(Program).Assembly.GetName().Name;
        var applicationAssemblyName = typeof(NewGame).Assembly.GetName().Name;

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Soccer",
                Version = "v1",
                Description = "Open Webinars Arquitectura Limpia con .NET",
                Contact = 
                    new OpenApiContact
                    {
                        Name = "Diego Martin",
                        Email = "diego.martin@sunnyatticsoftware.com"
                    }
            });

            var xmlCommentsWebApi = Path.Combine(AppContext.BaseDirectory, $"{mainAssemblyName}.xml");
            c.IncludeXmlComments(xmlCommentsWebApi);
            var xmlCommentsApplication = Path.Combine(AppContext.BaseDirectory, $"{applicationAssemblyName}.xml");
            c.IncludeXmlComments(xmlCommentsApplication);
        });

        return services;
    }

    public static void UseOpenApi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Soccer v1"));
    }
}
