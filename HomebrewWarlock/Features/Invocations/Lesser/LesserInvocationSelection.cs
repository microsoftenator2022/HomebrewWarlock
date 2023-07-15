﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class LesserInvocationSelection
    {
        [LocalizedString]
        internal const string DisplayName = "Lesser";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeatureSelection> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> ebFeatures,
            BlueprintInitializationContext.ContextInitializer<BlueprintFeature> prerequisite)
        {
            var selection = context.NewBlueprint<BlueprintFeatureSelection>(
                GeneratedGuid.Get(nameof(LesserInvocationSelection)), nameof(LesserInvocationSelection))
                .Combine(ebFeatures)
                .Combine(prerequisite)
                .Combine(CurseOfDespair.Create(context))
                .Combine(FellFlight.Create(context))
                .Combine(TheDeadWalk.Create(context))
                .Map(bps =>
                {
                    var (selection,
                        ebFeatures,
                        prerequisite,
                        curseOfDespair,
                        fellFlight,
                        theDeadWalk) = bps.Expand();

                    selection.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Lesser_LesserInvocationSelection_DisplayName;
#if !DEBUG
                    selection.AddPrerequisiteFeature(prerequisite.ToMicroBlueprint());
                    prerequisite.IsPrerequisiteFor = new() { selection.ToReference<BlueprintFeatureReference>() };
#endif
                    selection.AddFeatures(
                        ebFeatures.Essence.Lesser.BrimstoneBlast.Feature,
                        ebFeatures.Essence.Lesser.BeshadowedBlast.Feature,
                        curseOfDespair,
                        fellFlight,
                        ebFeatures.Blasts.Lesser.EldritchChain,
                        theDeadWalk);

                    return selection;

                });

            return selection;
        }
    }
}
