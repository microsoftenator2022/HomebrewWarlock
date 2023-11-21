using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Fx;

using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace HomebrewWarlock.NewComponents
{
    internal class EldritchBlastOnHitFX : ContextAction
    {
        public BlueprintProjectileReference? DefaultProjectile;

        internal static ContextActionSpawnFxOnLocator? GetProjectileHitFx(BlueprintProjectile projectile)
        {
            if (projectile.ProjectileHit.HitFx is not { } hitFx) return null;

            return new ContextActionSpawnFxOnLocator()
            {
                PrefabLink = new() { AssetId = hitFx.AssetId },
                TargetBone = projectile.TargetBone,
                TargetBoneOffsetMultiplier = projectile.TargetBoneOffsetMultiplier
            };
        }

        internal static ContextActionSpawnFxOnLocator? GetProjectileHitSnapFx(BlueprintProjectile projectile)
        {
            if (projectile.ProjectileHit.HitSnapFx is not { } hitSnapFx) return null;

            return new ContextActionSpawnFxOnLocator()
            {
                PrefabLink = new() { AssetId = hitSnapFx.AssetId }
            };
        }

        public override string GetCaption() => "Eldritch Blast FX";
        public override void RunAction()
        {
            if (Context is null) return;

            var essenceComponents = new EldritchBlastEssence[0];

            if (Context.MaybeCaster is not null)
            {
                essenceComponents = EldritchBlastEssence.GetEssenceBuffs(Context.MaybeCaster)
                    .SelectMany(buff => buff.BlueprintComponents.OfType<EldritchBlastEssence>())
                    .ToArray();
            }

            if (essenceComponents.Length == 0)
            {
                if (DefaultProjectile is null || DefaultProjectile.Get() is not { } projectile) return;

                GetProjectileHitFx(projectile)?.RunAction();
                GetProjectileHitSnapFx(projectile)?.RunAction();

                return;
            }

            foreach (var essence in essenceComponents)
            {
                essence.FxActions.Run();
            }
        }
    }

}
