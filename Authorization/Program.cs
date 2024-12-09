using System.Reflection;
using System.Text;
using Authorization.Services;
using Authorization.Services.ServiceException;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

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

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(swagger => {
    swagger.AddSecurityDefinition("Jwt", new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Cookie.Name = "GiggleBookAuth";
    });

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters{
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.Configure<RepositoryConfiguration>(builder.Configuration.GetSection(RepositoryConfiguration.PathConfiguration));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.PathConfiguration));

var allowedOrigins = new List<string>();
for (int port = 5000; port <= 5900; port++)
{
    allowedOrigins.Add($"http://localhost:{port}");
    allowedOrigins.Add($"https://localhost:{port}");
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder =>
        {
            builder.WithOrigins(allowedOrigins.ToArray()) 
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); 
        });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Cookies.TryGetValue("GiggleBookAuth", out var token))
    {
        context.Request.Headers["Authorization"] = "Bearer " + token;
    }

    await next();
});

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

app.UseCors("AllowLocalhost");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
