using AppColoreando.Domain.Entities;

namespace AppColoreando.Domain.UnitTests;

public sealed class ArtworkDomainTests
{
    [Fact]
    public void Artwork_is_published_only_when_status_is_published()
    {
        var artwork = new Artwork { PublishingStatus = PublishingStatus.Approved };
        Assert.False(artwork.IsPublished);

        artwork.PublishingStatus = PublishingStatus.Published;
        Assert.True(artwork.IsPublished);
    }

    [Fact]
    public void Brand_placeholders_can_be_disabled_without_removing_rights_model()
    {
        var brand = new Brand { Name = "Licensed Placeholder", Slug = "licensed-placeholder", Enabled = false };
        Assert.False(brand.Enabled);
        Assert.Equal("licensed-placeholder", brand.Slug);
    }
}

