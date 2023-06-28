using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations
{
    internal static class BeguilingInfluence
    {
        [LocalizedString]
        internal const string DisplayName = "Beguiling Influence";

        [LocalizedString]
        internal const string Description = "You can invoke this ability to beguile and bewitch your foes. You " +
            "gain a +6 bonus on Persuasion checks";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context) =>
            context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get(nameof(BeguilingInfluence)), nameof(BeguilingInfluence))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_BeguilingInfluence_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_BeguilingInfluence_Description;

                    feature.m_Icon = AssetUtils.Direct.GetSprite("494cc3f31fcb2a24cb7e69ec5df0055c", 21300000);

                    feature.AddAddStatBonus(c =>
                    {
                        c.Stat = StatType.SkillPersuasion;
                        c.Value = 6;
                        c.Descriptor = ModifierDescriptor.UntypedStackable;
                    });

                    return feature;
                });
    }
}
