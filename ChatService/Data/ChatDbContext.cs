using ChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Data;

/// <summary>
/// Owns the <c>chatDb</c> PostgreSQL database. No other service database is
/// referenced from this context (Constitution I / FR-016).
/// </summary>
public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ConversationKeyEnvelope> ConversationKeyEnvelopes => Set<ConversationKeyEnvelope>();
    public DbSet<UserDeviceKey> UserDeviceKeys => Set<UserDeviceKey>();
    public DbSet<ChatUserProjection> ChatUserProjections => Set<ChatUserProjection>();
    public DbSet<ChatConnectionProjection> ChatConnectionProjections => Set<ChatConnectionProjection>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
    }
}
