using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Features.Invocations.Least;

using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

using MicroWrath.BlueprintInitializationContext;

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
            "The four grades of invocations, in order of their relative power, are least, lesser, greater, and " +
            "dark. A warlock begins with knowledge of one invocation, which must be of the lowest grade (least). " +
            Environment.NewLine +
            "Lesser invocations are available starting from level 6th level, greater invocations from 13th level, " +
            "and dark invocations from 16th level." + Environment.NewLine +
            "Unlike other spell-like abilities, invocations are subject to arcane spell failure chance.";

        [LocalizedString]
        internal const string ShortDescription =
            "A warlock does not prepare or cast spells as other wielders of arcane magic do. Instead, he possesses a " +
            "repertoire of attacks, defenses, and abilities known as invocations that require him to focus the wild " +
            "energy that suffuses his soul.";

        [LocalizedString]
        internal const string LesserInvocationsDisplayName = "Lesser Invocations";

        [LocalizedString]
        internal const string GreaterInvocationsDisplayName = "Greater Invocations";

        [LocalizedString]
        internal const string DarkInvocationsDisplayName = "Dark Invocations";

        internal static BlueprintInitializationContext.ContextInitializer<(
            BlueprintFeatureSelection invocationSelection,
            BlueprintFeature lesserPrerequisite,
            BlueprintFeature greaterPrerequisite,
            BlueprintFeature darkPrerequisite)> CreateSelection(
                BlueprintInitializationContext context,
                BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> ebFeatures)
        {
            var placeholderFeature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("WarlockInvocationPlaceholder"),
                "WarlockInvocationPlaceholder")
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_InvocationSelection_PlaceholderName;

                    feature.Ranks = 12;

                    return feature;
                });

            var lesserInvocationPrerequisite = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.LesserInvocationsPrerequisiteFeature)
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_InvocationSelection_LesserInvocationsDisplayName;

                    return feature;
                });

            var lesserInvocationSelection =
                Invocations.Lesser.LesserInvocationSelection.Create(context, ebFeatures, lesserInvocationPrerequisite);

            var greaterInvocationsPrerequisite = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("GreaterInvocationsPrerequisiteFeature"))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_InvocationSelection_GreaterInvocationsDisplayName;

                    return feature;
                });

            var greaterInvocationSelection =
                Invocations.Greater.GreaterInvocationSelection.CreateSelection(context, ebFeatures, greaterInvocationsPrerequisite);

            var darkInvocationsPrerequisite = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("DarkInvocationsPrerequisiteFeature"))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_InvocationSelection_DarkInvocationsDisplayName;

                    return feature;
                });

            var darkInvocationSelection =
                Invocations.Dark.DarkInvocationSelection.Create(context, ebFeatures, darkInvocationsPrerequisite);

            var selection = context.NewBlueprint<BlueprintFeatureSelection>(
                GeneratedGuid.Get("WarlockInvocationSelection"),
                "WarlockInvocationSelection")
                .Combine(LeastInvocationSelection.CreateSelection(context, ebFeatures))
                .Combine(placeholderFeature)
                .Combine(ebFeatures)
                .Combine(lesserInvocationSelection)
                .Combine(greaterInvocationSelection)
                .Combine(darkInvocationSelection)
                .Map(features =>
                {
                    var (selection, least, placeholder, ebFeatures, lesser, greater, dark) = features.Expand();

                    selection.m_DisplayName = LocalizedStrings.Features_InvocationSelection_DisplayName;
                    selection.m_Description = LocalizedStrings.Features_InvocationSelection_Description;
                    selection.m_DescriptionShort = LocalizedStrings.Features_InvocationSelection_ShortDescription;

                    selection.AddFeatures(
                        least,
                        lesser,
                        greater,
                        dark
#if DEBUG
                        ,placeholder
#endif
                        );

                    return selection;
                });

            return selection
                .Combine(lesserInvocationPrerequisite)
                .Combine(greaterInvocationsPrerequisite)
                .Combine(darkInvocationsPrerequisite)
                .Map(Functional.Expand);
        }
    }
}
