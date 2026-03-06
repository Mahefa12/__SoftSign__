using Microsoft.Extensions.DependencyInjection;
using SoftSign.Application.Interfaces;
using SoftSign.Application.Services;

namespace SoftSign.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ISignatureService, SignatureService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
