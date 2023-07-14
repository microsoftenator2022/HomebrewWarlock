using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints.Classes;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;

namespace HomebrewWarlock.Features
{
    internal static class AbilityFocusEldritchBlast
    {
        [LocalizedString]
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
            BlueprintInitializationContext context)
        {
            return context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get(nameof(AbilityFocusEldritchBlast)),
                nameof(AbilityFocusEldritchBlast))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintParametrizedFeature.AbilityFocus))
                .Map(bps =>
                {
                    var (feature, abilityFocus) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_AbilityFocusEldritchBlast_DisplayName;
                    feature.m_Description = abilityFocus.m_Description;
                    feature.m_Icon = Sprites.SkillFocus;

                    feature.AddComponent<AbilityFocusEldritchBlast.Component>();

                    feature.AddPrerequisiteFeature(EldritchBlast.EldritchBlast.FeatureRef);

                    return feature;
                });
        }

        [Init]
        internal static void Init()
        {
            var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);
            Create(context)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.BasicFeatSelection))
                .Map(bps =>
                {
                    var (feature, basicFeats) = bps;

                    basicFeats.AddFeatures(feature);
                })
                .Register();
        }
    }
}
