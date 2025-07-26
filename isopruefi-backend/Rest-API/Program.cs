using Asp.Versioning;
using Database.EntityFramework;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;

namespace Rest_API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        //builder.Services.AddAuthorization();

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

        // Register Database
        builder.Services.AddSingleton<SettingsContext>();
        builder.Services.AddSingleton<IInfluxRepo, InfluxRepo>();

        // Register Repos
        builder.Services.AddSingleton<ISettingsRepo, SettingsRepo>();

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
            app.UseDeveloperExceptionPage();
            // app.UseReDoc(options => { options.Path = "/redoc"; });

            builder.Configuration.AddUserSecrets<Program>();
        }

        app.UseHttpsRedirection();
        app.MapControllers();

        //  app.UseAuthorization();

        app.Run();
    }
}