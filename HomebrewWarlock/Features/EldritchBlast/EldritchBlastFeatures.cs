using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations;

using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features.EldritchBlast
{
    using EssenceFeature = (BlueprintFeature feature, EldritchBlastComponents.EssenceEffect essence);

    internal class EldritchBlastFeatures
    {
        public Option<BlueprintFeature> EldritchBlastRank { get; private set; } = Option<BlueprintFeature>.None;
        public Option<BlueprintFeature> EldritchBlastBase { get; private set; } = Option<BlueprintFeature>.None;
        public Option<BlueprintFeature> EldritchSpear { get; private set; } = Option<BlueprintFeature>.None;

        public Option<EssenceFeature> FrightfulBlast { get; private set; } = Option<EssenceFeature>.None;

        private EldritchBlastFeatures() { }

        internal static BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> CreateFeatures(
            BlueprintInitializationContext context)
        {
            var projectile = EldritchBlast.CreateProjectile(context);

            var ebFeatures = context.Empty
                .Map(() => new EldritchBlastFeatures());

            var frightfulBlast = Invocations.FrightfulBlast.Create(context);

            ebFeatures = ebFeatures
                .Combine(frightfulBlast)
                .Map(features =>
                {
                    var (ebFeatures, fb) = features;
                    ebFeatures.FrightfulBlast = Option.Some(fb);
                    return ebFeatures;
                });

            var essenceEffects = frightfulBlast
                .Map(fb =>
                {
                    return EnumerableExtensions.Singleton(fb.Item2);
                });

            var baseFeatures = EldritchBlast.CreateEldritchBlast(context, projectile, essenceEffects);

            ebFeatures = ebFeatures
                .Combine(baseFeatures)
                .Map(features =>
                {
                    var (ebFeatures, (baseFeature, rankFeature, _, _)) = features;

                    ebFeatures.EldritchBlastBase = Option.Some(baseFeature);
                    ebFeatures.EldritchBlastRank = Option.Some(rankFeature);

                    return ebFeatures;
                });

            var eldritchSpear = Features.EldritchBlast.EldritchSpear.CreateBlast(context, baseFeatures);

            ebFeatures = ebFeatures
                .Combine(eldritchSpear)
                .Map(features =>
                {
                    var (ebFeatures, es) = features;

                    ebFeatures.EldritchSpear = Option.Some(es);

                    return ebFeatures;
                });

            return ebFeatures;
        }
    }
}
