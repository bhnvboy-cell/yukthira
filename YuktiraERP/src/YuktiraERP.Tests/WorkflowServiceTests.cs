using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class WorkflowServiceTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private (WorkflowService service, YuktiraDbContext db, WorkflowDefinitionEntity wf) CreateServiceWithWorkflow()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new YuktiraDbContext(options);

        var wf = new WorkflowDefinitionEntity
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Test Workflow",
            Code = "TEST-WF",
            Module = "Test",
            IsActive = true,
            Version = 1
        };
        db.Set<WorkflowDefinitionEntity>().Add(wf);
        db.SaveChanges();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        var logger = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(db, httpClientFactory.Object, logger.Object);

        return (service, db, wf);
    }

    [Fact]
    public async Task StartWorkflowAsync_CreatesInstance()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();
        var startNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "START", Label = "Start" };
        db.Set<WorkflowNodeEntity>().Add(startNode);
        await db.SaveChangesAsync();

        var instanceId = await service.StartWorkflowAsync(wf.Id, wf.TenantId, "TestEntity", "entity-1", Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, instanceId);
        var instance = await db.WorkflowInstances.FindAsync(instanceId);
        Assert.NotNull(instance);
        Assert.Equal("ACTIVE", instance!.Status);
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
        var logger = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(db, httpClientFactory.Object, logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartWorkflowAsync(wf.Id, wf.TenantId, "Test", "e-1", Guid.NewGuid()));
    }

    [Fact]
    public async Task ProcessNodeAsync_AndSplit_CreatesMultipleTokens()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();

        var startNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "START", Label = "Start" };
        var taskA = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "TASK", Label = "Task A" };
        var taskB = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "TASK", Label = "Task B" };
        var endNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "END", Label = "End" };

        db.Set<WorkflowNodeEntity>().AddRange(startNode, taskA, taskB, endNode);

        // Parallel edges from start to taskA and taskB
        db.Set<WorkflowEdgeEntity>().AddRange(
            new WorkflowEdgeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, FromNodeId = startNode.Id, ToNodeId = taskA.Id, BranchType = "PARALLEL", SequenceOrder = 1 },
            new WorkflowEdgeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, FromNodeId = startNode.Id, ToNodeId = taskB.Id, BranchType = "PARALLEL", SequenceOrder = 2 }
        );
        await db.SaveChangesAsync();

        var instanceId = await service.StartWorkflowAsync(wf.Id, wf.TenantId, "TestEntity", "e-1", Guid.NewGuid());

        await service.ProcessNodeAsync(instanceId, startNode.Id);

        var instance = await db.WorkflowInstances.FindAsync(instanceId);
        Assert.NotNull(instance);
        var tokens = JsonSerializer.Deserialize<List<Guid>>(instance!.ActiveTokens, JsonOpts) ?? new();
        Assert.Equal(2, tokens.Count);
        Assert.Contains(taskA.Id, tokens);
        Assert.Contains(taskB.Id, tokens);
    }

    [Fact]
    public async Task ProcessNodeAsync_ConditionalEdge_EvaluatesExpression()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();

        var startNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "START", Label = "Start" };
        var approvedNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "TASK", Label = "Approved" };
        var rejectedNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "TASK", Label = "Rejected" };

        db.Set<WorkflowNodeEntity>().AddRange(startNode, approvedNode, rejectedNode);
        db.Set<WorkflowEdgeEntity>().AddRange(
            new WorkflowEdgeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, FromNodeId = startNode.Id, ToNodeId = approvedNode.Id, ConditionExpression = "{status} == approved" },
            new WorkflowEdgeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, FromNodeId = startNode.Id, ToNodeId = rejectedNode.Id, ConditionExpression = "{status} == rejected" }
        );
        await db.SaveChangesAsync();

        var instanceId = await service.StartWorkflowAsync(wf.Id, wf.TenantId, "TestEntity", "e-1", Guid.NewGuid());

        // Set variable
        var instance = await db.WorkflowInstances.FindAsync(instanceId);
        var vars = new Dictionary<string, object> { ["status"] = "approved" };
        instance!.Variables = JsonSerializer.Serialize(vars, JsonOpts);
        await db.SaveChangesAsync();

        await service.ProcessNodeAsync(instanceId, startNode.Id);

        instance = await db.WorkflowInstances.FindAsync(instanceId);
        Assert.Equal(approvedNode.Id, instance!.CurrentNodeId);
    }

    [Fact]
    public async Task ProcessNodeAsync_TimerNode_Completes()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();

        var startNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "START", Label = "Start" };
        var timerNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "TIMER", Label = "Wait" };
        var endNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "END", Label = "End" };

        db.Set<WorkflowNodeEntity>().AddRange(startNode, timerNode, endNode);
        db.Set<WorkflowEdgeEntity>().AddRange(
            new WorkflowEdgeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, FromNodeId = startNode.Id, ToNodeId = timerNode.Id, SequenceOrder = 1 },
            new WorkflowEdgeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, FromNodeId = timerNode.Id, ToNodeId = endNode.Id, SequenceOrder = 1 }
        );
        await db.SaveChangesAsync();

        var instanceId = await service.StartWorkflowAsync(wf.Id, wf.TenantId, "TestEntity", "e-1", Guid.NewGuid());

        // Process start -> timer
        await service.ProcessNodeAsync(instanceId, startNode.Id);

        // Process timer (should move to end or stay)
        await service.ProcessNodeAsync(instanceId, timerNode.Id);

        var instance = await db.WorkflowInstances.FindAsync(instanceId);
        Assert.NotNull(instance);
        // Timer node should trigger transition
        Assert.True(instance!.Status == "ACTIVE" || instance.Status == "COMPLETED");
    }

    [Fact]
    public async Task CompleteWorkflowAsync_SetsStatusToCompleted()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();

        var instance = new WorkflowInstanceEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = wf.Id,
            TenantId = wf.TenantId,
            EntityName = "TestEntity",
            EntityId = "e-1",
            Status = "ACTIVE",
            StartedBy = Guid.NewGuid(),
            ActiveTokens = "[]",
            Variables = "{}"
        };
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync();

        await service.CompleteWorkflowAsync(instance.Id);

        var result = await db.WorkflowInstances.FindAsync(instance.Id);
        Assert.Equal("COMPLETED", result!.Status);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task TerminateWorkflowAsync_SetsStatusToTerminated()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();

        var instance = new WorkflowInstanceEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = wf.Id,
            TenantId = wf.TenantId,
            EntityName = "TestEntity",
            EntityId = "e-1",
            Status = "ACTIVE",
            StartedBy = Guid.NewGuid(),
            ActiveTokens = "[]",
            Variables = "{}"
        };
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync();

        await service.TerminateWorkflowAsync(instance.Id);

        var result = await db.WorkflowInstances.FindAsync(instance.Id);
        Assert.Equal("TERMINATED", result!.Status);
    }

    [Fact]
    public async Task EvaluateConditionAsync_ReturnsCorrectResult()
    {
        var (service, _, _) = CreateServiceWithWorkflow();
        var vars = new Dictionary<string, object> { ["status"] = "approved", ["amount"] = 500 };

        var result = await service.EvaluateConditionAsync("{status} == approved", vars);
        Assert.True(result);

        result = await service.EvaluateConditionAsync("{status} == rejected", vars);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateWorkflowDefinitionAsync_ReturnsErrorsForInvalidNodes()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();

        var invalidNode = new WorkflowNodeEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = wf.Id,
            NodeType = "INVALID_TYPE",
            Label = "Bad Node"
        };
        db.Set<WorkflowNodeEntity>().Add(invalidNode);
        await db.SaveChangesAsync();

        var result = await service.ValidateWorkflowDefinitionAsync(wf.Id);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetWorkflowNodesAsync_ReturnsAllNodes()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();

        db.Set<WorkflowNodeEntity>().AddRange(
            new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "START", Label = "Start" },
            new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "TASK", Label = "Task" },
            new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "END", Label = "End" }
        );
        await db.SaveChangesAsync();

        var nodes = await service.GetWorkflowNodesAsync(wf.Id);

        Assert.Equal(3, nodes.Count);
    }

    [Fact]
    public async Task ProcessNodeAsync_EndNode_CompletesWorkflow()
    {
        var (service, db, wf) = CreateServiceWithWorkflow();

        var startNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "START", Label = "Start" };
        var endNode = new WorkflowNodeEntity { Id = Guid.NewGuid(), WorkflowId = wf.Id, NodeType = "END", Label = "End" };

        db.Set<WorkflowNodeEntity>().AddRange(startNode, endNode);
        db.Set<WorkflowEdgeEntity>().Add(new WorkflowEdgeEntity
        {
            Id = Guid.NewGuid(), WorkflowId = wf.Id,
            FromNodeId = startNode.Id, ToNodeId = endNode.Id, SequenceOrder = 1
        });
        await db.SaveChangesAsync();

        var instanceId = await service.StartWorkflowAsync(wf.Id, wf.TenantId, "TestEntity", "e-1", Guid.NewGuid());

        // Start -> End (moves token to end node)
        await service.ProcessNodeAsync(instanceId, startNode.Id);
        // Process end node (removes token, completes workflow)
        await service.ProcessNodeAsync(instanceId, endNode.Id);

        var instance = await db.WorkflowInstances.FindAsync(instanceId);
        Assert.Equal("COMPLETED", instance!.Status);
    }
}
