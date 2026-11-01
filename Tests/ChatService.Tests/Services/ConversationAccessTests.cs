using ChatService.Data;
using ChatService.Models;
using ChatService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ChatService.Tests.Services;

public sealed class ConversationAccessTests : IDisposable
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";
    private const string Outsider = "user-outsider";

    private readonly ChatDbContext _db;
    private readonly IUserDirectoryClient _directory = Substitute.For<IUserDirectoryClient>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ConversationAccessService _service;

    public ConversationAccessTests()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ChatDbContext(options);
        _service = new ConversationAccessService(
            _db,
            _directory,
            _cache,
            NullLogger<ConversationAccessService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task AuthorizeConversation_RejectsNonParticipant()
    {
        var result = await _service.AuthorizeConversationAsync(
            Conversation(), Outsider, CancellationToken.None);

        Assert.Equal(ConversationAccessStatus.NotParticipant, result.Status);
        Assert.False(result.IsAllowed);
        await _directory.DidNotReceiveWithAnyArgs()
            .VerifyConnectionAsync(default!, default!, default);
    }

    [Fact]
    public async Task AuthorizeConversation_RejectsParticipantWithDifferentCase()
    {
        var result = await _service.AuthorizeConversationAsync(
            Conversation(), UserA.ToUpperInvariant(), CancellationToken.None);

        Assert.Equal(ConversationAccessStatus.NotParticipant, result.Status);
    }

    [Fact]
    public async Task AuthorizeConversation_AllowsWhenProjectionIsActive()
    {
        await SeedProjectionAsync(isActive: true);

        var result = await _service.AuthorizeConversationAsync(
            Conversation(), UserB, CancellationToken.None);

        Assert.True(result.IsAllowed);
        await _directory.DidNotReceiveWithAnyArgs()
            .VerifyConnectionAsync(default!, default!, default);
    }

    [Fact]
    public async Task AuthorizeConversation_DeniesInactiveProjectionWhenApiAlsoDenies()
    {
        await SeedProjectionAsync(isActive: false);
        _directory
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>())
            .Returns(ConnectionVerificationResult.NotConnected);

        var result = await _service.AuthorizeConversationAsync(
            Conversation(), UserA, CancellationToken.None);

        Assert.Equal(ConversationAccessStatus.NotConnected, result.Status);
        await _directory.Received(1)
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthorizeConversation_UsesApiFallbackOnProjectionMissAndCachesPositiveResult()
    {
        _directory
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>())
            .Returns(ConnectionVerificationResult.Connected);

        var first = await _service.AuthorizeConversationAsync(
            Conversation(), UserA, CancellationToken.None);
        var second = await _service.AuthorizeConversationAsync(
            Conversation(), UserB, CancellationToken.None);

        Assert.True(first.IsAllowed);
        Assert.True(second.IsAllowed);
        await _directory.Received(1)
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthorizeConversation_FailsClosedWhenApiIsUnavailable()
    {
        _directory
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>())
            .Returns(ConnectionVerificationResult.Unknown);

        var first = await _service.AuthorizeConversationAsync(
            Conversation(), UserA, CancellationToken.None);
        var second = await _service.AuthorizeConversationAsync(
            Conversation(), UserA, CancellationToken.None);

        Assert.Equal(ConversationAccessStatus.NotConnected, first.Status);
        Assert.Equal(ConversationAccessStatus.NotConnected, second.Status);

        // Unknown is never cached, so both attempts re-check.
        await _directory.Received(2)
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthorizeConversation_ReVerifiesWhenProjectionReportsRemovalAfterPositiveCache()
    {
        _directory
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>())
            .Returns(ConnectionVerificationResult.Connected);

        var first = await _service.AuthorizeConversationAsync(
            Conversation(), UserA, CancellationToken.None);

        await SeedProjectionAsync(isActive: false);

        _directory
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>())
            .Returns(ConnectionVerificationResult.NotConnected);

        var second = await _service.AuthorizeConversationAsync(
            Conversation(), UserA, CancellationToken.None);

        Assert.True(first.IsAllowed);
        Assert.Equal(ConversationAccessStatus.NotConnected, second.Status);
        await _directory.Received(2)
            .VerifyConnectionAsync(UserA, UserB, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthorizePair_ReturnsNotParticipantForBlankCaller()
    {
        var result = await _service.AuthorizePairAsync(" ", UserB, CancellationToken.None);

        Assert.Equal(ConversationAccessStatus.NotParticipant, result.Status);
    }

    private static Conversation Conversation() => new()
    {
        Id = 1,
        Participant1Id = UserA,
        Participant2Id = UserB,
        CreatedAt = DateTime.UtcNow,
        LastMessageAt = DateTime.UtcNow
    };

    private async Task SeedProjectionAsync(bool isActive)
    {
        _db.ChatConnectionProjections.Add(new ChatConnectionProjection
        {
            UserId = UserA,
            OtherUserId = UserB,
            IsActive = isActive,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
