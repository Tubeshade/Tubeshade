using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using NodaTime;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Tasks;

public sealed partial class TaskRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<TaskEntity>(connection)
{
    /// <inheritdoc />
    /// <remarks>
    /// A task for a library belongs to the library's owner, otherwise to <see cref="TaskEntity.OwnerId"/>.
    /// The creating user must hold Create on that owner.
    /// </remarks>
    protected override string InsertSql => Queries.Insert;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    /// <summary>Finds the task, unless it already has an unfinished task run.</summary>
    /// <remarks>
    /// The task must first be locked with <see cref="TryLockTaskForSystemAsync"/> in a separate statement,
    /// so that this query sees task runs committed by the previous holder of the lock.
    /// </remarks>
    public async ValueTask<TaskEntity?> TryDequeueTaskForSystemAsync(SystemPrincipal system, Guid taskId, NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<TaskEntity>(new CommandDefinition(
            Queries.TryDequeueForSystem,
            new { taskId },
            transaction));
    }

    public async ValueTask AddTaskRunForSystemAsync(SystemPrincipal system, Guid taskRunId, Guid taskId, TaskSource source, NpgsqlTransaction transaction)
    {
        await Connection.ExecuteAsync(new CommandDefinition(
            Queries.AddTaskRunForSystem,
            new { taskRunId, taskId, source },
            transaction));
    }

