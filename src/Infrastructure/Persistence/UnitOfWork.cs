using Application.DomainEvents;
using Application.Interface;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly DomainEventDispatcher _dispatcher;

    private IDbContextTransaction? _transaction;


    public UnitOfWork(
        AppDbContext dbContext,
        DomainEventDispatcher dispatcher)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
    }


    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var strategy =
            _dbContext.Database.CreateExecutionStrategy();


        await strategy.ExecuteAsync(async () =>
        {
            await BeginTransactionAsync(cancellationToken);

            try
            {
                await action(cancellationToken);


 


                await DispatchDomainEventsAsync(
                    cancellationToken);


                await SaveChangesAsync(cancellationToken);


                await CommitTransactionAsync(
                    cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(
                    cancellationToken);

                throw;
            }
        });
    }



    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var strategy =
            _dbContext.Database.CreateExecutionStrategy();


        return await strategy.ExecuteAsync(async () =>
        {
            await BeginTransactionAsync(
                cancellationToken);

            try
            {
                var result =
                    await action(cancellationToken);


         

                await DispatchDomainEventsAsync(
                    cancellationToken);


                await SaveChangesAsync(
                    cancellationToken);


                await CommitTransactionAsync(
                    cancellationToken);


                return result;
            }
            catch
            {
                await RollbackTransactionAsync(
                    cancellationToken);

                throw;
            }
        });
    }



    private async Task DispatchDomainEventsAsync(
        CancellationToken cancellationToken)
    {
        var operations =
            _dbContext.ChangeTracker
                .Entries<Operation>()
                .Select(x => x.Entity)
                .ToList();


        var events =
            operations
                .SelectMany(x => x.DomainEvents)
                .ToList();


        if (events.Count == 0)
            return;


        await _dispatcher.DispatchAsync(
            events,
            cancellationToken);


        foreach (var operation in operations)
        {
            operation.ClearDomainEvents();
        }
    }



    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        _transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);
    }



    private async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
    

        await _dbContext.SaveChangesAsync(
            cancellationToken);

    }



    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken)
    {
        if (_transaction is null)
            return;


        await _transaction.CommitAsync(
            cancellationToken);


        await _transaction.DisposeAsync();


        _transaction = null;
    }



    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken)
    {
        if (_transaction is null)
            return;


        await _transaction.RollbackAsync(
            cancellationToken);


        await _transaction.DisposeAsync();


        _transaction = null;
    }
}
