using System.Text;
using Asp.Versioning;
using Database.EntityFramework;
using Database.EntityFramework.Models;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Database.Repository.TokenRepo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rest_API.Seeder;
using Rest_API.Services.Auth;
using Rest_API.Services.Token;

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
        builder.Services.AddOpenApiDocument();

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
                        ValidAudience = builder.Configuration["JWT:ValidAudience"],
                        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
                    };
                }
            );

        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        builder.Services.AddScoped<ITokenService, TokenService>();

        // Register Repos
        builder.Services.AddScoped<ITokenRepo, TokenRepo>();
        builder.Services.AddScoped<IInfluxRepo, InfluxRepo>();
        builder.Services.AddScoped<ISettingsRepo, SettingsRepo>();

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
            app.UseDeveloperExceptionPage();
            //app.UseReDoc(options => { options.Path = "/redoc"; });

            builder.Configuration.AddUserSecrets<Program>();

            using var scope = ((IApplicationBuilder)app).ApplicationServices.CreateScope();
            ApplicationDbContext.ApplyMigration<ApplicationDbContext>(scope);
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Seed the database with initial data
        await SeedUser.SeedData(app);

        await app.RunAsync();
    }
}