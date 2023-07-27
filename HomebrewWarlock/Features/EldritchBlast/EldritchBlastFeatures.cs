﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations;
using HomebrewWarlock.Features.Invocations.Least;
using HomebrewWarlock.Features.Invocations.Lesser;

using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features.EldritchBlast
{
    //internal record class EssenceFeature(BlueprintFeature Feature, EldritchBlastComponents.EssenceEffect Essence);

    internal class EldritchBlastFeatures
    {
        internal class BlastFeatures
        {
            internal class LeastBlasts
            {
                public BlueprintFeature EldritchSpear = null!;
                public BlueprintFeature HideousBlow = null!;
            }
            public readonly LeastBlasts Least = new();

            internal class LesserBlasts
            {
                public BlueprintFeature EldritchChain = null!;
            }
            public readonly LesserBlasts Lesser = new();

            internal class GreaterBlasts
            {

            }
            public readonly GreaterBlasts Greater = new();

            internal class DarkBlasts
            {

            }
            public readonly DarkBlasts Dark = new();
        }

        internal class EssenceFeatures
        {
            internal class LeastEssence
            {
                public BlueprintFeature FrightfulBlast = null!;
                public BlueprintFeature SickeningBlast = null!;
            }
            public readonly LeastEssence Least = new();

            internal class LesserEssence
            {
                public BlueprintFeature BrimstoneBlast = null!;
                public BlueprintFeature BeshadowedBlast = null!;
                public BlueprintFeature HellrimeBlast = null!;
            }
            public readonly LesserEssence Lesser = new();

            internal class GreaterEssence
            {

            }
            public readonly GreaterEssence Greater = new();

            internal class DarkEssence
            {

            }
            public readonly DarkEssence Dark = new();

            //public IEnumerable<EldritchBlastComponents.EssenceEffect> EssenceEffects => new[]
            //{
            //    Least.FrightfulBlast.Essence,
            //    Least.SickeningBlast.Essence,

            //    Lesser.BrimstoneBlast.Essence,
            //    Lesser.BeshadowedBlast.Essence,
            //    Lesser.HellrimeBlast.Essence
            //};
        }

        public BlueprintFeature EldritchBlastRank = null!;
        public BlueprintFeature EldritchBlastBase = null!;

        public readonly BlastFeatures Blasts = new();
        
        public readonly EssenceFeatures Essence = new();

        private EldritchBlastFeatures() { }

        internal static BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> CreateFeatures(
            BlueprintInitializationContext context)
        {
            var projectile = EldritchBlast.CreateProjectile(context);

            var ebFeatures = context.Empty
                .Map(() => new EldritchBlastFeatures())
                .Combine(FrightfulBlast.Create(context))
                .Combine(SickeningBlast.Create(context))
                .Combine(BrimstoneBlast.Create(context))
                .Combine(BeshadowedBlast.Create(context))
                .Combine(HellrimeBlast.Create(context))
                .Map(features =>
                {
                    var (ebFeatures, fb, sb, brimstone, beshadowed, hellrime) = features.Expand();

                    ebFeatures.Essence.Least.FrightfulBlast = fb;
                    ebFeatures.Essence.Least.SickeningBlast = sb;
                    ebFeatures.Essence.Lesser.BrimstoneBlast = brimstone;
                    ebFeatures.Essence.Lesser.BeshadowedBlast = beshadowed;
                    ebFeatures.Essence.Lesser.HellrimeBlast = hellrime;
                    
                    return ebFeatures;
                });

            //var essenceEffects = frightfulBlast
            //    .Combine(sickeningBlast)
            //    .Combine(brimstoneBlast)
            //    .Combine(beshadowedBlast)
            //    .Map(es =>
            //    {
            //        var (fb, sb, brimstone, beshadowed) = es.Expand();

            //        IEnumerable<EldritchBlastComponents.EssenceEffect> ees = new[]
            //        {
            //            fb.Essence,
            //            sb.Essence,
            //            brimstone.Essence,
            //            beshadowed.Essence
            //        };

            //        return ees;
            //    });

            //var essenceEffects = ebFeatures.Map(ebFeatures => ebFeatures.Essence.EssenceEffects);

            var baseFeatures = EldritchBlast.CreateEldritchBlast(context, projectile);

            ebFeatures = ebFeatures
                .Combine(baseFeatures)
                .Map(features =>
                {
                    var (ebFeatures, (baseFeature, rankFeature, _, _)) = features;

                    ebFeatures.EldritchBlastBase = baseFeature;
                    ebFeatures.EldritchBlastRank = rankFeature;

                    return ebFeatures;
                });

            ebFeatures = ebFeatures
                .Combine(EldritchSpear.CreateBlast(context, baseFeatures))
                .Combine(HideousBlow.Create(context, baseFeatures))
                .Combine(EldritchChain.CreateBlast(context, baseFeatures))
                .Map(features =>
                {
                    var (ebFeatures, es, hb, ec) = features.Expand();

                    ebFeatures.Blasts.Least.EldritchSpear = es;
                    ebFeatures.Blasts.Least.HideousBlow = hb;
                    ebFeatures.Blasts.Lesser.EldritchChain = ec;

                    return ebFeatures;
                });

            return ebFeatures;
        }
    }
}
