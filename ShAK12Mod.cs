using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace ShAK12Sys;

/// <summary>
///     Adds the ASh-12 full-size suppressor and carry handle, the MTs-558 muzzle
///     device/handguard and the 12.7x55 PS12 Sabot round.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.Preload)]
public sealed class ShAK12Mod(
    ISptLogger<ShAK12Mod> logger,
    JsonUtil jsonUtil,
    CustomItemService customItemService,
    TemplateTable templateTable,
    TradersTable tradersTable,
    LocaleTable localeTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(modDirectory))
        {
            logger.Error("[ShAK-12-Sys] Unable to resolve the mod directory, aborting.");
            return Task.CompletedTask;
        }

        var locales = LoadLocales(modDirectory, cancellationToken);
        ApplyLocaleTransformers(locales);

        var itemsDirectory = Path.Combine(modDirectory, "db", "nerv_inv");
        if (!Directory.Exists(itemsDirectory))
        {
            logger.Error($"[ShAK-12-Sys] Missing item data directory: {itemsDirectory}");
            return Task.CompletedTask;
        }

        var loaded = 0;
        foreach (var file in Directory.GetFiles(itemsDirectory, "*.json").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var injection = jsonUtil.Deserialize<InjectionFile>(File.ReadAllText(file));
                if (injection?.OverwriteProperties?.OverrideProperties is null)
                {
                    logger.Warning($"[ShAK-12-Sys] Skipping invalid injection file: {Path.GetFileName(file)}");
                    continue;
                }

                ProcessInjection(injection, Path.GetFileNameWithoutExtension(file), locales);
                loaded++;
            }
            catch (Exception exception)
            {
                logger.Error($"[ShAK-12-Sys] Failed to process {Path.GetFileName(file)}: {exception.Message}", exception);
            }
        }

        logger.Success($"[ShAK-12-Sys] Registered {loaded} custom item(s).");
        return Task.CompletedTask;
    }

    private void ProcessInjection(InjectionFile injection, string itemName, Dictionary<string, LocaleFile> locales)
    {
        var config = injection.OverwriteProperties!;
        var newId = config.NewId;

        if (templateTable.Items.ContainsKey(newId))
        {
            logger.Warning($"[ShAK-12-Sys] Item {newId} already exists in the database, skipping {itemName}.");
            return;
        }

        // TemplateItemProperties is a record with a lazily-created ExtensionData dictionary
        // and a non-nullable PlayFuzeSound bool. Both would otherwise be copied onto the
        // clone by CustomItemService and could wipe base-item data, so neutralise them:
        // only values that were actually present in the JSON should be applied.
        var overrides = config.OverrideProperties!;
        overrides.ExtensionData = null;
        if (templateTable.Items.TryGetValue(config.ItemTplToClone, out var itemToClone) && itemToClone.Properties is not null)
        {
            overrides.PlayFuzeSound = itemToClone.Properties.PlayFuzeSound;
        }

        var result = customItemService.CreateItemFromClone(
            new NewItemFromCloneDetails
            {
                ItemTplToClone = config.ItemTplToClone,
                OverrideProperties = overrides,
                ParentId = config.ParentId,
                NewId = newId,
                NewItemName = $"shak12_{itemName}",
                FleaPriceRoubles = config.FleaPriceRoubles,
                HandbookPriceRoubles = config.HandbookPriceRoubles,
                HandbookParentId = config.HandbookParentId,
                AddToHandbook = true,
                // The old mod defined flea prices; registering them also keeps the
                // items out of the PMC loot blacklist.
                AddToFleaPriceDb = config.FleaPriceRoubles is > 0,
                Locales = BuildLocales(newId, locales)
            },
            Assembly.GetExecutingAssembly()
        );

        if (!result.Success)
        {
            logger.Error($"[ShAK-12-Sys] Could not create {itemName} ({newId}): {string.Join(", ", result.Errors ?? [])}");
            return;
        }

        if (injection.SlotsToAdd is not null)
        {
            var count = Math.Min(injection.SlotsToAdd.TargetIds.Count, injection.SlotsToAdd.SlotNames.Count);
            for (var i = 0; i < count; i++)
            {
                AddToSlot(injection.SlotsToAdd.TargetIds[i], injection.SlotsToAdd.SlotNames[i], newId);
            }
        }

        if (injection.ChamberToAdd is not null)
        {
            foreach (var weaponId in injection.ChamberToAdd.WeaponIds)
            {
                AddToChamber(weaponId, newId);
            }
        }

        if (injection.CartridgeToAdd is not null)
        {
            foreach (var magazineId in injection.CartridgeToAdd.MagazineIds)
            {
                AddToCartridge(magazineId, newId);
            }
        }

        if (injection.TraderToAdd is not null)
        {
            AddTraderOffer(injection.TraderToAdd, newId);
        }
    }

    private Dictionary<string, LocaleFile> LoadLocales(string modDirectory, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, LocaleFile>(StringComparer.OrdinalIgnoreCase);
        var localesDirectory = Path.Combine(modDirectory, "db", "locales", "global");
        if (!Directory.Exists(localesDirectory))
        {
            return result;
        }

        foreach (var file in Directory.GetFiles(localesDirectory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var locale = jsonUtil.Deserialize<LocaleFile>(File.ReadAllText(file));
            if (locale is not null)
            {
                result[Path.GetFileNameWithoutExtension(file)] = locale;
            }
        }

        return result;
    }

    private void ApplyLocaleTransformers(Dictionary<string, LocaleFile> locales)
    {
        foreach (var (language, localeFile) in locales)
        {
            if (!localeTable.Global.TryGetValue(language, out var lazyLocale))
            {
                logger.Warning($"[ShAK-12-Sys] Locale '{language}' is not available in this SPT install, skipping.");
                continue;
            }

            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData is null)
                {
                    return null;
                }

                foreach (var (itemId, fields) in localeFile.ItemIds)
                {
                    foreach (var (field, value) in fields)
                    {
                        localeData[$"{itemId} {field}"] = value;
                    }
                }

                return localeData;
            });
        }
    }

    private Dictionary<string, LocaleDetails> BuildLocales(MongoId newId, Dictionary<string, LocaleFile> locales)
    {
        var result = new Dictionary<string, LocaleDetails>(StringComparer.OrdinalIgnoreCase);
        foreach (var (language, localeFile) in locales)
        {
            if (!localeFile.ItemIds.TryGetValue(newId, out var fields))
            {
                continue;
            }

            fields.TryGetValue("Name", out var name);
            if (name is null)
            {
                continue;
            }

            fields.TryGetValue("ShortName", out var shortName);
            fields.TryGetValue("Description", out var description);

            result[language] = new LocaleDetails
            {
                Name = name,
                ShortName = shortName ?? string.Empty,
                Description = description ?? string.Empty
            };
        }

        if (result.Count == 0)
        {
            result["en"] = new LocaleDetails
            {
                Name = newId.ToString(),
                ShortName = newId.ToString(),
                Description = string.Empty
            };
        }

        return result;
    }

    private void AddToSlot(MongoId targetId, string slotName, MongoId newItemId)
    {
        if (!templateTable.Items.TryGetValue(targetId, out var target))
        {
            logger.Warning($"[ShAK-12-Sys] Cannot add {newItemId} to slot '{slotName}': item {targetId} was not found.");
            return;
        }

        var slot = target.Properties?.Slots?.FirstOrDefault(candidate => candidate.Name == slotName);
        var filter = slot?.Properties?.Filters?.FirstOrDefault()?.Filter;
        if (filter is null)
        {
            logger.Warning($"[ShAK-12-Sys] Item {targetId} has no usable '{slotName}' slot filter, skipping.");
            return;
        }

        filter.Add(newItemId);
    }

    private void AddToChamber(MongoId weaponId, MongoId newItemId)
    {
        if (!templateTable.Items.TryGetValue(weaponId, out var weapon))
        {
            logger.Warning($"[ShAK-12-Sys] Cannot add {newItemId} to chamber: weapon {weaponId} was not found.");
            return;
        }

        var filter = weapon.Properties?.Chambers?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter;
        if (filter is null)
        {
            logger.Warning($"[ShAK-12-Sys] Weapon {weaponId} has no usable chamber filter, skipping.");
            return;
        }

        filter.Add(newItemId);
    }

    private void AddToCartridge(MongoId magazineId, MongoId newItemId)
    {
        if (!templateTable.Items.TryGetValue(magazineId, out var magazine))
        {
            logger.Warning($"[ShAK-12-Sys] Cannot add {newItemId} to magazine: magazine {magazineId} was not found.");
            return;
        }

        var filter = magazine.Properties?.Cartridges?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter;
        if (filter is null)
        {
            logger.Warning($"[ShAK-12-Sys] Magazine {magazineId} has no usable cartridge filter, skipping.");
            return;
        }

        filter.Add(newItemId);
    }

    private void AddTraderOffer(TraderToAdd config, MongoId newItemId)
    {
        if (!tradersTable.TryGetValue(config.TraderId, out var trader))
        {
            logger.Warning($"[ShAK-12-Sys] Trader {config.TraderId} was not found, skipping offer for {newItemId}.");
            return;
        }

        var assort = trader.Assort;
        if (assort is null)
        {
            logger.Warning($"[ShAK-12-Sys] Trader {config.TraderId} has no assort, skipping offer for {newItemId}.");
            return;
        }

        assort.Items ??= [];
        assort.BarterScheme ??= [];
        assort.LoyalLevelItems ??= [];

        if (assort.Items.All(item => item.Id != newItemId))
        {
            assort.Items.Add(new Item
            {
                Id = newItemId,
                Template = newItemId,
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd
                {
                    UnlimitedCount = false,
                    StackObjectsCount = config.BuyRestrictionMax * 11,
                    BuyRestrictionMax = config.BuyRestrictionMax,
                    BuyRestrictionCurrent = 0
                }
            });
        }

        var currency = config.BarterScheme.ToUpperInvariant() switch
        {
            "DOLLARS" => ItemTpl.MONEY_DOLLARS,
            "EUROS" => ItemTpl.MONEY_EUROS,
            _ => ItemTpl.MONEY_ROUBLES
        };

        assort.BarterScheme[newItemId] =
        [
            [
                new BarterScheme
                {
                    Count = config.BarterSchemeValue,
                    Template = currency
                }
            ]
        ];
        assort.LoyalLevelItems[newItemId] = config.LoyalLevelItems;

        logger.Info($"[ShAK-12-Sys] Added {newItemId} to trader {config.TraderId} (loyalty {config.LoyalLevelItems}, {config.BarterSchemeValue} {config.BarterScheme}).");
    }
}
