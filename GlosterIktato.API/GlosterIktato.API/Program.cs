using GlosterIktato.API.BackgroundServices;
using GlosterIktato.API.Data;
using GlosterIktato.API.Services;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. DATABASE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. HTTP CLIENT (Dataxo számára)
builder.Services.AddHttpClient("DataxoClient", client =>
{
    var baseUrl = builder.Configuration["Dataxo:BaseUrl"] ?? "https://api.dataxo.example.com";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// HTTP CLIENT (Business Central számára)
builder.Services.AddHttpClient("BusinessCentralClient", client =>
{
    var baseUrl = builder.Configuration["BusinessCentral:BaseUrl"] ?? "https://api.businesscentral.dynamics.com";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    // TODO: Add OAuth authentication header in production
});

// 3. MEMORY CACHE (BC master data cache-eléshez)
builder.Services.AddMemoryCache();

// 4. SERVICES (Dependency Injection)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IArchiveNumberService, ArchiveNumberService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDataxoService, DataxoService>();
builder.Services.AddScoped<IBusinessCentralService, BusinessCentralService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();

// 5. BACKGROUND SERVICES
builder.Services.AddHostedService<DataxoPollingService>(); // 30 sec-es polling

// 6. JWT AUTHENTICATION
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Dev környezetben false, prod-ban true!
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero // Token azonnal lejár az expiry idõpontban
    };
});

builder.Services.AddAuthorization();

// 7. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        policy => policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000") // Vue dev server
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// 8. CONTROLLERS & SWAGGER
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // JWT Bearer auth Swagger-ben
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// BUILD APP
var app = builder.Build();

// 9. SEED DATA (csak dev környezetben)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Migration futtatása automatikusan
        logger.LogInformation("Running database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Migrations completed");

        // Seed data futtatása
        logger.LogInformation("Seeding database...");
        await DbSeeder.SeedAsync(context);
        logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
    }
}

// 10. MIDDLEWARE PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gloster Iktató API v1");
        options.RoutePrefix = "swagger"; // URL: /swagger
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowVueApp"); // CORS elõbb mint Auth!

app.UseAuthentication(); // Authentication elõbb mint Authorization
app.UseAuthorization();

app.MapControllers();

// 11. RUN
app.Run();