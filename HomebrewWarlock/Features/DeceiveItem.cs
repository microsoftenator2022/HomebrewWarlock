using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features
{
    internal static class DeceiveItem
    {
        [LocalizedString]
        internal const string DisplayName = "Deceive Item";

        [LocalizedString]
        internal const string Description = "At 4th level and higher, a warlock has the ability to more easily " +
            "commandeer magic items made for the use of other characters. When making a Use Magic Device check, a " +
            "warlock can take 10 even if distracted or threatened.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var buff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("DeceiveItemBuff"),
                nameof(GeneratedGuid.DeceiveItemBuff))
                .Map((BlueprintBuff buff) =>
                {
                    buff.AddModifyD20(c =>
                    {
                        c.Rule = RuleType.SkillCheck;
                        c.SpecificSkill = true;
                        c.Skill = new[] { StatType.SkillUseMagicDevice };
                        c.m_SavingThrowType = ModifyD20.InnerSavingThrowType.All;
                    
                        c.Replace = true;

                        c.RollsAmount = 0;
                        c.RollResult.Value = 10;
                    });

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DeceiveItemAbility"),
                nameof(GeneratedGuid.DeceiveItemAbility));

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("DeceiveItemFeature"),
                nameof(GeneratedGuid.DeceiveItemFeature))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_DeceiveItem_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_DeceiveItem_Description;

                    feature.m_Icon = AssetUtils.Direct.GetSprite("53a4e13e9b8bfd34eb3f770ec000809f", 21300000);

                    return feature;
                })
                .Combine(ability)
                .Combine(buff)
                .Map(fa =>
                {
                    var (feature, ability, buff) = fa.Flatten();

                    ability.m_DisplayName = feature.m_DisplayName;
                    ability.m_Description = feature.m_Description;
                    ability.m_Icon = feature.m_Icon;

                    ability.m_Buff = buff.ToReference<BlueprintBuffReference>();
                    ability.ActivationType = AbilityActivationType.Immediately;
                    ability.DeactivateImmediately = true;

                    feature.AddAddFacts(c =>
                    {
                        c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() };
                    });

                    return feature;
                });

            return feature;
        }
    }
}
