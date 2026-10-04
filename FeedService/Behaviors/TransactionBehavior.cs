using System;
using FeedService.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FeedService.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : ITransactionCommand
{
    private readonly FeedDbContext dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> logger;

    public TransactionBehavior(FeedDbContext dbContext, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return response;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogWarning(
                "Unique violation on reaction save — another request won the race. Reloading.");

            foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;


            throw new InvalidOperationException(
                "Concurrent reaction detected. Please retry.", ex);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("IX_FeedPosts_PostId") == true
            || ex.InnerException?.Message.Contains("duplicate key") == true;
    }

}

public interface ITransactionCommand
{

}
