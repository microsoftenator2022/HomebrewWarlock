using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features
{
    internal static class WarlockDamageReduction
    {
        [LocalizedString]
        internal const string DisplayName = "Damage Reduction";

        [LocalizedString]
        internal const string Description =
            "Fortified by the supernatural power flowing in his body, a warlock becomes resistant to " +
            "physical attacks at 3rd level and above, gaining damage reduction 1/cold iron. At 7th level " +
            "and every four levels thereafter, a warlock's damage reduction improves by 1 to a maximum of " +
            "5 at 19th level";

        public static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            return context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get(nameof(WarlockDamageReduction)), nameof(WarlockDamageReduction))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_WarlockDamageReduction_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_WarlockDamageReduction_Description;

                    feature.m_Icon = AssetUtils.Direct.GetSprite("da043f7009753c5418615fb68cf7b198", 21300000);

                    feature.AddAddDamageResistancePhysical(c =>
                    {
                        c.Value = new()
                        {
                            ValueType = ContextValueType.Rank
                        };
                        c.BypassedByMaterial = true;
                        c.Material = PhysicalDamageMaterial.ColdIron;
                    });

                    feature.AddContextRankConfig(c =>
                    {
                        c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                        c.m_Feature = feature.ToReference<BlueprintFeatureReference>();
                        c.m_Progression = ContextRankProgression.AsIs;
                        c.m_StartLevel = 0;
                        c.m_StepLevel = 1;
                    });

                    feature.Ranks = 5;

                    return feature;
                });
        }
    }
}
