using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.Types;

public partial class EgsFreeGamesDetail
{
    [JsonProperty("productRatings")]
    public ExternalNavLinks ProductRatings { get; set; }

    [JsonProperty("disableNewAddons")]
    public bool DisableNewAddons { get; set; }

    [JsonProperty("modMarketplaceEnabled")]
    public bool ModMarketplaceEnabled { get; set; }

    [JsonProperty("_title")]
    public string Title { get; set; }

    [JsonProperty("_noIndex")]
    public bool NoIndex { get; set; }

    [JsonProperty("productName")]
    public string ProductName { get; set; }

    [JsonProperty("pageTheme")]
    public PageTheme PageTheme { get; set; }

    [JsonProperty("namespace")]
    public string Namespace { get; set; }

    [JsonProperty("theme")]
    public ExternalNavLinks Theme { get; set; }

    [JsonProperty("reviewOptOut")]
    public bool ReviewOptOut { get; set; }

    [JsonProperty("externalNavLinks")]
    public ExternalNavLinks ExternalNavLinks { get; set; }

    [JsonProperty("_urlPattern")]
    public string UrlPattern { get; set; }

    [JsonProperty("_slug")]
    public string Slug { get; set; }

    [JsonProperty("_activeDate")]
    public DateTimeOffset ActiveDate { get; set; }

    [JsonProperty("lastModified")]
    public DateTimeOffset LastModified { get; set; }

    [JsonProperty("_locale")]
    public string Locale { get; set; }

    [JsonProperty("_id")]
    public Guid Id { get; set; }

    [JsonProperty("_templateName")]
    public string TemplateName { get; set; }

    [JsonProperty("pages", NullValueHandling = NullValueHandling.Ignore)]
    public List<EgsFreeGamesDetail> Pages { get; set; }

    [JsonProperty("offer", NullValueHandling = NullValueHandling.Ignore)]
    public Offer Offer { get; set; }

    [JsonProperty("item", NullValueHandling = NullValueHandling.Ignore)]
    public EgsFreeGamesDetailItem Item { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public Data Data { get; set; }

    [JsonProperty("ageGate", NullValueHandling = NullValueHandling.Ignore)]
    public AgeGate AgeGate { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("_images_", NullValueHandling = NullValueHandling.Ignore)]
    public List<Uri> Images { get; set; }
}

public partial class AgeGate
{
    [JsonProperty("hasAgeGate")]
    public bool HasAgeGate { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }
}

public partial class Data
{
    [JsonProperty("productLinks")]
    public ExternalNavLinks ProductLinks { get; set; }

    [JsonProperty("socialLinks")]
    public SocialLinks SocialLinks { get; set; }

    [JsonProperty("requirements")]
    public Requirements Requirements { get; set; }

    [JsonProperty("footer")]
    public Footer Footer { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("about")]
    public About About { get; set; }

    [JsonProperty("banner")]
    public Banner Banner { get; set; }

    [JsonProperty("hero")]
    public Hero Hero { get; set; }

    [JsonProperty("carousel")]
    public Carousel Carousel { get; set; }

    [JsonProperty("editions")]
    public Editions Editions { get; set; }

    [JsonProperty("meta")]
    public Meta Meta { get; set; }

    [JsonProperty("markdown")]
    public ExternalNavLinks Markdown { get; set; }

    [JsonProperty("dlc")]
    public Dlc Dlc { get; set; }

    [JsonProperty("seo")]
    public Seo Seo { get; set; }

    [JsonProperty("productSections")]
    public List<ProductSection> ProductSections { get; set; }

    [JsonProperty("gallery")]
    public ExternalNavLinks Gallery { get; set; }

    [JsonProperty("navTitle")]
    public string NavTitle { get; set; }
}

public partial class About
{
    [JsonProperty("image")]
    public Image Image { get; set; }

    [JsonProperty("developerAttribution")]
    public string DeveloperAttribution { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("publisherAttribution")]
    public string PublisherAttribution { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("shortDescription")]
    public string ShortDescription { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("developerLogo")]
    public ExternalNavLinks DeveloperLogo { get; set; }
}

public partial class ExternalNavLinks
{
    [JsonProperty("_type")]
    public string Type { get; set; }
}

public partial class Image
{
    [JsonProperty("src", NullValueHandling = NullValueHandling.Ignore)]
    public Uri Src { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }
}

public partial class Banner
{
    [JsonProperty("showPromotion")]
    public bool ShowPromotion { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("link")]
    public ExternalNavLinks Link { get; set; }
}

public partial class Carousel
{
    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("items")]
    public List<ItemElement> Items { get; set; }
}

public partial class ItemElement
{
    [JsonProperty("image")]
    public Image Image { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("video")]
    public ItemVideo Video { get; set; }
}

