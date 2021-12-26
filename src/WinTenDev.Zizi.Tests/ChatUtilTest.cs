using FluentAssertions;
using WinTenDev.Zizi.Utils;
using Xunit;

namespace WinTenDev.Zizi.Tests;

public class ChatUtilTest
{
    private const long ChatId = -1001404591750;
    private const long ReducedChatId = 1404591750;

    [Fact]
    public void ReduceChatIdTest()
    {
        var reduced = ChatId.ReduceChatId();

        reduced.Should().Be(ReducedChatId);
    }

    [Fact]
    public void FixChatIdTest()
    {
        var fix = ReducedChatId.FixChatId();

        fix.Should().Be(ChatId);
    }
}