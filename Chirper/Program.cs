using System.Reflection;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Chirper.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.Configure<RepositoryConfiguration>(builder.Configuration.GetSection(RepositoryConfiguration.PathConfiguration));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.PathConfiguration));

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
    .AddCookie(options => {
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Cookie.Name = "GiggleBookAuth";
    });

builder.Services
    .AddAuthentication(options => {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddEndpointsApiExplorer();

var currentAssemblyXmlDoc = Path.Combine(
        Path.GetDirectoryName(AppContext.BaseDirectory) ?? throw new Exception("Отсутствует файл документации"),
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
    );

builder.Services.AddSwaggerGen(swagger => {
    swagger.IncludeXmlComments(currentAssemblyXmlDoc);
    swagger.UseInlineDefinitionsForEnums();
    swagger.CustomSchemaIds(i => i.ToString());

    swagger.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Cookie,
        Name = "GiggleBookAuth", // Укажите имя вашего куки
        Type = SecuritySchemeType.Http,
        Scheme = "cookie"
    });
});

// var allowedOrigins = new List<string>();
// for (int port = 5000; port <= 5900; port++)
// {
//     allowedOrigins.Add($"http://localhost:{port}");
//     allowedOrigins.Add($"https://localhost:{port}");
// }
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowLocalhost",
//         builder =>
//         {
//             builder.WithOrigins(allowedOrigins.ToArray()) 
//                 .AllowAnyMethod()
//                 .AllowAnyHeader()
//                 .AllowCredentials(); 
//         });
// });

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
                        
            if (errorType is Exception typedError)
            {
                c.Response.StatusCode = StatusCodes.Status400BadRequest;
                details.Extensions.Add("code", typedError.Data);
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

app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
