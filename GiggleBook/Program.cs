using Autofac;
using Autofac.Extensions.DependencyInjection;
using GiggleBook.Interfaces;
using GiggleBook.Services;
using GiggleBook.Services.ServiceException;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Reflection;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : Directory.GetCurrentDirectory(),
    ApplicationName = typeof(Program).Assembly.FullName
});

if (WindowsServiceHelpers.IsWindowsService())
{
    Console.WriteLine("WindowsService");

    builder.Host.UseWindowsService();
    builder.Services.AddSingleton<IHostLifetime, WindowsServiceLifetime>();
}
else  if (SystemdHelpers.IsSystemdService())
{
    Console.WriteLine("SystemdService");

    builder.Host.UseSystemd();
    builder.Services.AddSingleton<IHostLifetime, SystemdLifetime>();
}

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new ServicesModule());
    });

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddDateOnlyTimeOnlyStringConverters();

builder.Services.AddEndpointsApiExplorer();

var currentAssemblyXmlDoc = Path.Combine(
        Path.GetDirectoryName(AppContext.BaseDirectory) ?? throw new Exception("Отсутствует файл документации"),
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
    );

builder.Services.Configure<RepositoryConfiguration>(builder.Configuration.GetSection(RepositoryConfiguration.PathConfiguration));
builder.Services.AddSingleton<IRepository, RepositoryService>();

builder.Services.AddSwaggerGen(swagger =>
{
    swagger.IncludeXmlComments(currentAssemblyXmlDoc);
    swagger.UseInlineDefinitionsForEnums();
    swagger.CustomSchemaIds(i => i.ToString());

    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
});

var app = builder.Build();

app.UseExceptionHandler(e =>
{
    e.Run(async c =>
    {
        var exceptionFeature = c.Features.Get<IExceptionHandlerFeature>();
        var errorType = exceptionFeature?.Error;


        if (errorType != null)
        {
            var details = new ProblemDetails
            {
                Title = $"Возникла ошибка. [ {errorType.GetType().Name} ]",
            };
                        
            if (errorType is CommonServiceException typedError)
            {
                c.Response.StatusCode = StatusCodes.Status400BadRequest;
                details.Extensions.Add("code", typedError.Code);
                details.Extensions.Add("message", typedError.Message);
            }

            if (errorType is NpgsqlException npgError)
            {
                c.Response.StatusCode = StatusCodes.Status400BadRequest;
                details.Extensions.Add("code", npgError.ErrorCode);
                details.Extensions.Add("message", npgError.Message);
            }

            if (errorType is ArgumentException)
            {
                c.Response.StatusCode = StatusCodes.Status400BadRequest;
            }

            Assembly assembly = Assembly.GetExecutingAssembly();

            details.Extensions.Add("product", new
            {
                Name = typeof(Program).Assembly.GetName(true).Name,
                Version = Assembly.GetExecutingAssembly().GetName().Version,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            });

            await c.Response.WriteAsJsonAsync(details);
        }
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();
