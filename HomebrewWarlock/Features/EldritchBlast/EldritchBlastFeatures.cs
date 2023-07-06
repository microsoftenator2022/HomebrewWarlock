using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations;
using HomebrewWarlock.Features.Invocations.Lesser;

using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features.EldritchBlast
{
    //using EssenceFeature = (BlueprintFeature feature, EldritchBlastComponents.EssenceEffect essence);

    internal record class EssenceFeature(BlueprintFeature Feature, EldritchBlastComponents.EssenceEffect Essence);

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
                public EssenceFeature FrightfulBlast = null!;
                public EssenceFeature SickeningBlast = null!;
            }
            public readonly LeastEssence Least = new();

            internal class LesserEssence
            {
                public EssenceFeature BrimstoneBlast = null!;
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
        }

        public BlueprintFeature EldritchBlastRank = null!;
        public BlueprintFeature EldritchBlastBase = null!;

        public readonly BlastFeatures Blasts = new();
        //public BlueprintFeature EldritchSpear { get; private set; } = null!;
        
        public readonly EssenceFeatures Essence = new();
        //public EssenceFeature FrightfulBlast { get; private set; } = null!;
        //public EssenceFeature SickeningBlast { get; private set; } = null!;

        //public EssenceFeature BrimstoneBlast { get; private set; } = null!;

        private EldritchBlastFeatures() { }

        internal static BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> CreateFeatures(
            BlueprintInitializationContext context)
        {
            var projectile = EldritchBlast.CreateProjectile(context);

            var ebFeatures = context.Empty
                .Map(() => new EldritchBlastFeatures());

            var frightfulBlast = Invocations.FrightfulBlast.Create(context);
            var sickeningBlast = Invocations.SickeningBlast.Create(context);
            var brimstoneBlast = Invocations.Lesser.BrimstoneBlast.Create(context);

            ebFeatures = ebFeatures
                .Combine(frightfulBlast)
                .Combine(sickeningBlast)
                .Combine(brimstoneBlast)
                .Map(features =>
                {
                    var (ebFeatures, fb, sb, bb) = features.Expand();
                    ebFeatures.Essence.Least.FrightfulBlast = fb;
                    ebFeatures.Essence.Least.SickeningBlast = sb;
                    ebFeatures.Essence.Lesser.BrimstoneBlast = bb;
                    return ebFeatures;
                });

            var essenceEffects = frightfulBlast
                .Combine(sickeningBlast)
                .Combine(brimstoneBlast)
                .Map(es =>
                {
                    var (fb, sb, bb) = es.Expand();

                    IEnumerable<EldritchBlastComponents.EssenceEffect> ees = new[]
                    {
                        fb.Essence,
                        sb.Essence,
                        bb.Essence
                    };

                    return ees;
                });

            var baseFeatures = EldritchBlast.CreateEldritchBlast(context, projectile, essenceEffects);

            ebFeatures = ebFeatures
                .Combine(baseFeatures)
                .Map(features =>
                {
                    var (ebFeatures, (baseFeature, rankFeature, _, _)) = features;

                    ebFeatures.EldritchBlastBase = baseFeature;
                    ebFeatures.EldritchBlastRank = rankFeature;

                    return ebFeatures;
                });

            var eldritchSpear = EldritchSpear.CreateBlast(context, baseFeatures);
            var hideousBlow = HideousBlow.Create(context, baseFeatures, essenceEffects);

            ebFeatures = ebFeatures
                .Combine(eldritchSpear)
                .Combine(hideousBlow)
                .Map(features =>
                {
                    var (ebFeatures, es, hb) = features.Expand();

                    ebFeatures.Blasts.Least.EldritchSpear = es;
                    ebFeatures.Blasts.Least.HideousBlow = hb;

                    return ebFeatures;
                });

            return ebFeatures;
        }
    }
}
