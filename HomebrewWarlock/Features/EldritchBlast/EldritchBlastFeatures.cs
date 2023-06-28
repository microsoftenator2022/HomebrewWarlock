using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.EldritchBlast
{
    internal record class EldritchBlastFeatures(
        BlueprintFeature? EldritchBlastBaseFeature,
        BlueprintFeature? EldritchSpearFeature)
    {
        internal static BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> Create(
            BlueprintInitializationContext context)
        {
            var projectile = EldritchBlast.CreateProjectile(context);

            var features = EldritchBlast.CreateFeature(context, projectile)
                .Map(eb => new EldritchBlastFeatures(eb, null))
                .Combine(EldritchSpear.Create(context, projectile))
                .Map(ebf => ebf.Left with { EldritchSpearFeature = ebf.Right });

            return features;
        }
    }
}
