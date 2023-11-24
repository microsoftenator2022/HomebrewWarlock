using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features
{
    internal static class HellfireBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Hellfire Blast";

        [LocalizedString]
        internal const string Description =
            "Whenever you use your eldritch blast ability, you can change your eldritch blast into a hell fire " +
            "blast. A hellfire blast deals your normal eldritch blast damage plus an extra 2d6 points of damage per " +
            "class level. If your blast hits multiple targets (for example, the eldritch chain or eldritch cone " +
            "blast shape invocations), each target takes the extra damage. This damage is not fire damage. Hellfire " +
            "burns hotter than any normal fire.\n" +
            "Hellfire is the creation of Mephistopheles, archduke of Cania. Hotter than the hottest flames of any " +
            "world, hellfire burns with a white-hot glow and is capable of burning through even the hardest of " +
            "substances. Hellfire does not deal fire damage, despite its flames. Even creatures with immunity or " +
            "resistance to fire take full normal damage from these hellish flames. Hellfire also deals full damage " +
            "to objects, unlike normal fire damage.\n" +
            "Each time you use this ability, you take 1 point of Constitution damage. Because the diabolical forces " +
            "behind the power of hellfire demand part of your essence in exchange for this granted power, if you do " +
            "not have a Constitution score or are somehow immune to Constitution damage, you cannot use this ability.";

        [LocalizedString]
        internal const string DescriptionShort =
            "Whenever you use your eldritch blast ability, you can change your eldritch blast into a hell fire " +
            "blast. A hellfire blast deals your normal eldritch blast damage plus an extra 2d6 points of damage per " +
            "class level.\n" +
            "Each time you use this ability, you take 1 point of Constitution damage. Because the diabolical forces " +
            "behind the power of hellfire demand part of your essence in exchange for this granted power, if you do " +
            "not have a Constitution score or are somehow immune to Constitution damage, you cannot use this ability.";

        class HellfireBlastComponent : EldritchBlastElementalEssence
        {
            public override int DamageTypePriority => 1;

            public HellfireBlastComponent() : base()
            {
                this.BlastDamageType = DamageEnergyType.Unholy;
            }
        }

        public static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var rankFeature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("HellfireBlastRankFeature"))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_HellfireBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_HellfireBlast_Description;
                    feature.m_DescriptionShort = LocalizedStrings.Features_HellfireBlast_DescriptionShort;
                    feature.m_Icon = Sprites.EldritchBlastMythic;

                    feature.Ranks = 3;

                    feature.HideInCharacterSheetAndLevelUp = true;

                    return feature;
                });

            var essenceBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("HellfireBlastEssenceBuff"))
                .Combine(rankFeature)
                .Map(bps =>
                {
                    var (buff, rank) = bps;

                    buff.m_DisplayName = rank.m_DisplayName;
                    buff.m_Description = rank.m_DescriptionShort;
                    buff.m_Icon = rank.m_Icon;

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<HellfireBlastComponent>(c =>
                    {
                        c.Actions.Add(GameActions.ContextActionDealDamage(damage =>
                        {
                            damage.DamageType.Type = DamageType.Direct;

                            damage.m_Type = ContextActionDealDamage.Type.AbilityDamage;
                            damage.AbilityType = StatType.Constitution;
                            damage.Value.BonusValue = 1;
                        }));
                    });

                    buff.AddComponent<AddDamageToEldritchBlast>(c =>
                    {
                        c.DamageType.Type = DamageType.Energy;
                        c.DamageType.Energy = DamageEnergyType.Unholy;

                        c.Value.DiceType = DiceType.D6;
                        c.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                    });

                    buff.AddContextRankConfig(crc =>
                    {
                        crc.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                        crc.m_Feature = rank.ToReference();
                        crc.m_StepLevel = 2;
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("HellfireBlastAbility"))
                .Combine(essenceBuff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    ability.m_DisplayName = buff.m_DisplayName;
                    ability.m_Description = buff.m_Description;
                    ability.m_Icon = buff.m_Icon;

                    ability.m_Buff = buff.ToReference();

                    ability.ActivationType = AbilityActivationType.Immediately;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("HellfireBlastFeature"))
                .Combine(rankFeature)
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, rank, ability) = bps.Expand();

                    feature.m_DisplayName = rank.m_DisplayName;
                    feature.m_Description = rank.m_Description;
                    feature.m_DescriptionShort = rank.m_DescriptionShort;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c =>
                    {
                        c.m_Facts = [ability.ToReference<BlueprintUnitFactReference>()];
                    });

                    return feature;
                });

            return feature;
        }
    }
}
