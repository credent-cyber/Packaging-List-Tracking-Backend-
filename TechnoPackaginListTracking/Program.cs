using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TechnoPackaginListTracking.DataContext;
using TechnoPackaginListTracking.DataContext.Models;
using TechnoPackaginListTracking.Infrastructure;
using TechnoPackaginListTracking.Repositories;
using TechnoPackaginListTracking.Services; 

var builder = WebApplication.CreateBuilder(args);

// Retrieve configuration values
var useSqlLite = builder.Configuration.GetValue<bool>("UseSqlLite");
var SqlLiteAuthConnectionString = builder.Configuration.GetValue<string>("SqlLiteAuthConnectionString");
var SqlLiteDBConnectionString = builder.Configuration.GetValue<string>("SqlLiteDBConnectionString");

// Configure Entity Framework and Identity for Auth and App Databases
if (useSqlLite)
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
    {
        options.UseSqlite(SqlLiteAuthConnectionString);
        options.EnableSensitiveDataLogging(); // Enable detailed logging
        //options.LogTo(Console.WriteLine);      // Log EF Core SQL queries to console
    });

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlite(SqlLiteDBConnectionString);
        options.EnableSensitiveDataLogging(); // Enable detailed logging
        //options.LogTo(Console.WriteLine);      // Log EF Core SQL queries to console
    });
}
else
{
    // Configure MSSQL database context
    builder.Services.AddDbContext<AuthDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDBConnection"));
        options.EnableSensitiveDataLogging(); // Enable detailed logging
        //options.LogTo(Console.WriteLine);      // Log EF Core SQL queries to console
    });

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultDbConnection"));
        options.EnableSensitiveDataLogging(); // Enable detailed logging
        //options.LogTo(Console.WriteLine);      // Log EF Core SQL queries to console
    });
}

// Add services to the container, using OData
builder.Services.AddControllers()
    .AddODataControllers()
    .AddNewtonsoftJson();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        //ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        //ClockSkew = TimeSpan.Zero // Adjust clock skew if needed
    };
});

// Read allowed origins from configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

// Configure CORS to allow multiple domains
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policyBuilder =>
        policyBuilder.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});


// Configure EmailSender
builder.Services.AddTransient<IEmailSender>(provider =>
{
    var config = builder.Configuration.GetSection("EmailSettings");
    var smtpServer = config["SmtpServer"];
    var smtpPort = int.Parse(config["SmtpPort"]);
    var smtpUser = config["SmtpUser"];
    var smtpPass = config["SmtpPass"];
    var logger = provider.GetRequiredService<ILogger<EmailSender>>();
    return new EmailSender(smtpServer, smtpPort, smtpUser, smtpPass, logger);
});

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = false;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});


// Configure Logging
builder.Logging.AddConsole();
builder.Services.AddLogging();
builder.Services.AddScoped<BaseRepository>();
builder.Services.AddScoped<IDataRepository, DataRepository>();
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.AddConsole();
builder.Services.AddLogging();


var app = builder.Build();

// Apply database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AuthDbContext>();
        context.Database.Migrate();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.EnsureCreated())
        {
            // Seed data if required
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
