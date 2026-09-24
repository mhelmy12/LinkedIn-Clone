using System;
using EngagementService.Data;
using MediatR;

namespace EngagementService.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : ITransactionCommand
{
    private readonly EngagmentDbContext dbContext;

    public TransactionBehavior(EngagmentDbContext dbContext)
    {
        this.dbContext = dbContext;
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
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

}


public interface ITransactionCommand
{

}
