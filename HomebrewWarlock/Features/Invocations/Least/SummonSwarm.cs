﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;

using MicroWrath;
using MicroWrath.BlueprintsDb;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;
using MicroWrath.GameActions;

namespace HomebrewWarlock.Features.Invocations
{
    internal static class SummonSwarm
    {
        [LocalizedString]
        internal const string DisplayName = "Summon Spider Swarm";

        [LocalizedString]
        internal static readonly string SpellDescription =
            "This spell summons a spider swarm. The summoned swarm appears where you designate and acts according to " +
            "its initiative. It attacks your opponents to the best of its ability.";

        [LocalizedString]
        internal static readonly string FeatureDescription = $"You gain {DisplayName} as the spell";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintAbility> CreateSpell(BlueprintInitializationContext context)
        {
            var unit = context.CloneBlueprint(
                BlueprintsDb.Owlcat.BlueprintUnit.CR1_SpiderSwarm,
                GeneratedGuid.Get("CR1_SummonedSpiderSwarm"),
                nameof(GeneratedGuid.CR1_SummonedSpiderSwarm))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.Unlootable))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFaction.Summoned))
                .Map(bps =>
                {
                    var (unit, unlootableBuff, summonedFaction) = bps.Expand();

                    unit.Components.OfType<Experience>().First().PlayerGainsNoExp = true;

                    unit.AddBuffOnEntityCreated(c =>
                    {
                        c.m_Buff = unlootableBuff.ToReference<BlueprintBuffReference>();
                    });

                    unit.m_Faction = summonedFaction.ToReference<BlueprintFactionReference>();

                    //unit.Visual.DismemberFx = unit.Visual.BloodPuddleFx;
                    //unit.Visual.IsNotUseDismember = false;

                    return unit;
                });

            var spell = context.CloneBlueprint(
                BlueprintsDb.Owlcat.BlueprintAbility.SummonMonsterISingle,
                GeneratedGuid.Get("SummonSwarmSpell"),
                nameof(GeneratedGuid.SummonSwarmSpell))
                .Combine(unit)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.GenericSwarmBuff))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintSpellList.ClericSpellList))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintSpellList.ShamanSpelllist))
                .Map(bps =>
                {
                    var (spell, unit, swarmBuff, clericSpellList, shamanSpellList) = bps.Expand();

                    spell.m_DisplayName = LocalizedStrings.Features_Invocations_SummonSwarm_DisplayName;
                    spell.m_Description = LocalizedStrings.Features_Invocations_SummonSwarm_SpellDescription;

                    spell.m_Icon = AssetUtils.Direct.GetSprite("4abed12203b403a47b0b32425580e5bb", 21300000);

                    var spawnAction = spell.Components
                        .OfType<AbilityEffectRunAction>()
                        .SelectMany(c => c.Actions.Actions.OfType<ContextActionSpawnMonster>())
                        .First();
                        
                    spawnAction.m_Blueprint = unit.ToReference<BlueprintUnitReference>();
                    spawnAction.AfterSpawn.Add(
                        GameActions.ContextActionRemoveBuff(a => a.m_Buff = swarmBuff.ToReference<BlueprintBuffReference>()),
                        GameActions.ContextActionApplyBuff(a =>
                        {
                            a.m_Buff = swarmBuff.ToReference<BlueprintBuffReference>();
                            a.IsNotDispelable = true;
                            a.DurationValue = spawnAction.DurationValue;
                        }));

                    var spellListComponents = spell.Components.OfType<SpellListComponent>();

                    spellListComponents.First(slc => slc.SpellList == clericSpellList).m_SpellList =
                        shamanSpellList.ToReference<BlueprintSpellListReference>();

                    foreach (var slc in spellListComponents)
                    {
                        slc.SpellLevel = 2;
                    }

                    spell.AddToSpellLists(spellListComponents.Select(slc => (slc.SpellList, slc.SpellLevel)));

                    return spell;
                });

            return spell;
        }

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateFeature(BlueprintInitializationContext context)
        {
            var ability = CreateSpell(context)
                .Map(spell =>
                {
                    var ability = AssetUtils.CloneBlueprint(spell, GeneratedGuid.Get("SummonSwarmAbility"), nameof(GeneratedGuid.SummonSwarmAbility));

                    ability.Type = AbilityType.SpellLike;
                    
                    ability.Components = ability.Components.Where(c => c is not SpellListComponent).ToArray();

                    ability.AddInvocationComponents(
                        spell.Components
                        .OfType<SpellListComponent>()
                        .Select(c => c.SpellLevel)
                        .OrderByDescending(Functional.Identity)
                        .First());

                    return ability;
                });

            return context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("SummonSwarmFeature"),
                nameof(GeneratedGuid.SummonSwarmFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_SummonSwarm_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_SummonSwarm_FeatureDescription;

                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });
        }
    }
}