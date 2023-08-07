using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;

using UnityEngine;

namespace HomebrewWarlock.Fx
{
    internal class ContextActionSpawnFxOnLocator : ContextAction
    {
        public string? TargetBone;
        public float TargetBoneOffsetMultiplier = 1f;
        public PrefabLink PrefabLink = new();

        public override string GetCaption() =>
		    "Spawn FX: " + (PrefabLink.Load(false, false)?.name ?? "unspecified");

        public override void RunAction()
        {
            if (base.Context.DisableFx) return;

            var fxPrefab = PrefabLink.Load(false, false);

            MicroLogger.Debug(() => $"{nameof(ContextActionSpawnFxOnLocator)}");
            MicroLogger.Debug(() => $"Caster: {base.Context.MaybeCaster}");
            MicroLogger.Debug(() => $"Target: {base.Target.Unit}");

            var caster = base.Context.MaybeCaster;

            if (caster == base.Target.Unit) return;

            if (base.Target.Unit is not null)
            {
                var target = base.Target.Unit.View;
            
                if (TargetBone is not null)
                {
                    var targetBone = target.ParticlesSnapMap.ToOption().Map(sm => sm[TargetBone]).Value;
                    var offset = targetBone.ToOption().Map(bone =>
                    {
                        return bone.CameraOffset * TargetBoneOffsetMultiplier *
                            ((caster?.Position ?? default) - bone.Transform.position).normalized;
                    }).Value;

                    FxHelper.SpawnFxOnUnit(fxPrefab, target, caster?.IsPlayerFaction ?? false, TargetBone, offset, 
                        FxPriority.EventuallyImportant);
                    return;
                }

                FxHelper.SpawnFxOnUnit(fxPrefab, target, caster?.IsPlayerFaction ?? false, null, 
                    base.Target.Unit.Position + Vector3.up, FxPriority.EventuallyImportant);
            }
        }
    }
}
