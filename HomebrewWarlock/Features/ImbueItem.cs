using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Resources;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Rest;
using Kingmaker.Craft;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features
{
    internal static class ImbueItem
    {
        static IEnumerable<BlueprintSpellbook> GetClassSpellbooks(bool mythic = false)
        {
            foreach (var c in BlueprintRoot.Instance.Progression.CharacterClasses.Where(c => mythic == c.IsMythic))
            {
                if (c.Spellbook is not null) yield return c.Spellbook;

                foreach (var a in c.Archetypes)
                {
                    if (a.Spellbook is not null) yield return a.Spellbook;
                }
            }
        }

        static readonly Lazy<IEnumerable<BlueprintSpellbook>> ClassSpellbooks = new(() => GetClassSpellbooks(false).ToArray());
        static readonly Lazy<IEnumerable<BlueprintSpellbook>> MythicClassSpellbooks = new(() => GetClassSpellbooks(true).ToArray());

        static IEnumerable<BlueprintSpellbook> GetSpellbooks(BlueprintSpellList? spellList, bool includeMythic = false)
        {
            if (spellList is null)
                return Array.Empty<BlueprintSpellbook>();

            return ClassSpellbooks.Value
                .Concat(includeMythic ? MythicClassSpellbooks.Value : Array.Empty<BlueprintSpellbook>())
                .Where(sb => sb.SpellList == spellList);
        }

        static bool SpellListIsArcaneCaster(BlueprintSpellList? spellList, bool includeMythic = false) =>
            GetSpellbooks(spellList, includeMythic).Any(sb => sb.IsArcane);

        record class CraftableSpell(int SpellLevel, bool IsArcane, BlueprintAbility Spell);
        static IEnumerable<CraftableSpell> GetAllCraftableSpells(bool arcane, bool debugLog = false)
        {
            var craftRoot = BlueprintRoot.Instance.CraftRoot;
            var items = craftRoot.m_ScrollsItems.Concat(craftRoot.m_PotionsItems)
                .Select(item => item.Get())
                .SkipIfNull()
                .Where(item => item.Ability.GetComponent<CraftInfoComponent>() is not null);

            foreach (var item in items)
            {
                var spellLists = item.Ability.GetComponents<SpellListComponent>()
                    .OrderBy(slc => slc.SpellLevel)
                    .Select(slc => (slc, isArcane: SpellListIsArcaneCaster(slc.SpellList)));

                var arcaneLists = spellLists.Where(slc => slc.isArcane);
                var divineLists = spellLists.Where(slc => !slc.isArcane);

                if (arcane && arcaneLists.Any())
                {
                    var (slc, _) = arcaneLists.First();

                    MicroLogger.Debug(() => $"{item.NameSafe()}: Adding from {slc.m_SpellList.Get().NameSafe()} {slc.SpellLevel} (Arcane)");
                    yield return new(slc.SpellLevel, true, item.Ability);
                }
                else if (divineLists.Any())
                {
                    var (slc, _) = divineLists.First();

                    MicroLogger.Debug(() => $"{item.NameSafe()}: Adding from {slc.m_SpellList.Get().NameSafe()} {slc.SpellLevel} (Divine)");
                    yield return new(slc.SpellLevel, false, item.Ability);
                    
                }

                if (debugLog)
                    MicroLogger.Debug(sb =>
                    {
                        var spellListsBooks = spellLists
                            .Select(slc =>
                                (spellList: slc.slc.SpellList,
                                spellLevel: slc.slc.SpellLevel,
                                GetSpellbooks(slc.slc.SpellList)));

                        sb.AppendLine($"Spell: {item.Ability.NameSafe()}");
                        sb.Append($"  {spellListsBooks.Count()} Spell lists:");

                        foreach (var (list, level, books) in spellListsBooks)
                        {
                            sb.AppendLine();
                            sb.Append($"    {list.NameSafe()} {level}:");

                            foreach (var book in books)
                            {
                                var arcane = book.IsArcane;
                                var arcaneCaster = book.CharacterClass is not null && book.CharacterClass.IsArcaneCaster;
                                var divine = book.CharacterClass is not null && book.CharacterClass.IsDivineCaster;

                                sb.AppendLine();
                                sb.Append($"      {book.name} {(arcane ? "(Arcane spellbook) " : "")}{(arcaneCaster ? "(Arcane caster) " : "")}{(divine ? "(Divine caster)" : "")}");
                            }
                        }
                    });
            }
        }

        static BlueprintSpellList AddAllCraftableSpells(BlueprintSpellList spellList, bool isArcane, bool debugLog = false)
        {
            var spells = GetAllCraftableSpells(isArcane, debugLog).ToArray();

            var spellsByLevel = new List<BlueprintAbilityReference>[10];

            for (var i = 0; i <= 9; i++)
            {
                spellsByLevel[i] = new();

                foreach (var s in spells.Where(s => s.SpellLevel == i))
                {
                    spellsByLevel[i].Add(s.Spell.ToReference<BlueprintAbilityReference>());
                }

                //MicroLogger.Debug(sb =>
                //{
                //    sb.Append($"{(isArcane ? "Arcane" : "Divine")} {i} spells:");

                //    foreach (var s in spellsByLevel[i])
                //    {
                //        sb.AppendLine();
                //        sb.Append($"{s.NameSafe()}");
                //    }
                //});
            }

            spellList.SpellsByLevel = spellsByLevel.Indexed().Select(ss => new SpellLevelList(ss.index) { m_Spells = ss.item }).ToArray();

            return spellList;
        }

        internal class UnitPartImbueItem : UnitPart
        {
            [HarmonyPatch]
            internal class CraftSpellbook(UnitDescriptor owner, BlueprintSpellbook blueprint, UnitPartImbueItem unitPart) : Spellbook(owner, blueprint)
            {
                public new int BaseLevel => base.m_BaseLevelInternal = unitPart.Owner.Progression.CharacterLevel;

                [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.BaseLevel), MethodType.Getter)]
                [HarmonyPostfix]
                static int BaseLevel_Patch(int value, Spellbook __instance)
                {
                    if (__instance is CraftSpellbook cs)
                        return cs.BaseLevel;

                    return value;
                }

                [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetSpellsPerDay))]
                [HarmonyPostfix]
                public static int GetSpellsPerDay_Postfix(int num, Spellbook __instance)
                {
                    if (__instance is CraftSpellbook)
                        return 0;

                    return num;
                }
            }

            private ImbueItemComponent? GetImbueItemComponent() =>
                base.Owner.Facts.List.Select(f => f.GetComponent<ImbueItemComponent>()).FirstOrDefault(c => c is not null);

            private Spellbook? arcane;
            public Spellbook? ArcaneSpellbook
            {
                get
                {
                    var component = GetImbueItemComponent();
                    if (component is null)
                    {
                        this.RemoveSelf();
                        return null;
                    }

                    if (arcane as CraftSpellbook is null)
                    {
                        var bp = component.ArcaneSpellbook?.Get();
                        if (bp is null) return null;

                        arcane = new CraftSpellbook(base.Owner, bp, this);
                    }

                    //arcane.TryRestoreKnownSpells();

                    foreach (var spells in arcane.Blueprint.SpellList.SpellsByLevel)
                    {
                        foreach (var spell in spells.Spells)
                        {
                            if (!arcane.IsKnownOnLevel(spell, spells.SpellLevel))
                            {
                                arcane.AddKnown(spells.SpellLevel, spell, false);
                            }
                        }
                    }

                    return arcane;
                }
            }

            private Spellbook? divine;
            public Spellbook? DivineSpellbook
            {
                get
                {
                    var component = GetImbueItemComponent();
                    if (component is null)
                    {
                        this.RemoveSelf();
                        return null;
                    }

                    if (divine as CraftSpellbook is null)
                    {
                        var bp = component.DivineSpellbook?.Get();
                        if (bp is null) return null;
                        divine = new CraftSpellbook(base.Owner, bp, this);
                    }

                    //divine.TryRestoreKnownSpells();

                    foreach (var spells in divine.Blueprint.SpellList.SpellsByLevel)
                    {
                        foreach (var spell in spells.Spells)
                        {
                            if (!divine.IsKnownOnLevel(spell, spells.SpellLevel))
                            {
                                divine.AddKnown(spells.SpellLevel, spell, false);
                            }
                        }
                    }

                    return divine;
                }
            }

            public bool KnowsSpell(BlueprintAbility spell) =>
                Owner.Spellbooks.Where(sb => sb is not CraftSpellbook && sb.GetKnownSpells(spell).Any()).Any();
        }

        internal class ImbueItemComponent : UnitFactComponentDelegate
        {
            public BlueprintSpellbookReference ArcaneSpellbook = null!;
            public BlueprintSpellbookReference DivineSpellbook = null!;

            public override void OnActivate()
            {
                base.OnActivate();

                base.Owner.Ensure<UnitPartImbueItem>();
            }

            public override void OnDeactivate()
            {
                base.OnDeactivate();

                base.Owner.Remove<UnitPartImbueItem>();
            }
        }

        [LocalizedString]
        internal const string FeatureDisplayName = "Imbue Item";

        [LocalizedString]
        internal const string ArcaneSpellbookDisplayName = "Imbue Item (Arcane)";

        [LocalizedString]
        internal const string DivineSpellbookDisplayName = "Imbue Item (Divine)";

        [LocalizedString]
        internal static readonly string Description =
            "A warlock of 12th level or higher can use his supernatural power to create magic items, even if he " +
            "does not know the spells required to make an item (although he must know the appropriate item creation " +
            "feat). He can substitute a Use Magic Device check (DC 15 + spell level for arcane spells or 25 + spell " +
            "level for divine spells) in place of a required spell he doesn't know or can't cast." +
            Environment.NewLine +
            "If the check succeeds, the warlock can create the item as if he had cast the required spell. If it " +
            "fails, he cannot complete the item. He does not expend any resources for making the item; his " +
            "progress is simply arrested.";
            //"He cannot retry this Use Magic Device check for that spell until he gains a new level.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context)
        {
            bool debugLog =
#if DEBUG
                true;
#else
                false;
#endif

            var table = context.NewBlueprint<BlueprintSpellsTable>(
                GeneratedGuid.Get("ImbueItemSpellsTable"),
                nameof(GeneratedGuid.ImbueItemSpellsTable))
                .Map(table =>
                {
                    table.Levels = new SpellsLevelEntry[21];
                    
                    for (var i = 0; i < table.Levels.Length; i++)
                    {
                        table.Levels[i] = new() { Count = new int[10] };

                        for (var j = 0; j < 10; j++)
                        {
                            table.Levels[i].Count[j] = 0;
                        }
                    }

                    return table;
                });

            var arcaneSpellList = context.NewBlueprint<BlueprintSpellList>(
                GeneratedGuid.Get("ImbueItemArcaneSpellList"),
                nameof(GeneratedGuid.ImbueItemArcaneSpellList))
                .Map(spellList =>
                {
                    AddAllCraftableSpells(spellList, true, debugLog);

                    return spellList;
                });

            var arcaneSpellbook = context.NewBlueprint<BlueprintSpellbook>(
                GeneratedGuid.Get("ImbueItemArcaneSpellbook"),
                nameof(GeneratedGuid.ImbueItemArcaneSpellbook))
                .Combine(arcaneSpellList)
                .Combine(table)
                .Map(bps =>
                {
                    var (spellbook, spellList, table) = bps.Expand();

                    spellbook.Name = LocalizedStrings.Features_ImbueItem_ArcaneSpellbookDisplayName;

                    spellbook.m_CharacterClass = WarlockClass.Blueprint.ToReference<BlueprintCharacterClass, BlueprintCharacterClassReference>();

                    spellbook.AllSpellsKnown = true;
                    spellbook.CastingAttribute = StatType.Charisma;
                    spellbook.IsArcane = true;
                    //spellbook.Spontaneous = true;
                    spellbook.m_SpellList = spellList.ToReference<BlueprintSpellListReference>();
                    spellbook.m_SpellsPerDay = table.ToReference<BlueprintSpellsTableReference>();

                    return spellbook;
                });

            var divineSpellList = context.NewBlueprint<BlueprintSpellList>(
                GeneratedGuid.Get("ImbueItemDivineSpellList"),
                nameof(GeneratedGuid.ImbueItemDivineSpellList))
                .Map(spellList =>
                {
                    AddAllCraftableSpells(spellList, false, debugLog);

                    return spellList;
                });

            var divineSpellbook = context.NewBlueprint<BlueprintSpellbook>(
                GeneratedGuid.Get("ImbueItemDivineSpellbook"),
                nameof(GeneratedGuid.ImbueItemDivineSpellbook))
                .Combine(divineSpellList)
                .Combine(table)
                .Map(bps =>
                {
                    var (spellbook, spellList, table) = bps.Expand();

                    spellbook.Name = LocalizedStrings.Features_ImbueItem_DivineSpellbookDisplayName;

                    spellbook.m_CharacterClass = WarlockClass.Blueprint.ToReference<BlueprintCharacterClass, BlueprintCharacterClassReference>();

                    spellbook.AllSpellsKnown = true;
                    spellbook.CastingAttribute = StatType.Charisma;
                    spellbook.IsArcane = false;
                    //spellbook.Spontaneous = true;
                    spellbook.m_SpellList = spellList.ToReference<BlueprintSpellListReference>();
                    spellbook.m_SpellsPerDay = table.ToReference<BlueprintSpellsTableReference>();

                    return spellbook;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("ImbueItemFeature"),
                nameof(GeneratedGuid.ImbueItemFeature))
                .Combine(arcaneSpellbook)
                .Combine(divineSpellbook)
                .Map(bps =>
                {
                    var (feature, arcane, divine) = bps.Expand();

                    feature.m_DisplayName = LocalizedStrings.Features_ImbueItem_FeatureDisplayName;
                    feature.m_Description = LocalizedStrings.Features_ImbueItem_Description;
                    feature.m_Icon = Sprites.InfusedCurative;

                    feature.AddComponent<ImbueItemComponent>(c =>
                    {
                        c.ArcaneSpellbook = arcane.ToReference<BlueprintSpellbookReference>();
                        c.DivineSpellbook = divine.ToReference<BlueprintSpellbookReference>();
                    });

                    return feature;
                });

            return feature;
        }

        [HarmonyPatch]
        static class Patches
        {
            [HarmonyPatch(typeof(UnitDescriptor), nameof(UnitDescriptor.Spellbooks), MethodType.Getter)]
            [HarmonyPostfix]
            static IEnumerable<Spellbook> UnitDescriptorSpellbooks_Postfix(IEnumerable<Spellbook> result, UnitDescriptor __instance)
            {
                foreach (var sb in result)
                    yield return sb;

                if (!__instance.Unit.Parts.Parts.OfType<UnitPartImbueItem>().Any())
                    yield break;
                
                #if !DEBUG
                if (!new StackTrace().GetFrames().Any(frame =>
                    frame.GetMethod().DeclaringType is { } t &&
                    (t.Namespace.StartsWith("Kingmaker.Craft") ||
                    t.Namespace.StartsWith("Kingmaker.Controllers.Rest") ||
                    t.Namespace.StartsWith("Kingmaker.UI.MVVM._VM.Rest") ||
                    t.Namespace.StartsWith("Kingmaker.UI.MVVM._PCView.Rest") ||
                    t.Namespace.StartsWith("Kingmaker.UI.MVVM._ConsoleView.Rest"))
                ))
                    yield break;
                #endif

                if (__instance.Unit.Get<UnitPartImbueItem>().ArcaneSpellbook is { } asb)
                    yield return asb;

                if (__instance.Unit.Get<UnitPartImbueItem>().DivineSpellbook is { } dsb)
                    yield return dsb;
            }

            public static RuleSkillCheck ImbueItemCheck(RuleSkillCheck result, CraftSlotState css)
            {
                if (css.CurrentCraft is null || Game.Instance.Player.CraftManager.Disabled)
                    return result;

                var campingRole = Game.Instance.Player.Camping.CurrentCampingRoles[css.m_CampingRoleType];

                if (campingRole is null || campingRole.PrimaryUnit.Value is null)
                    return result;

                if (campingRole.PrimaryUnit.Value.Parts.Parts.OfType<UnitPartImbueItem>().FirstOrDefault() is { } up &&
                    up.KnowsSpell(css.CurrentCraft.CraftInfo.Item.Ability))
                    return result;

                var dc = (css.CurrentCraft.CraftInfo.IsArcane ? 15 : 25) + css.CurrentCraft.CraftInfo.SpellLevel;

                var rule = new RuleSkillCheck(campingRole.PrimaryUnit.Value, StatType.SkillUseMagicDevice, dc);
                rule = GameHelper.TriggerSkillCheck(rule);

                if (rule.Success)
                    return result;

                return rule;
            }

            [HarmonyPatch(typeof(CraftSlotState), nameof(CraftSlotState.SetCheckResult))]
            [HarmonyPostfix]
            static void CraftSlotState_SetCheckResult_Postfix(CraftSlotState __instance, RestStatus status)
            {
                var craftStatus = status.CurrentIteration.GetCraftStatus(__instance.m_CampingRoleType);

                if (craftStatus.Check is null)
                    return;

                craftStatus.SetCheckResult(ImbueItemCheck(craftStatus.Check, __instance));
            }

            [HarmonyPatch(typeof(CraftRoot), nameof(CraftRoot.TryFindAbilityInSpellbooks))]
            [HarmonyPostfix]
            static void CraftRoot_TryFindAbilityInSpellbooks_Postfix(UnitEntityData crafter, BlueprintAbility abillity, ref Spellbook spellbook)
            {
                if (crafter.Parts.Parts.OfType<UnitPartImbueItem>().FirstOrDefault() is not { } part)
                {
                    return;
                }

                if (!part.KnowsSpell(abillity))
                {
                    if (part.DivineSpellbook is not null && part.DivineSpellbook.IsKnown(abillity))
                    {
                        spellbook = part.DivineSpellbook;
                    }
                    else if (part.ArcaneSpellbook is not null && part.ArcaneSpellbook.IsKnown(abillity))
                    {
                        spellbook = part.ArcaneSpellbook;
                    }
                }
            }
        }
    }
}
