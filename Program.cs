using Hl7Gateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hl7Gateway
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Crear WebApplication para API REST
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
            builder.Configuration.AddConfiguration(configuration);
            
            builder.Services.AddControllers();
            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // Configurar CORS para permitir requests desde el frontend
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500", "http://127.0.0.1:8000", "http://localhost:8000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Configurar HttpClient para DirectoryMS
            builder.Services.AddHttpClient<DirectoryService>(client =>
            {
                var baseUrl = configuration["Microservices:DirectoryMS:BaseUrl"] 
                    ?? throw new InvalidOperationException("DirectoryMS BaseUrl no configurado");
                client.BaseAddress = new Uri(baseUrl);
                var authToken = configuration["Microservices:DirectoryMS:AuthToken"];
                if (!string.IsNullOrEmpty(authToken))
                {
                    client.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                }
            });

            // Configurar HttpClient para SchedulingMS
            builder.Services.AddHttpClient<SchedulingService>(client =>
            {
                var baseUrl = configuration["Microservices:SchedulingMS:BaseUrl"] 
                    ?? throw new InvalidOperationException("SchedulingMS BaseUrl no configurado");
                client.BaseAddress = new Uri(baseUrl);
                var authToken = configuration["Microservices:SchedulingMS:AuthToken"];
                if (!string.IsNullOrEmpty(authToken))
                {
                    client.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                }
            });

            builder.Services.AddSingleton<Hl7MessageLogger>();
            // Comentado temporalmente hasta arreglar errores de NHapi
            // builder.Services.AddSingleton<Hl7MessageProcessor>();
            // builder.Services.AddSingleton<MllpListener>();

            var app = builder.Build();
            
            // Habilitar CORS
            app.UseCors();
            
            // Mapear endpoint raíz simple
            app.MapGet("/", () => new
            {
                message = "Hl7Gateway está funcionando correctamente",
                status = "running",
                port = 5000,
                timestamp = DateTime.Now,
                endpoints = new
                {
                    list = "/api/v1/Hl7Summary/list",
                    byAppointment = "/api/v1/Hl7Summary/by-appointment/{appointmentId}",
                    byPatient = "/api/v1/Hl7Summary/by-patient/{patientId}",
                    generate = "/api/v1/Hl7Summary/generate (POST)"
                }
            });
            
            app.MapControllers();

            // Configurar puerto explícitamente solo si ASPNETCORE_URLS no está configurado
            var port = configuration.GetValue<int>("Hl7Gateway:Port", 5000);
            var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            if (string.IsNullOrEmpty(urls))
            {
                var host = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" 
                    ? "0.0.0.0" 
                    : "localhost";
                app.Urls.Clear();
                app.Urls.Add($"http://{host}:{port}");
            }

            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Iniciando Hl7Gateway...");
            logger.LogInformation("API REST disponible en puerto {Port}", port);
            logger.LogWarning("MLLP Listener deshabilitado temporalmente (errores de NHapi)");
            
            // Comentado temporalmente hasta arreglar errores de NHapi
            // var listener = app.Services.GetRequiredService<MllpListener>();
            // logger.LogInformation("MLLP Listener en puerto 2575");
            // _ = Task.Run(async () => await listener.StartAsync());

            // Iniciar Web API
            await app.RunAsync();
        }
    }
}
