using Soccer.Application;
using Soccer.Application.Factories;
using Soccer.Application.Mappers;
using Soccer.Notification.Abstractions;
using Soccer.Notification.Email;
using Soccer.Notification.Email.Models;
using Soccer.Persistence.Abstractions;
using Soccer.Persistence.InMemory;
using Soccer.WebApi;

var builder = WebApplication.CreateBuilder(args);

// IoCC
var services = builder.Services;
services.AddTransient<GameToScoreBoardMapper>();
services.AddSingleton<IDateTimeFactory, DateTimeFactory>();
services.AddTransient<GameCommandService>();
services.AddTransient<GameQueryService>();
services
    .AddTransient(sp =>
    {
        var smtpConfiguration =
            new SmtpConfiguration
            {
                Hostname = "smtp",
                Port = 25
            };
        return smtpConfiguration;
    });
services.AddSingleton<IGameRepository, GameRepositoryInMemory>();
services.AddTransient<INotifier, EmailNotifier>();
services.AddControllers();

services.AddOpenApi(); // OpenAPI

var app = builder.Build();

// Http Pipeline
app.UseOpenApi(); // OpenAPI

app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.Run();
