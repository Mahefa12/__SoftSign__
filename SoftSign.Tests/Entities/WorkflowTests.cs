using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using Xunit;

namespace SoftSign.Tests.Entities;

public class WorkflowTests
{
    [Fact]
    public void Workflow_Constructor_SetsDefaultValues()
    {
        var workflow = new Workflow();
        Assert.Equal(string.Empty, workflow.Name);
        Assert.Equal(WorkflowStatus.Active, workflow.Status);
        Assert.False(workflow.IsDefault);
    }

    [Fact]
    public void WorkflowStep_Constructor_SetsDefaultValues()
    {
        var step = new WorkflowStep();
        Assert.Equal(string.Empty, step.Name);
        Assert.Equal(SignatureLevel.Signature, step.RequiredSignatureLevel);
        Assert.False(step.IsCompleted);
    }

    [Fact]
    public void User_Constructor_SetsDefaultValues()
    {
        var user = new User();
        Assert.Equal(string.Empty, user.FirstName);
        Assert.True(user.IsActive);
        Assert.False(user.IsDeleted);
    }
}
