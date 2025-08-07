using System.Net;
using System.Text;
using Asp.Versioning;
using Database.EntityFramework;
using Database.EntityFramework.Models;
using Database.Repository.CoordinateRepo;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Database.Repository.TokenRepo;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using Rest_API.Helper;
using Rest_API.Models;
using Rest_API.Seeder;
using Rest_API.Services.Auth;
using Rest_API.Services.Temp;
using Rest_API.Services.Token;
using Rest_API.Services.User;

namespace Rest_API;

//Migration dotnet ef migrations add Init --project ./Database/Database.csproj --startup-project ./Rest-API/Rest-API.csproj

/// <summary>
///  Main entry point for the application.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the Rest API application.
    /// </summary>
    /// <param name="args"></param>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), b => { });
        });

        builder.Services.AddIdentity<ApiUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApiDocument(configure =>
        {
            configure.Title = "IsoPruefi API";
            configure.Version = "v1";
            configure.Description = "Temperature monitoring API with JWT authentication";

            configure.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token in the text input below."
            });

            configure.OperationProcessors.Add(
                new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        });
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader());
            })
            .AddMvc() // This is needed for controllers
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }
            )
            .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:ValidAudience"],
                        ValidIssuer = builder.Configuration["Jwt:ValidIssuer"],
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
                    };
                }
            );

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin))
            .AddPolicy("UserOrAdmin", policy => policy.RequireRole(Roles.User, Roles.Admin));
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<ITempService, TempService>();
        
        // Register HttpFactory
        builder.Services.AddHttpClient();

        // Register Repos
        builder.Services.AddScoped<ITokenRepo, TokenRepo>();
        builder.Services.AddScoped<IInfluxRepo, InfluxRepo>();
        builder.Services.AddScoped<ISettingsRepo, SettingsRepo>();
        builder.Services.AddScoped<ICoordinateRepo, CoordinateRepo>();

        builder.ConfigureHealthChecks();

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi(settings =>
            {
                settings.DocumentTitle = "IsoPruefi API Documentation";
                settings.OAuth2Client = new OAuth2ClientSettings
                {
                    ClientId = "swagger",
                    AppName = "IsoPruefi API"
                };
            });
            app.UseDeveloperExceptionPage();

            builder.Configuration.AddUserSecrets<Program>();

            using var scope = ((IApplicationBuilder)app).ApplicationServices.CreateScope();
            ApplicationDbContext.ApplyMigration<ApplicationDbContext>(scope);
        }

        //app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        //HealthCheck Middleware
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecksPrometheusExporter("/healthoka",
            options => options.ResultStatusCodes[HealthStatus.Unhealthy] = (int)HttpStatusCode.OK);

        app.MapControllers();

        // Seed the database with initial data
        await SeedUser.SeedData(app);

        await app.RunAsync();
    }
}