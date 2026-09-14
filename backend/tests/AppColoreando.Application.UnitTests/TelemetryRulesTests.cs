using AppColoreando.Application.Services;

namespace AppColoreando.Application.UnitTests;

public sealed class TelemetryRulesTests
{
    [Fact]
    public void Allows_only_known_event_names()
    {
        Assert.True(TelemetryRules.IsAllowedEvent("artwork_open"));
        Assert.False(TelemetryRules.IsAllowedEvent("email_captured"));
    }

    [Fact]
    public void Sanitizes_property_keys_and_values()
    {
        var properties = TelemetryRules.SanitizeProperties(new Dictionary<string, string>
        {
            ["source"] = " catalog ",
            ["email"] = "secret@example.com"
        });
        Assert.Equal("catalog", properties["source"]);
        Assert.False(properties.ContainsKey("email"));
    }

    [Fact]
    public void Replaces_out_of_window_timestamp()
    {
        var now = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now, TelemetryRules.NormalizeOccurredAt(now.AddDays(-30), now));
    }
}