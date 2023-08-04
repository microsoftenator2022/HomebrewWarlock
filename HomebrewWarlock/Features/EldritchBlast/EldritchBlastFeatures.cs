using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations.Greater;
using HomebrewWarlock.Features.Invocations.Least;
using HomebrewWarlock.Features.Invocations.Lesser;
using HomebrewWarlock.Fx;

using Kingmaker.Blueprints.Classes;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.EldritchBlast
{
    internal class EldritchBlastFeatures
    {
        internal class BlastFeatures
        {
            internal class LeastBlasts
            {
                public BlueprintFeature EldritchSpear = null!;
                public BlueprintFeature HideousBlow = null!;
                public BlueprintFeature EldritchGlaive = null!;
            }
            public readonly LeastBlasts Least = new();

            internal class LesserBlasts
            {
                public BlueprintFeature EldritchChain = null!;
            }
            public readonly LesserBlasts Lesser = new();

            internal class GreaterBlasts
            {
                public BlueprintFeature EldritchCone = null!;
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
                public BlueprintFeature BewitchingBlast = null!;
                public BlueprintFeature NoxiousBlast = null!;
                public BlueprintFeature VitriolicBlast = null!;
            }
            public readonly GreaterEssence Greater = new();

            internal class DarkEssence
            {

            }
            public readonly DarkEssence Dark = new();
        }

        public BlueprintFeature EldritchBlastRank = null!;
        public BlueprintFeature EldritchBlastBase = null!;

        public readonly BlastFeatures Blasts = new();
        
        public readonly EssenceFeatures Essence = new();

        private EldritchBlastFeatures() { }

        internal static BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> CreateFeatures(
            BlueprintInitializationContext context)
        {
            var projectile = EldritchBlastProjectile.CreateProjectile(context);

            var ebFeatures = context.Empty
                .Map(() => new EldritchBlastFeatures())
                .Combine(FrightfulBlast.Create(context))
                .Combine(SickeningBlast.Create(context))
                .Combine(BrimstoneBlast.Create(context))
                .Combine(BeshadowedBlast.Create(context))
                .Combine(HellrimeBlast.Create(context))
                .Combine(BewitchingBlast.Create(context))
                .Map(features =>
                {
                    var (ebFeatures, fb, sb, brimstone, beshadowed, hellrime, bewitching) = features.Expand();

                    ebFeatures.Essence.Least.FrightfulBlast = fb;
                    ebFeatures.Essence.Least.SickeningBlast = sb;
                    ebFeatures.Essence.Lesser.BrimstoneBlast = brimstone;
                    ebFeatures.Essence.Lesser.BeshadowedBlast = beshadowed;
                    ebFeatures.Essence.Lesser.HellrimeBlast = hellrime;
                    ebFeatures.Essence.Greater.BewitchingBlast = bewitching;
                    
                    return ebFeatures;
                })
                .Combine(NoxiousBlast.Create(context))
                .Combine(VitriolicBlast.Create(context))
                .Map(features =>
                {
                    var (ebFeatures, noxious, vitriolic) = features.Expand();

                    ebFeatures.Essence.Greater.NoxiousBlast = noxious;
                    ebFeatures.Essence.Greater.VitriolicBlast = vitriolic;

                    return ebFeatures;

                });

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
                .Combine(EldritchGlaive.Create(context, baseFeatures))
                .Combine(EldritchCone.Create(context, baseFeatures))
                .Map(features =>
                {
                    var (ebFeatures, es, hb, chain, eg, cone) = features.Expand();

                    ebFeatures.Blasts.Least.EldritchSpear = es;
                    ebFeatures.Blasts.Least.HideousBlow = hb;
                    ebFeatures.Blasts.Least.EldritchGlaive = eg;
                    ebFeatures.Blasts.Lesser.EldritchChain = chain;
                    ebFeatures.Blasts.Greater.EldritchCone = cone;

                    return ebFeatures;
                });

            return ebFeatures;
        }
    }
}
