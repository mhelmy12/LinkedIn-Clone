using ChatService.Services;
using Xunit;

namespace ChatService.Tests.Services;

public class ChatGroupManagerTests
{
    [Fact]
    public void UserGroup_UsesUserPrefix()
    {
        Assert.Equal("user:user-1", ChatGroupManager.UserGroup("user-1"));
    }

    [Fact]
    public void ConversationGroup_UsesConversationPrefix()
    {
        Assert.Equal("conv:42", ChatGroupManager.ConversationGroup(42));
    }

    [Fact]
    public void PresenceGroup_UsesPresencePrefix()
    {
        Assert.Equal("presence:user-1", ChatGroupManager.PresenceGroup("user-1"));
    }
}
