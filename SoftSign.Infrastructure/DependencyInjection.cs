using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Interfaces;
using SoftSign.Infrastructure.Data;
using SoftSign.Infrastructure.Repositories;
using SoftSign.Infrastructure.Services;

namespace SoftSign.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                    .CommandTimeout(60)));

        // Identity
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddSingleton<IFileStorageService>(provider =>
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            var logger = provider.GetRequiredService<ILogger<FileStorageService>>();
            var storagePath = configuration["FileStorage:BasePath"] 
                ?? Path.Combine(env.ContentRootPath, "uploads");
            return new FileStorageService(storagePath, logger);
        });

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPdfService, PdfService>();

        return services;
    }
}
