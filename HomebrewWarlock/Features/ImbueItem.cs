using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Rest;
using Kingmaker.Craft;
using Kingmaker.Designers;
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
        static IEnumerable<BlueprintSpellbook> GetClassSpellbooks(bool includeMythic = false)
        {
            foreach (var c in BlueprintRoot.Instance.Progression.CharacterClasses.Where(c => !c.IsMythic || includeMythic))
            {
                if (c.Spellbook is not null) yield return c.Spellbook;

                foreach (var a in c.Archetypes)
                {
                    if (a.Spellbook is not null) yield return a.Spellbook;
                }
            }
        }

        static readonly Lazy<IEnumerable<BlueprintSpellbook>> ClassSpellbooks = new(() => GetClassSpellbooks().ToArray());

        static bool SpellListIsArcaneCaster(BlueprintSpellList? spellList, bool includeMythic = false)
        {
            if (spellList is null) return false;

            foreach (var sb in includeMythic ? GetClassSpellbooks(includeMythic) : ClassSpellbooks.Value)
            {
                if (sb.IsArcane && sb.SpellList == spellList)
                    return true;
            }

            return false;
        }

        record class CraftableSpell(int SpellLevel, bool IsArcane, BlueprintAbility Spell);
        static IEnumerable<CraftableSpell> GetAllCraftableSpells(bool arcane)
        {
            var craftRoot = BlueprintRoot.Instance.CraftRoot;
            var items = craftRoot.m_ScrollsItems.Concat(craftRoot.m_PotionsItems).Select(item => item.Get());

            foreach (var item in items)
            {
                var spellLists = item.Ability.GetComponents<SpellListComponent>()
                    .OrderBy(slc => slc.SpellLevel)
                    .Select(slc => (slc, isArcane: SpellListIsArcaneCaster(slc.SpellList)));

                var arcaneLists = spellLists.Where(slc => slc.isArcane);
                var divineLists = spellLists.Where(slc => !slc.isArcane);

                if (arcaneLists.Any() && arcane)
                {
                    var (slc, _) = arcaneLists.First();

                    MicroLogger.Debug(() => $"{item.NameSafe()}: Adding from {slc.m_SpellList.Get().NameSafe()} {slc.SpellLevel} (Arcane)");
                    yield return new(slc.SpellLevel, true, item.Ability);
                }
                else if (divineLists.Any() && !arcane)
                {
                    var (slc, _) = divineLists.First();

                    MicroLogger.Debug(() => $"{item.NameSafe()}: Adding from {slc.m_SpellList.Get().NameSafe()} {slc.SpellLevel} (Divine)");
                    yield return new(slc.SpellLevel, false, item.Ability);
                }
            }
        }

        static BlueprintSpellList AddAllCraftableSpells(BlueprintSpellList spellList, bool isArcane)
        {
            var spells = GetAllCraftableSpells(isArcane).ToArray();

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
                public new int BaseLevel => unitPart.Owner.Progression.CharacterLevel;

                [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.BaseLevel), MethodType.Getter)]
                [HarmonyPostfix]
                static int BaseLevel_Patch(int value, Spellbook __instance)
                {
                    if (__instance is CraftSpellbook cs)
                        return cs.BaseLevel;

                    return value;
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

                    if (arcane is null)
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

                    if (divine is null)
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

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
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
                    AddAllCraftableSpells(spellList, true);

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
                    AddAllCraftableSpells(spellList, false);

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

                if (!new StackTrace().GetFrames().Any(frame =>
                    frame.GetMethod().DeclaringType is { } t &&
                    (t.Namespace.StartsWith("Kingmaker.Craft") ||
                    t.Namespace.StartsWith("Kingmaker.Controllers.Rest") ||
                    t.Namespace.StartsWith("Kingmaker.UI.MVVM._VM.Rest") ||
                    t.Namespace.StartsWith("Kingmaker.UI.MVVM._PCView.Rest") ||
                    t.Namespace.StartsWith("Kingmaker.UI.MVVM._ConsoleView.Rest"))
                ))
                    yield break;

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

            //[HarmonyPatch(typeof(CraftRoot), nameof(CraftRoot.TryFindAbilityInSpellbooks))]
            //[HarmonyPostfix]
            //static void CraftRoot_TryFindAbilityInSpellbooks_Postfix(UnitEntityData crafter, BlueprintAbility abillity, ref Spellbook spellbook)
            //{
            //    if (crafter.Parts.Parts.OfType<UnitPartImbueItem>().FirstOrDefault() is not { } part)
            //    {
            //        return;
            //    }

            //    if (spellbook == null)
            //    {
            //        if (part.DivineSpellbook is not null && part.DivineSpellbook.IsKnown(abillity))
            //        {
            //            spellbook = part.DivineSpellbook;
            //        }

            //        if (part.ArcaneSpellbook is not null && part.ArcaneSpellbook.IsKnown(abillity))
            //        {
            //            spellbook = part.ArcaneSpellbook;
            //        }
            //    }
            //}
        }
    }
}
