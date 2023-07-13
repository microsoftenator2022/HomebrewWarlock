using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints.Classes;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;

namespace HomebrewWarlock.Features
{
    internal static class AbilityFocusEldritchBlast
    {
        internal const string DisplayName = "Ability Focus (Eldritch Blast)";
        internal class Component : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAbilityParams>
        {
            public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
            {
                if (!evt.AbilityData.Blueprint.Components.OfType<EldritchBlastCalculateSpellLevel>().Any())
                    return;

                evt.AddBonusDC(2);
            }

            public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
        }

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintFeature> eldritchBlast)
        {
            return context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get(nameof(AbilityFocusEldritchBlast)),
                nameof(AbilityFocusEldritchBlast))
                .Combine(eldritchBlast)
                .Map(bps =>
                {
                    var (feature, eldritchBlast) = bps;
                    feature.AddComponent<Component>();

                    feature.AddPrerequisiteFeature(eldritchBlast.ToMicroBlueprint());

                    return feature;
                });
        }
    }
}
