using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class WorkflowServiceTests
{
    [Fact]
    public async Task StartWorkflowAsync_CreatesInstance()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new YuktiraDbContext(options);

        var wf = new WorkflowDefinitionEntity
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Test",
            Code = "TEST",
            Module = "Test",
            IsActive = true,
            Version = 1
        };
        var node = new WorkflowNodeEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = wf.Id,
            NodeType = "START",
            Label = "Start"
        };
        db.Set<WorkflowDefinitionEntity>().Add(wf);
        db.Set<WorkflowNodeEntity>().Add(node);
        await db.SaveChangesAsync();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        var service = new WorkflowService(db, httpClientFactory.Object);

        var instanceId = await service.StartWorkflowAsync(wf.Id, wf.TenantId, "TestEntity", "entity-1", Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, instanceId);
    }

    [Fact]
    public async Task StartWorkflowAsync_InactiveWorkflow_Throws()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new YuktiraDbContext(options);

        var wf = new WorkflowDefinitionEntity
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Inactive",
            Code = "INACTIVE",
            Module = "Test",
            IsActive = false,
            Version = 1
        };
        db.Set<WorkflowDefinitionEntity>().Add(wf);
        await db.SaveChangesAsync();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        var service = new WorkflowService(db, httpClientFactory.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartWorkflowAsync(wf.Id, wf.TenantId, "Test", "e-1", Guid.NewGuid()));
    }
}
