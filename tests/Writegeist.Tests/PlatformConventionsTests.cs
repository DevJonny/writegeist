using FluentAssertions;
using Writegeist.Core;
using Writegeist.Core.Models;

namespace Writegeist.Tests;

public class PlatformConventionsTests
{
    [Fact]
    public void GetRules_LinkedIn_ReturnsCorrectRules()
    {
        var rules = PlatformConventions.GetRules(Platform.LinkedIn);

        rules.Name.Should().Be("LinkedIn");
        rules.MaxCharacters.Should().Be(3000);
        rules.RecommendedMaxLength.Should().Be(1500);
        rules.SupportsHashtags.Should().BeTrue();
        rules.RecommendedHashtagCount.Should().BeInRange(3, 5);
        rules.HashtagPlacement.Should().Be("end");
        rules.SupportsEmoji.Should().BeTrue();
        rules.EmojiGuidance.Should().ContainEquivalentOf("sparingly");
    }

    [Fact]
    public void GetRules_X_ReturnsCorrectRules()
    {
        var rules = PlatformConventions.GetRules(Platform.X);

        rules.Name.Should().Be("X");
        rules.MaxCharacters.Should().Be(280);
        rules.RecommendedMaxLength.Should().Be(280);
        rules.SupportsHashtags.Should().BeTrue();
        rules.RecommendedHashtagCount.Should().BeInRange(1, 2);
        rules.HashtagPlacement.Should().Be("inline");
        rules.SupportsEmoji.Should().BeTrue();
        rules.EmojiGuidance.Should().ContainEquivalentOf("moderate");
    }

    [Fact]
    public void GetRules_Instagram_ReturnsCorrectRules()
    {
        var rules = PlatformConventions.GetRules(Platform.Instagram);

        rules.Name.Should().Be("Instagram");
        rules.MaxCharacters.Should().Be(2200);
        rules.RecommendedMaxLength.Should().Be(750);
        rules.SupportsHashtags.Should().BeTrue();
        rules.RecommendedHashtagCount.Should().BeGreaterThanOrEqualTo(30);
        rules.HashtagPlacement.Should().Contain("end");
        rules.SupportsEmoji.Should().BeTrue();
        rules.EmojiGuidance.Should().ContainEquivalentOf("freely");
    }

    [Fact]
    public void GetRules_Facebook_ReturnsCorrectRules()
    {
        var rules = PlatformConventions.GetRules(Platform.Facebook);

        rules.Name.Should().Be("Facebook");
        rules.MaxCharacters.Should().Be(63206);
        rules.RecommendedMaxLength.Should().Be(600);
        rules.SupportsHashtags.Should().BeTrue();
        rules.RecommendedHashtagCount.Should().BeInRange(1, 3);
        rules.HashtagPlacement.Should().Be("end");
        rules.SupportsEmoji.Should().BeTrue();
        rules.EmojiGuidance.Should().ContainEquivalentOf("moderate");
    }
}