    /// <summary>Locks the task, unless it is already locked by another transaction.</summary>
    /// <returns><see langword="true"/> if the task was locked, otherwise <see langword="false"/>.</returns>
    public async ValueTask<bool> TryLockTaskForSystemAsync(SystemPrincipal system, Guid taskId, NpgsqlTransaction transaction)
    {
        var lockedId = await Connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
            Queries.TryLockTaskForSystem,
            new { taskId },
            transaction));

        return lockedId is not null;
    }

    /// <summary>Deletes the task request, unless it was already deleted or is locked by another transaction.</summary>
    /// <returns>The deleted task request, or <see langword="null"/> if it was not deleted.</returns>
    public async ValueTask<TaskRequest?> TryDeleteTaskRequestForSystemAsync(SystemPrincipal system, Guid taskRequestId, NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<TaskRequest>(new CommandDefinition(
            Queries.TryDeleteTaskRequestForSystem,
            new { taskRequestId },
            transaction));
    }

    /// <summary>
    /// Deletes the task request, unless it was already deleted.
    /// If another transaction locked the request, waits for it to finish instead of skipping the request,
    /// and only deletes the request if the other transaction did not delete it.
    /// </summary>
    /// <returns>The deleted task request, or <see langword="null"/> if it was not deleted.</returns>
    public async ValueTask<TaskRequest?> TryDeleteTaskRequestWaitingForLockForSystemAsync(
        SystemPrincipal system,
        Guid taskRequestId,
        NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<TaskRequest>(new CommandDefinition(
            Queries.TryDeleteTaskRequestWaitingForLockForSystem,
            new { taskRequestId },
            transaction));
    }

    /// <summary>Finds the task and source of an existing task run.</summary>
    public async ValueTask<TaskRequest?> FindTaskRunRequestForSystemAsync(SystemPrincipal system, Guid taskRunId, NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<TaskRequest>(new CommandDefinition(
            Queries.FindTaskRunRequestForSystem,
            new { taskRunId },
            transaction));
    }

    public async ValueTask<List<Guid>> GetTaskRequestIdsForSystemAsync(SystemPrincipal system, CancellationToken cancellationToken)
    {
        var ids = await Connection.QueryAsync<Guid>(new CommandDefinition(
            Queries.GetTaskRequestIdsForSystem,
            cancellationToken: cancellationToken));

        return ids.AsList();
    }

    public async ValueTask StartTaskRunForSystemAsync(SystemPrincipal system, Guid taskRunId, CancellationToken cancellationToken)
    {
        var count = await Connection.ExecuteAsync(new CommandDefinition(
            Queries.StartTaskRunForSystem,
            new { taskRunId },
            cancellationToken: cancellationToken));

        Trace.Assert(count is 1);
    }

    /// <summary>Requests the task to be run, unless the task does not exist, for example because it was deleted.</summary>
    public async ValueTask TriggerTaskForSystemAsync(SystemPrincipal system, Guid taskId, TaskSource source, NpgsqlTransaction transaction)
    {
        await Connection.ExecuteAsync(
            Queries.TriggerTaskForSystem,
            new { taskId, source },
            transaction);
    }

    /// <summary>Requests the task to be run, if the user holds Create on the task's owner.</summary>
    public async ValueTask TriggerTask(Guid taskId, TaskSource source, Guid userId, NpgsqlTransaction transaction)
    {
        var requestIds = await Connection.QueryAsync<Guid>(
            Queries.TriggerTask,
            Access.Parameters(new UserPrincipal(userId), Resources.Task, Permissions.Create, new { taskId, source }),
            transaction);

        if (!requestIds.Any())
        {
            throw new InvalidOperationException("Failed to trigger task due to missing access");
        }
    }

    public async ValueTask CancelTaskRun(Guid taskRunId, Guid userId, NpgsqlTransaction transaction)
    {
        await Connection.ExecuteAsync(
            Queries.CancelTaskRun,
            Access.Parameters(new UserPrincipal(userId), Resources.Task, Permissions.Update, new { taskRunId }),
            transaction);
    }

    public async ValueTask InitializeTaskProgressForSystemAsync(SystemPrincipal system, Guid taskRunId, decimal target, decimal value = 0)
    {
        await Connection.ExecuteAsync(new CommandDefinition(
            Queries.InitializeTaskProgressForSystem,
            new { taskRunId, target, value }));
    }

    public async ValueTask UpdateProgressForSystemAsync(
        SystemPrincipal system,
        Guid taskRunId,
        decimal newValue,
        decimal? rate = null,
        Period? remainingDuration = null)
    {
        var count = await Connection.ExecuteAsync(new CommandDefinition(
            Queries.UpdateProgressForSystem,
            new { taskRunId, newValue, rate, remainingDuration }));

        Trace.Assert(count is 1);
    }

    /// <summary>Gets the progress of the latest previous run of the same task that reported progress, unless that run was successful.</summary>
    /// <remarks>
    /// Runs that failed before reporting any progress are skipped,
    /// so that the progress is kept if a retry fails again before reaching the same point.
    /// </remarks>
    public async ValueTask<decimal?> GetPreviousProgressForSystemAsync(SystemPrincipal system, Guid taskRunId)
    {
        return await Connection.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition(
            Queries.GetPreviousProgressForSystem,
            new { taskRunId }));
    }

    /// <summary>Saves the result of a task run, unless the task run already has a result.</summary>
    /// <returns>The id of the saved result, or <see langword="null"/> if the task run already had a result.</returns>
    public async ValueTask<Guid?> FinishTaskForSystemAsync(
        SystemPrincipal system,
        Guid taskRunId,
        TaskResult result,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        await using var transaction = await Connection.OpenAndBeginTransaction(cancellationToken);
        await FinishTaskRun(taskRunId, transaction);

        var message = exception?.Message;
        var id = await Connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
            Queries.FinishTaskForSystem,
            new { taskRunId, result, message },
            transaction));

        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async ValueTask<List<RunningTaskEntity>> GetRunningTasks(
        TaskParameters parameters,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(Queries.GetRunningTasks, parameters, cancellationToken: cancellationToken);
        var enumerable = await Connection.QueryAsync<RunningTaskEntity>(command);
        return enumerable as List<RunningTaskEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<RunningTaskEntity>> GetRunningTasks(
        TaskParameters parameters,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(Queries.GetRunningTasks, parameters, transaction,
            cancellationToken: cancellationToken);
        var enumerable = await Connection.QueryAsync<RunningTaskEntity>(command);
        return enumerable as List<RunningTaskEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<Guid>> GetBlockingTaskRunIdsForSystemAsync(
        SystemPrincipal system,
        BlockingTaskParameters parameters,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            Queries.GetBlockingTaskRunIdsForSystem,
            parameters,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<Guid>(command);
        return enumerable as List<Guid> ?? enumerable.ToList();
    }

    public async ValueTask<Guid> AddTask(TaskEntity task, NpgsqlTransaction transaction)
    {
        return await AddAsync(task, transaction) ?? throw new InvalidOperationException("Failed to create the task");
    }

    /// <summary>Adds the task, unless an unfinished task for the same work exists, if the creating user holds Create on the task's owner.</summary>
    public async ValueTask<Guid?> TryAddTask(TaskEntity task, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(Queries.TryAddTask, Access.WithPermissions(task, Permissions.Create), transaction);
        return await Connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    /// <summary>Adds the task without checking access, unless an unfinished task for the same work exists.</summary>
    public async ValueTask<Guid?> TryAddTaskForSystemAsync(SystemPrincipal system, TaskEntity task, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(Queries.TryAddTaskForSystem, task, transaction);
        return await Connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    /// <summary>Updates the task, if <see cref="TaskEntity.ModifiedByUserId"/> holds Update on its owner.</summary>
    public async ValueTask<int> UpdateAsync(TaskEntity task)
    {
        var command = new CommandDefinition(UpdateSql, Access.WithPermissions(task, Permissions.Update));
        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask CompleteStuckTasksForSystemAsync(SystemPrincipal system, CancellationToken cancellationToken)
    {
        // ReadCommitted, so that ON CONFLICT skips results committed concurrently, for example by a previous instance during a rolling restart.
        // At Serializable, a conflicting result that is not visible to the snapshot fails with a serialization error instead.
        await using var transaction = await Connection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted, cancellationToken);

        var taskRunIds = await Connection.QueryAsync<Guid>(new(
            Queries.GetUnfinishedTaskRunIdsForSystem,
            new { },
            transaction));

        foreach (var taskRunId in taskRunIds.Distinct())
        {
            await FinishTaskRun(taskRunId, transaction);
            await Connection.ExecuteAsync(new(
                Queries.FailStuckTaskRunForSystem,
                new { taskRunId, result = TaskResult.Failed },
                transaction));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async ValueTask<int> DeleteOldTasksForSystemAsync(SystemPrincipal system, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(Queries.DeleteOldTasksForSystem, null, transaction, cancellationToken: cancellationToken);
        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<int> DeleteOldReindexRunsForSystemAsync(SystemPrincipal system, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            Queries.DeleteOldReindexRunsForSystem,
            null,
            transaction,
            cancellationToken: cancellationToken);

        return await Connection.ExecuteAsync(command);
    }

    private async ValueTask FinishTaskRun(Guid taskRunId, NpgsqlTransaction transaction)
    {
        await Connection.ExecuteAsync(new CommandDefinition(
            Queries.FinishTaskRun,
            new { taskRunId },
            transaction));
    }
}
