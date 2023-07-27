using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Components;
using MicroWrath.Constructors;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features.EldritchBlast.Components
{
    internal class EnchantmentRemoveSelf : ContextAction
    {
        public override string GetCaption() => "Remove self";
        public override void RunAction()
        {
            ItemEnchantment.Data data = ContextData<ItemEnchantment.Data>.Current;

            var enchant = data.ItemEnchantment;

            enchant.DestroyFx();
            enchant.Owner.RemoveEnchantment(enchant);
        }
    }

    internal class EldritchBlastCalculateSpellLevel : ContextCalculateAbilityParams
    {
        public EldritchBlastCalculateSpellLevel() : this(1) { }

        public EldritchBlastCalculateSpellLevel(int equivalentSpellLevel)
        {
            SpellLevel = BaseEquivalentSpellLevel = equivalentSpellLevel;
            StatType = StatType.Charisma;
            ReplaceSpellLevel = true;
        }

        public int BaseEquivalentSpellLevel;

        public override AbilityParams Calculate(MechanicsContext context)
        {
            RecalculateSpellLevel(context);

            return base.Calculate(context);
        }

        public void RecalculateSpellLevel(MechanicsContext context)
        {
            if (context?.MaybeOwner is not { } owner) return;

            if (owner.Buffs.Enumerable
                .Where(b => b.IsTurnedOn)
                .SelectMany(b => b.Blueprint.ComponentsArray.OfType<EldritchBlastEssence>())
                .FirstOrDefault() is { } essence)
                this.SpellLevel = Math.Max(essence.EquivalentSpellLevel, this.BaseEquivalentSpellLevel);
            else this.SpellLevel = this.BaseEquivalentSpellLevel;
        }
    }

    internal class EldritchBlastEssence : UnitFactComponentDelegate
        ,
        //IInitiatorRulebookHandler<RuleSpellResistanceCheck>,
        IInitiatorRulebookHandler<RuleDealDamage>
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

        //bool IgnoreSpellResistance;
        //public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt)
        //{
        //    if (!evt.Ability.ComponentsArray.OfType<EldritchBlastCalculateSpellLevel>().Any())
        //        return;

        //    evt.IgnoreSpellResistance = true;
        //}
        //public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }

        public DamageEnergyType BlastDamageType = DamageEnergyType.Magic;
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (BlastDamageType is DamageEnergyType.Magic) return;

            if (evt.SourceAbility is null) return;
            if (!evt.SourceAbility.ComponentsArray.OfType<EldritchBlastCalculateSpellLevel>().Any()) return;

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
        public void OnEventDidTrigger(RuleDealDamage evt) { }

        public ActionList Actions = new();
    }

    internal class EldritchBlastEssenceActions : ContextAction
    {
        public override string GetCaption() => "Eldritch Blast";

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

    internal class BlastAbility(int equivalentSpellLevel)
    {
        public static ContextActionDealDamage BaseDamage =>
            GameActions.ContextActionDealDamage(action =>
            {
                action.m_Type = ContextActionDealDamage.Type.Damage;

                action.DamageType.Type = DamageType.Energy;
                action.DamageType.Energy = DamageEnergyType.Magic;

                action.Value.DiceType = DiceType.D6;

                action.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                action.Value.DiceCountValue.Value = 1;
                action.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;
            });

        public readonly int EquivalentSpellLevel = equivalentSpellLevel;

        public virtual ActionList DamageActions => new() { Actions = new[] { BaseDamage } };

        public virtual BlueprintAbility ConfigureAbility(
            BlueprintAbility ability,
            BlueprintFeatureReference rankFeature)
        {
            ability.Type = AbilityType.Special;

            ability.CanTargetEnemies = true;
            ability.SpellResistance = true;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;
            ability.ActionType = UnitCommand.CommandType.Standard;

            ability.AddComponent<ArcaneSpellFailureComponent>();
            ability.AddComponent(new EldritchBlastCalculateSpellLevel(EquivalentSpellLevel));

            ability.AddContextRankConfig(c =>
            {
                c.m_Type = AbilityRankType.DamageDice;
                c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                c.m_Feature = rankFeature;
                c.m_Progression = ContextRankProgression.AsIs;
                c.m_StartLevel = 0;
                c.m_StepLevel = 1;
            });

            var runAction = ability.EnsureComponent<AbilityEffectRunAction>();
            
            runAction.Actions.Add(DamageActions.Actions);
            runAction.Actions.Add(new EldritchBlastEssenceActions());

            return ability;
        }
    }
}
