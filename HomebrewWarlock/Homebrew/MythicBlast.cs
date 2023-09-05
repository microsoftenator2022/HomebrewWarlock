using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Features.EldritchBlast.Components;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Homebrew
{
    internal class ChangeAbilityCommandType : UnitFactComponentDelegate
    {
        public UnitCommand.CommandType NewCommandType = UnitCommand.CommandType.Standard;
        public bool ToFullRoundAction = false;

        public UnitCommand.CommandType? RequireCommandType;

        public BlueprintAbilityReference[] Abilities = Array.Empty<BlueprintAbilityReference>();

        IEnumerable<BlueprintAbility> GetAbilityBlueprints() =>
            Abilities.Select(ability => ability.Get()).SkipIfNull();

        public override void OnTurnOn()
        {
            foreach (var ability in this.GetAbilityBlueprints())
            {
                var actionEntry = new UnitPartAbilityModifiers.ActionEntry(base.Fact, this.NewCommandType, ability)
                {
                    RequireFullRound = this.NewCommandType == UnitCommand.CommandType.Standard && ToFullRoundAction,
                    SpellCommandType = RequireCommandType
                };

                base.Owner.Ensure<UnitPartAbilityModifiers>().AddEntry(actionEntry);
            }
        }

        public override void OnTurnOff()
        {
            base.Owner.Ensure<UnitPartAbilityModifiers>().RemoveEntry(base.Fact);
        }
    }

    internal static class MythicBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Mythic Eldritch Blast";

        internal static readonly IMicroBlueprint<BlueprintBuff> BuffRef = GeneratedGuid.MythicEldritchBlastBuff.ToMicroBlueprint<BlueprintBuff>();
        internal static readonly IMicroBlueprint<BlueprintBuff> CastBuffRef = GeneratedGuid.MythicEldritchBlastCastBuff.ToMicroBlueprint<BlueprintBuff>();

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var castBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("MythicEldritchBlastCastBuff"));

            var buff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("MythicEldritchBlastBuff"))
                .Combine(castBuff)
                .Map(bps =>
                {
                    (BlueprintBuff buff, var castBuff) = bps;

                    buff.AddComponent<ChangeAbilityCommandType>(changeCT => 
                    {
                        changeCT.RequireCommandType = UnitCommand.CommandType.Standard;

                        changeCT.ToFullRoundAction = true;
                        
                        changeCT.Abilities = EldritchBlastFeatures.BlastAbilities.ToArray();
                    });

                    buff.AddAddAbilityUseTrigger(onAbilityUse =>
                    {
                        onAbilityUse.ForMultipleSpells = true;
                        onAbilityUse.Abilities = EldritchBlastFeatures.BlastAbilities.ToList();

                        onAbilityUse.Action.Add(GameActions.ContextActionApplyBuff(ab =>
                        {
                            ab.ToCaster = true;

                            ab.m_Buff = castBuff.ToReference();

                            ab.AsChild = true;
                            ab.DurationValue.Rate = DurationRate.Rounds;
                            ab.DurationValue.BonusValue = 1;
                        }));

                        onAbilityUse.OnlyOnce = true;
                    });

                    buff.AddAddAbilityUseTrigger(onAbilityUse =>
                    {
                        onAbilityUse.ForMultipleSpells = true;
                        onAbilityUse.Abilities = EldritchBlastFeatures.BlastAbilities.ToList();

                        onAbilityUse.AfterCast = true;
                    });

                    return buff;
                });
                

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("MythicEldritchBlastFeature"))
                .Combine(buff)
                .Map(bps =>
                {
                    var (feature, buff) = bps;

                    feature.m_DisplayName = LocalizedStrings.Homebrew_MythicBlast_DisplayName;

                    feature.AddComponent<AddFactContextActions>(actions =>
                    {
                        actions.Activated.Add(GameActions.ContextActionApplyBuff(ab =>
                        {
                            ab.m_Buff = buff.ToReference();
                            ab.Permanent = true;
                        }));
                        actions.Deactivated.Add(GameActions.ContextActionRemoveBuff(rb => rb.m_Buff = buff.ToReference()));
                    });

                    return feature;
                });

            return feature;
        }

        [Init]
        static void Init()
        {
            var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            Create(context)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.MythicFeatSelection)
                .Map(bps =>
                {
                    (BlueprintFeature feature, var mythicFeats) = bps;

                    mythicFeats.AddFeatures(feature);
                })
                .Register();
        }
    }
}
