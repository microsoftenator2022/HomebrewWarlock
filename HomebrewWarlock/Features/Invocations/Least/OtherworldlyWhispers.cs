﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations
{
    internal static class OtherworldlyWhispers
    {
        [LocalizedString]
        internal const string DisplayName = "Otherworldly Whispers";

        [LocalizedString]
        internal const string Description =
            "You hear whispers in your ears, revealing secrets of the multiverse. You gain a +6 bonus on Knowledge " +
            "(arcana), Lore (religion) checks.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get(nameof(OtherworldlyWhispers)), nameof(OtherworldlyWhispers))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_OtherworldlyWhispers_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_OtherworldlyWhispers_Description;

                    feature.m_Icon = AssetUtils.Direct.GetSprite("aec14e47a17206049aea57b6e325b900", 21300000);

                    feature.AddAddStatBonus(c =>
                    {
                        c.Stat = StatType.SkillKnowledgeArcana;
                        c.Value = 6;
                    });

                    feature.AddAddStatBonus(c =>
                    {
                        c.Stat = StatType.SkillLoreReligion;
                        c.Value = 6;
                    });

                    return feature;
                });

            return feature;
        }
    }
}