using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;

namespace HomebrewWarlock.NewComponents
{
    [TypeId("dd6af2a9-111e-4d45-a254-62cbf7e4e478")]
    internal class DeliverEldritchBlastProjectile : AbilityDeliverProjectile
    {
        public BlueprintProjectileReference? DefaultProjectile;

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            base.m_Projectiles = new[] { DefaultProjectile };

            if (context is not null && context.MaybeCaster is { } caster)
            {
                var essenceProjectiles = EldritchBlastEssence.GetEssenceBuffs(caster)
                    .SelectMany(buff => buff.BlueprintComponents.OfType<EldritchBlastEssence>())
                    .Select(c => c.Projectiles)
                    .FirstOrDefault(ep => ep.ContainsKey(base.Type));

                if (essenceProjectiles is not null)
                    base.m_Projectiles = essenceProjectiles[base.Type];
            }

            return base.Deliver(context, target);
        }
    }
}
