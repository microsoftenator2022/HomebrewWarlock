using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Features.EldritchBlast.Components;
using HomebrewWarlock.Features.Invocations;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
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

    internal class AddDamageToBundle : UnitFactComponentDelegate, IInitiatorRulebookHandler<RulePrepareDamage>
    {
        public DamageTypeDescription DamageType = Default.DamageTypeDescription;

        public ContextDiceValue Value = Default.ContextDiceValue;

        public virtual void OnEventAboutToTrigger(RulePrepareDamage evt) { }

        public virtual void OnEventDidTrigger(RulePrepareDamage evt)
        {
            var damage = this.DamageType.CreateDamage(
                new DiceFormula(this.Value.DiceCountValue.Calculate(base.Context), this.Value.DiceType),
                this.Value.BonusValue.Calculate(base.Context));

            var fst = evt.DamageBundle.First();

            foreach (var dm in fst.Modifiers)
            {
                MicroLogger.Debug(() => $"{dm.Fact}, {dm.Value}, {dm.Descriptor}");
                damage.AddModifier(dm);
            }

            damage.CriticalModifier = fst.CriticalModifier;
            damage.CalculationType.Copy(fst.CalculationType);
            damage.AlignmentsMask = fst.AlignmentsMask;
            damage.Durability = fst.Durability;
            damage.EmpowerBonus.Copy(fst.EmpowerBonus);
            damage.BonusPercent = fst.BonusPercent;

            damage.SourceFact = base.Fact;

            evt.Add(damage);
        }
    }

    internal class AddDamageToEldritchBlast : AddDamageToBundle
    {
        public override void OnEventDidTrigger(RulePrepareDamage evt)
        {
            if (evt.ParentRule?.SourceAbility is not { } sourceBlueprint) return;

            if (!sourceBlueprint.Components.OfType<EldritchBlastComponent>().Any()) return;

            base.OnEventDidTrigger(evt);
        }
    }

    internal static class MythicBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Mythic Eldritch Blast";

        [LocalizedString]
        internal const string Description =
            "As a full-round action, you can cast an eldritch blast that deals an additional 1d6 + 1 divine damage per mythic rank.";

        internal static readonly IMicroBlueprint<BlueprintBuff> BuffRef = GeneratedGuid.MythicEldritchBlastBuff.ToMicroBlueprint<BlueprintBuff>();
        internal static readonly IMicroBlueprint<BlueprintBuff> CastBuffRef = GeneratedGuid.MythicEldritchBlastCastBuff.ToMicroBlueprint<BlueprintBuff>();

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var castBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("MythicEldritchBlastCastBuff"))
                .Map(buff =>
                {
                    buff.m_DisplayName = LocalizedStrings.Homebrew_MythicBlast_DisplayName;
                    buff.m_Description = LocalizedStrings.Homebrew_MythicBlast_Description;
                    buff.m_Icon = Sprites.EldritchBlastMythic;

                    buff.AddContextRankConfig(crc =>
                    {
                        crc.m_BaseValueType = ContextRankBaseValueType.MythicLevel;
                    });

                    buff.AddComponent<AddDamageToEldritchBlast>(c =>
                    {
                        c.DamageType.Type = DamageType.Energy;
                        c.DamageType.Energy = DamageEnergyType.Divine;

                        c.Value.DiceType = DiceType.D6;
                        c.Value.DiceCountValue.ValueType = ContextValueType.Rank;

                        c.Value.BonusValue.ValueType = ContextValueType.Rank;
                    });

                    return buff;
                });

            var buff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("MythicEldritchBlastBuff"))
                .Combine(castBuff)
                .Map(bps =>
                {
                    (BlueprintBuff buff, var castBuff) = bps;

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

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

            var toggle = context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("MythicEldritchBlastToggleAbility"))
                .Combine(buff)
                .Map(bps =>
                {
                    (BlueprintActivatableAbility toggle, var buff) = bps;

                    toggle.m_DisplayName = LocalizedStrings.Homebrew_MythicBlast_DisplayName;
                    toggle.m_Description = LocalizedStrings.Homebrew_MythicBlast_Description;
                    toggle.m_Icon = Sprites.EldritchBlastMythic;

                    toggle.m_Buff = buff.ToReference();

                    toggle.DeactivateImmediately = true;

                    return toggle;
                });
                

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("MythicEldritchBlastFeature"))
                .Combine(toggle)
                .Map(bps =>
                {
                    var (feature, toggle) = bps;

                    feature.m_DisplayName = toggle.m_DisplayName;
                    feature.m_Description = toggle.m_Description;
                    feature.m_Icon = toggle.m_Icon;

                    feature.AddAddFacts(af => af.m_Facts = new[] { toggle.ToReference<BlueprintUnitFactReference>() });

                    //feature.AddComponent<AddFactContextActions>(actions =>
                    //{
                    //    actions.Activated.Add(GameActions.ContextActionApplyBuff(ab =>
                    //    {
                    //        ab.m_Buff = buff.ToReference();
                    //        ab.Permanent = true;
                    //    }));
                    //    actions.Deactivated.Add(GameActions.ContextActionRemoveBuff(rb => rb.m_Buff = buff.ToReference()));
                    //});

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
