using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath.Util.Linq;

namespace HomebrewWarlock.NewComponents
{
    [TypeId("40686a57-7952-4459-8dd9-3e6c0c830ebb")]
    internal class EldritchBlastEssence : UnitFactComponentDelegate
    {
        public static IEnumerable<Buff> GetEssenceBuffs(UnitEntityData unit) =>
            unit.Buffs.Enumerable.Where(buff => buff.BlueprintComponents.OfType<EldritchBlastEssence>().Any());

        public int EquivalentSpellLevel = 1;

        void RecalculateBlastDCs()
        {
            if (Context is null || Owner is null) return;

            foreach (var component in Owner.Abilities.Enumerable
                .SelectMany(a => a.Blueprint.Components.OfType<EldritchBlastCalculateSpellLevel>()))
            {
                component.RecalculateSpellLevel(Context);
            }
        }

        public override void OnTurnOn()
        {
            RecalculateBlastDCs();

            base.OnTurnOn();
        }

        public override void OnTurnOff()
        {
            RecalculateBlastDCs();

            base.OnTurnOff();
        }

        public virtual IDictionary<AbilityProjectileType, BlueprintProjectileReference[]> Projectiles { get; } =
            new (AbilityProjectileType, BlueprintProjectileReference[])[0].ToDictionary();

        public virtual ActionList Actions { get; set; }

        public virtual ActionList FxActions
        {
            get
            {
                var actions = new ActionList() { Actions = new GameAction[0] };

                if (Projectiles[AbilityProjectileType.Simple].FirstOrDefault() is { } blueprint)
                {
                    if (EldritchBlastOnHitFX.GetProjectileHitFx(blueprint) is { } onHit)
                        actions.Add(onHit);

                    if (EldritchBlastOnHitFX.GetProjectileHitSnapFx(blueprint) is { } onHitSnap)
                        actions.Add(onHitSnap);
                }

                return actions;
            }
        }

        public EldritchBlastEssence()
        {
            Actions = new();
        }
    }

    [TypeId("5b3a3656-5820-4175-969e-eee497dab50f")]
    internal class EldritchBlastElementalEssence : EldritchBlastEssence, IInitiatorRulebookHandler<RuleDealDamage>
    {
        public DamageEnergyType BlastDamageType = DamageEnergyType.Magic;

        public virtual int DamageTypePriority { get; } = 0;

        public virtual void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (BlastDamageType is DamageEnergyType.Magic) return;

            if (evt.SourceAbility is null) return;
            if (!evt.SourceAbility.ComponentsArray.OfType<EldritchBlastComponent>().Any()) return;

            if (evt.Initiator.Buffs.Enumerable
                .Where(b => b.IsTurnedOn)
                .SelectMany(b => b.Blueprint.ComponentsArray.OfType<EldritchBlastElementalEssence>())
                .Where(b => b != this)
                .Any(element => element.DamageTypePriority > this.DamageTypePriority))
                return;

            var damage = evt.DamageBundle.ToArray();
            evt.Remove(_ => true);

            foreach (var bd in damage)
            {
                if (bd is EnergyDamage ed && ed.EnergyType is DamageEnergyType.Magic)
                {
                    ed = new EnergyDamage(ed.Dice, ed.Bonus, BlastDamageType);
                    ed.CopyFrom(bd);

                    evt.Add(ed);
                }
                else evt.Add(bd);
            }
        }
        public virtual void OnEventDidTrigger(RuleDealDamage evt) { }
    }

    internal class EldritchBlastEssenceActions : ContextAction
    {
        public override string GetCaption() => "Eldritch Blast Essence Actions";

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

            foreach (var essence in essenceComponents)
            {
                essence.Actions.Run();
            }
        }
    }
}
