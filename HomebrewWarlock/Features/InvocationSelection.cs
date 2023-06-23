using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;

namespace HomebrewWarlock.Features
{
    internal static class InvocationSelection
    {
        [LocalizedString]
        internal const string PlaceholderName = "Placeholder";

        [LocalizedString]
        internal const string DisplayName = "Invocations";

        [LocalizedString]
        internal static readonly string Description =
            "A warlock does not prepare or cast spells as other wielders of arcane magic do. Instead, he possesses a " +
            "repertoire of attacks, defenses, and abilities known as invocations that require him to focus the wild " +
            "energy that suffuses his soul. A warlock can use any invocation he knows at will, with the following " +
            "qualifications:" + Environment.NewLine +
            "A warlock's invocations are spell-like abilities; using an invocation is therefore a standard action " +
            "that provokes attacks of opportunity. An invocation can be disrupted, just as a spell can be ruined " +
            "during casting. A warlock is entitled to a Concentration check to successfully use an invocation if " +
            "he is hit by an attack while invoking, just as a spellcaster would be. A warlock can choose to use an " +
            "invocation defensively, by making a successful Concentration check, to avoid provoking attacks of " +
            "opportunity. A warlock's invocations are subject to spell resistance unless an invocation's " +
            "description specifically states otherwise. A warlock's caster level with his invocations is equal to " +
            "his warlock level." + Environment.NewLine +
            "The save DC for an invocation (if it allows a save) is 10 + equivalent spell level + the warlock's " +
            "Charisma modifier." + Environment.NewLine +
            "Unlike other spell-like abilities, invocations are subject to arcane spell failure chance.";

        [LocalizedString]
        internal const string ShortDescription =
            "A warlock does not prepare or cast spells as other wielders of arcane magic do. Instead, he possesses a " +
            "repertoire of attacks, defenses, and abilities known as invocations that require him to focus the wild " +
            "energy that suffuses his soul.";

        internal static readonly IMicroBlueprint<BlueprintFeatureSelection> Selection =
            new MicroBlueprint<BlueprintFeatureSelection>(GeneratedGuid.WarlockInvocationSelection);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeatureSelection> CreateSelection(BlueprintInitializationContext context)
        {
            var placeholderFeature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("WarlockInvocationPlaceholder"), "WarlockInvocationPlaceholder")
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_InvocationSelection_PlaceholderName;

                    feature.Ranks = 12;

                    return feature;
                });

            return context.NewBlueprint<BlueprintFeatureSelection>(
                GeneratedGuid.Get("WarlockInvocationSelection"),
                "WarlockInvocationSelection")
                .Combine(placeholderFeature)
                .Map(features =>
                {
                    var (selection, placeholder) = features;

                    selection.m_DisplayName = LocalizedStrings.Features_InvocationSelection_DisplayName;
                    selection.m_Description = LocalizedStrings.Features_InvocationSelection_Description;
                    selection.m_DescriptionShort = LocalizedStrings.Features_InvocationSelection_ShortDescription;

                    selection.AddFeatures(new[] { placeholder.ToMicroBlueprint() });

                    return selection;
                });
        }
    }
}
