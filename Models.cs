using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace ShAK12Sys;

/// <summary>
///     One file inside db/nerv_inv. Mirrors the original SPT 3.x JSON so the data files
///     shipped with the mod stay untouched.
/// </summary>
public sealed class InjectionFile
{
    [JsonPropertyName("overwriteProperties")]
    public OverwriteProperties? OverwriteProperties { get; set; }

    [JsonPropertyName("slotsToAdd")]
    public SlotsToAdd? SlotsToAdd { get; set; }

    [JsonPropertyName("traderToAdd")]
    public TraderToAdd? TraderToAdd { get; set; }

    [JsonPropertyName("chamberToAdd")]
    public ChamberToAdd? ChamberToAdd { get; set; }

    // Original mod spells this "catridgeToAdd"; keep the file format as-is.
    [JsonPropertyName("catridgeToAdd")]
    public CartridgeToAdd? CartridgeToAdd { get; set; }
}

public sealed class OverwriteProperties
{
    [JsonPropertyName("itemTplToClone")]
    public MongoId ItemTplToClone { get; set; }

    // Deserialized directly into the SPT 4.x model, whose JSON names still match
    // the EFT item format (_props, Prefab, Slots, ...).
    [JsonPropertyName("overrideProperties")]
    public TemplateItemProperties? OverrideProperties { get; set; }

    [JsonPropertyName("parentId")]
    public MongoId ParentId { get; set; }

    [JsonPropertyName("newId")]
    public MongoId NewId { get; set; }

    [JsonPropertyName("fleaPriceRoubles")]
    public double? FleaPriceRoubles { get; set; }

    [JsonPropertyName("handbookPriceRoubles")]
    public double? HandbookPriceRoubles { get; set; }

    [JsonPropertyName("handbookParentId")]
    public MongoId HandbookParentId { get; set; }

    [JsonPropertyName("locales")]
    public Dictionary<string, LocaleDetails>? Locales { get; set; }
}

public sealed class SlotsToAdd
{
    [JsonPropertyName("targetID")]
    public List<MongoId> TargetIds { get; set; } = [];

    [JsonPropertyName("slotName")]
    public List<string> SlotNames { get; set; } = [];
}

public sealed class TraderToAdd
{
    [JsonPropertyName("traderID")]
    public MongoId TraderId { get; set; }

    [JsonPropertyName("loyal_level_items")]
    public int LoyalLevelItems { get; set; }

    [JsonPropertyName("barter_scheme")]
    public string BarterScheme { get; set; } = "ROUBLES";

    [JsonPropertyName("barter_scheme_value")]
    public double BarterSchemeValue { get; set; }

    [JsonPropertyName("BuyRestrictionMax")]
    public int BuyRestrictionMax { get; set; }
}

public sealed class ChamberToAdd
{
    [JsonPropertyName("weaponID")]
    public List<MongoId> WeaponIds { get; set; } = [];
}

public sealed class CartridgeToAdd
{
    [JsonPropertyName("magazineID")]
    public List<MongoId> MagazineIds { get; set; } = [];
}

/// <summary>
///     db/locales/global/{language}.json
/// </summary>
public sealed class LocaleFile
{
    [JsonPropertyName("itemids")]
    public Dictionary<MongoId, Dictionary<string, string>> ItemIds { get; set; } = [];
}
