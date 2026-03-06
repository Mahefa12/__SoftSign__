using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;

namespace SoftSign.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed Roles
        string[] roles = { "Admin", "Manager", "User", "Signer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Name = role,
                    NormalizedName = role.ToUpper()
                });
            }
        }

        // Seed Admin User
        var adminEmail = "admin@softsign.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new User
            {
                FirstName = "System",
                LastName = "Administrator",
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // Seed Default Company
        if (!context.Companies.Any())
        {
            var company = new Company
            {
                Name = "SoftSign Demo",
                TradeName = "SoftSign",
                Email = "info@softsign.com",
                IsActive = true
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();
        }

        // Seed Default Workflow
        if (!context.Workflows.Any())
        {
            var workflow = new Workflow
            {
                Name = "Standard Signature Workflow",
                Description = "Default workflow with review and signature steps",
                Status = WorkflowStatus.Active,
                IsDefault = true,
                Order = 1
            };

            workflow.Steps.Add(new WorkflowStep
            {
                Name = "Document Review",
                StepOrder = 1,
                RequiredSignatureLevel = SignatureLevel.None
            });

            workflow.Steps.Add(new WorkflowStep
            {
                Name = "Paraphe (Initials)",
                StepOrder = 2,
                RequiredSignatureLevel = SignatureLevel.Paraphe
            });

            workflow.Steps.Add(new WorkflowStep
            {
                Name = "Final Signature",
                StepOrder = 3,
                RequiredSignatureLevel = SignatureLevel.Signature
            });

            context.Workflows.Add(workflow);
            await context.SaveChangesAsync();
        }
    }
}