public partial class ItemVideo
{
    [JsonProperty("recipes", NullValueHandling = NullValueHandling.Ignore)]
    public string Recipes { get; set; }

    [JsonProperty("loop")]
    public bool Loop { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("hasFullScreen")]
    public bool HasFullScreen { get; set; }

    [JsonProperty("hasControls")]
    public bool HasControls { get; set; }

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string Title { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string VideoType { get; set; }

    [JsonProperty("muted")]
    public bool Muted { get; set; }

    [JsonProperty("autoplay")]
    public bool Autoplay { get; set; }
}

public partial class Dlc
{
    [JsonProperty("contingentOffer")]
    public ContingentOffer ContingentOffer { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("enableImages")]
    public bool EnableImages { get; set; }
}

public partial class ContingentOffer
{
    [JsonProperty("regionRestrictions")]
    public ExternalNavLinks RegionRestrictions { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("hasOffer")]
    public bool HasOffer { get; set; }
}

public partial class Editions
{
    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("enableImages")]
    public bool EnableImages { get; set; }
}

public partial class Footer
{
    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("copy")]
    public string Copy { get; set; }

    [JsonProperty("privacyPolicyLink")]
    public PrivacyPolicyLink PrivacyPolicyLink { get; set; }
}

public partial class PrivacyPolicyLink
{
    [JsonProperty("src")]
    public Uri Src { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }
}

public partial class Hero
{
    [JsonProperty("logoImage")]
    public Image LogoImage { get; set; }

    [JsonProperty("portraitBackgroundImageUrl")]
    public Uri PortraitBackgroundImageUrl { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("action")]
    public ExternalNavLinks Action { get; set; }

    [JsonProperty("video")]
    public HeroVideo Video { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("isFullBleed")]
    public bool IsFullBleed { get; set; }

    [JsonProperty("altContentPosition")]
    public bool AltContentPosition { get; set; }

    [JsonProperty("backgroundImageUrl")]
    public Uri BackgroundImageUrl { get; set; }
}

public partial class HeroVideo
{
    [JsonProperty("loop")]
    public bool Loop { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("hasFullScreen")]
    public bool HasFullScreen { get; set; }

    [JsonProperty("hasControls")]
    public bool HasControls { get; set; }

    [JsonProperty("muted")]
    public bool Muted { get; set; }

    [JsonProperty("autoplay")]
    public bool Autoplay { get; set; }
}

public partial class Meta
{
    [JsonProperty("releaseDate")]
    public DateTimeOffset ReleaseDate { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("logo")]
    public ExternalNavLinks Logo { get; set; }

    [JsonProperty("platform")]
    public List<string> Platform { get; set; }

    [JsonProperty("tags")]
    public List<string> Tags { get; set; }
}

public partial class ProductSection
{
    [JsonProperty("productSection")]
    public string ProductSectionProductSection { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }
}

public partial class Requirements
{
    [JsonProperty("languages")]
    public List<string> Languages { get; set; }

    [JsonProperty("systems")]
    public List<SystemElement> Systems { get; set; }

    [JsonProperty("accountRequirements")]
    public string AccountRequirements { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("rating")]
    public ExternalNavLinks Rating { get; set; }
}

public partial class SystemElement
{
    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("systemType")]
    public string SystemType { get; set; }

    [JsonProperty("details")]
    public List<Detail> Details { get; set; }
}

public partial class Detail
{
    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("minimum")]
    public string Minimum { get; set; }

    [JsonProperty("recommended")]
    public string Recommended { get; set; }
}

public partial class Seo
{
    [JsonProperty("image")]
    public ExternalNavLinks Image { get; set; }

    [JsonProperty("twitter")]
    public ExternalNavLinks Twitter { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("og")]
    public ExternalNavLinks Og { get; set; }
}

public partial class SocialLinks
{
    [JsonProperty("linkTwitter")]
    public Uri LinkTwitter { get; set; }

    [JsonProperty("linkFacebook")]
    public Uri LinkFacebook { get; set; }

    [JsonProperty("linkYoutube")]
    public Uri LinkYoutube { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("linkHomepage")]
    public Uri LinkHomepage { get; set; }

    [JsonProperty("linkInstagram")]
    public Uri LinkInstagram { get; set; }
}

public partial class EgsFreeGamesDetailItem
{
    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("hasItem")]
    public bool HasItem { get; set; }
}

public partial class Offer
{
    [JsonProperty("regionRestrictions")]
    public ExternalNavLinks RegionRestrictions { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("namespace")]
    public string Namespace { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("hasOffer")]
    public bool HasOffer { get; set; }
}

public partial class PageTheme
{
    [JsonProperty("light")]
    public ExternalNavLinks Light { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("dark")]
    public ExternalNavLinks Dark { get; set; }
}