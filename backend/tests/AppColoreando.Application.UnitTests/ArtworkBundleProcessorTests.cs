using AppColoreando.Application.Services;

namespace AppColoreando.Application.UnitTests;

public sealed class ArtworkBundleProcessorTests
{
    [Fact]
    public void Validate_accepts_palette_and_regions_and_returns_checksum()
    {
        var processor = new ArtworkBundleProcessor();

        var result = processor.Validate("""{"palette":[{"id":1,"hex":"#246b5a"}],"regions":[{"id":1,"colorId":1,"polygon":[[0,0],[1,0],[1,1]]}]}""");

        Assert.True(result.IsValid);
        Assert.Equal(1, result.PaletteCount);
        Assert.Equal(1, result.RegionCount);
        Assert.NotEmpty(result.Checksum);
    }

    [Fact]
    public void Validate_rejects_missing_palette_and_regions()
    {
        var processor = new ArtworkBundleProcessor();

        var result = processor.Validate("""{"version":1}""");

        Assert.False(result.IsValid);
        Assert.Contains("Palette is required.", result.Errors);
        Assert.Contains("At least one region is required.", result.Errors);
    }
}

