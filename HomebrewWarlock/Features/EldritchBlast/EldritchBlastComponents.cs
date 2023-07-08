using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Features.Invocations;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Components;
using MicroWrath.Conditions;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Util;

using Newtonsoft.Json;

using Owlcat.Runtime.Core;
using Owlcat.Runtime.Core.Utils;

using UnityEngine;

namespace HomebrewWarlock.Features.EldritchBlast
{
    internal static class EldritchBlastComponents
    {
        internal static ContextRankConfig GetContextRankConfig(BlueprintFeatureReference ebRankFeature) =>
            new()
            {
                m_Type = AbilityRankType.DamageDice,
                m_BaseValueType = ContextRankBaseValueType.FeatureRank,
                m_Feature = ebRankFeature,
                m_Progression = ContextRankProgression.AsIs,
                m_StartLevel = 0,
                m_StepLevel = 1
            };

        private static ContextActionDealDamage GetEldritchBlastDamage(DamageEnergyType damageType = DamageEnergyType.Magic) =>
            GameActions.ContextActionDealDamage(action =>
            {
                action.m_Type = ContextActionDealDamage.Type.Damage;

                action.DamageType.Type = DamageType.Energy;
                action.DamageType.Energy = damageType;

                action.Value.DiceType = DiceType.D6;

                action.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                action.Value.DiceCountValue.Value = 1;
                action.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;
            });

        internal record class EssenceEffect(
            BlueprintBuff EssenceBuff,
            Func<IEnumerable<GameAction>> OnHitActions,
            int EquivalentSpellLevel,
            DamageEnergyType DamageType = DamageEnergyType.Magic,
            IEnumerable<(AbilityProjectileType type, BlueprintProjectileReference[] projectiles)>? Projectiles = null);

        internal static ConditionalList GetOnHitEffect(IEnumerable<EssenceEffect> essenceEffects)
        {
            var conditionalEffects = new ConditionalList();
            conditionalEffects.Operation = Operation.Or;

            conditionalEffects.IfFalse.Add(GetEldritchBlastDamage());

            foreach (var ee in essenceEffects)
            {
                var esl = ee.EssenceBuff.EnsureComponent<EldritchBlastEssenceComponent>();

                esl.EquivalentSpellLevel = Math.Max(esl.EquivalentSpellLevel, ee.EquivalentSpellLevel);

                var buffCondition = Conditions.ContextConditionCasterHasFact(condition =>
                    condition.m_Fact = ee.EssenceBuff.ToReference<BlueprintUnitFactReference>());

                var onHit = GameActions.Conditional(casterHasBuff =>
                {
                    casterHasBuff.ConditionsChecker.Add(buffCondition);

                    casterHasBuff.IfTrue.Add(GetEldritchBlastDamage(ee.DamageType));
                    casterHasBuff.IfTrue.Add(ee.OnHitActions().ToArray());
                });

                conditionalEffects.Conditionals.Add((ee.EssenceBuff.ToReference<BlueprintBuffReference>(), onHit));
            }

            return conditionalEffects;
        }

        internal static BlueprintAbility AddBlastComponents(
            BlueprintAbility ability,
            int equivalentSpellLevel,
            BlueprintFeatureReference ebRankFeature,
            IEnumerable<EssenceEffect> essenceEffects,
            Action<AbilityDeliverProjectile>? initProjectile = null)
        {
            ability.AddComponent<ArcaneSpellFailureComponent>();

            ability.AddComponent(new EldritchBlastCalculateSpellLevel(equivalentSpellLevel));

            ability.AddComponent(GetContextRankConfig(ebRankFeature));

            var runAction = ability.EnsureComponent<AbilityEffectRunAction>();

            //var conditionalEffects = new ConditionalList();
            //conditionalEffects.Operation = Operation.Or;

            //conditionalEffects.IfFalse.Add(GetEldritchBlastDamage());

            AbilityDeliverProjectileVariant? projectiles = null;
            if (initProjectile is not null)
            {
                projectiles = ability.AddComponent<AbilityDeliverProjectileVariant>(initProjectile);
            }

            foreach (var ee in essenceEffects)
            {
                //var esl = ee.EssenceBuff.EnsureComponent<EldritchBlastEssenceComponent>();

                //esl.EquivalentSpellLevel = Math.Max(esl.EquivalentSpellLevel, ee.EquivalentSpellLevel);

                var buffCondition = Conditions.ContextConditionCasterHasFact(condition =>
                    condition.m_Fact = ee.EssenceBuff.ToReference<BlueprintUnitFactReference>());

                //var onHit = GameActions.Conditional(casterHasBuff =>
                //{
                //    casterHasBuff.ConditionsChecker.Add(buffCondition);

                //    casterHasBuff.IfTrue.Add(GetEldritchBlastDamage(ee.DamageType));
                //    casterHasBuff.IfTrue.Add(ee.OnHitActions().ToArray());
                //});

                //conditionalEffects.Conditionals.Add((ee.EssenceBuff.ToReference<BlueprintBuffReference>(), onHit));

                if (projectiles is null) continue;
                if (ee.Projectiles is null ||
                    ee.Projectiles
                        .Where(p => p.type == projectiles.Type)
                        .Select(p => p.projectiles)
                        .FirstOrDefault() is not { } p)
                    continue;

                var variant = new AbilityDeliverProjectileVariant.Variant();
                variant.ConditionsChecker.Add(buffCondition);
                variant.Projectiles = p;

                projectiles.Variants.Add(variant);
            }

            runAction.AddActions(GetOnHitEffect(essenceEffects));

            return ability;
        }
    }

    internal class ConditionalList : GameAction
    {
        public List<(BlueprintBuffReference, Conditional)> Conditionals = new();

        public Operation Operation = Operation.Or;
        public ActionList IfTrue = new();
        public ActionList IfFalse = new();

        public override string GetCaption() => $"{nameof(ConditionalList)} ({Owner}, {name})";

        public ConditionalList() : base()
        {
        }

        public override void RunAction()
        {
            var results = new List<(Conditional, bool result)>();

            foreach (var (buff, conditional) in Conditionals)
            {
                if (conditional.ConditionsChecker.Check())
                {
                    //MicroLogger.Debug(() => $"{buff} passed");

                    results.Add((conditional, true));
                    conditional.IfTrue.Run();
                }
                else
                {
                    //MicroLogger.Debug(() => $"{buff} failed");

                    results.Add((conditional, false));
                    conditional.IfFalse.Run();
                }
            }

            switch (Operation)
            {
                case Operation.And:
                    if (results.Select(x => x.result).All(Functional.Identity))
                    {
                        IfTrue.Run();
                        return;
                    }
                    break;

                case Operation.Or:
                    if (results.Select(x => x.result).Any(Functional.Identity))
                    {
                        IfTrue.Run();
                        return;
                    }
                    break;
            }

            IfFalse.Run();
        }
    }

    internal class AbilityDeliverProjectileVariant : AbilityDeliverProjectile
    {
        public List<Variant> Variants = new();

        [JsonIgnore]
        private BlueprintProjectileReference[]? defaultProjectiles;

        public BlueprintProjectileReference[] Default => defaultProjectiles ??= base.m_Projectiles;

        public BlueprintProjectileReference[] GetProjectiles() => Variants
            .Where(v => v.Check())
            .Select(v => v.Projectiles)
            .FirstOrDefault() ?? Default;

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            defaultProjectiles ??= base.m_Projectiles;

            using (context.GetDataScope(target))
                m_Projectiles = GetProjectiles();

            return base.Deliver(context, target);
        }

        public class Variant : ContextAction
        {
            public ConditionsChecker ConditionsChecker = MicroWrath.Default.ConditionsChecker;

            public BlueprintProjectileReference[]? Projectiles;

            public override string GetCaption() => $"{this}";
            public override void RunAction() => result = ConditionsChecker.Check();

            [JsonIgnore]
            private bool result;
            public bool Check()
            {
                RunAction();
                return result;
            }
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
            if (context.MaybeOwner is not { } owner) return;

            if (owner.Buffs.Enumerable
                .Where(b => b.IsTurnedOn)
                .SelectMany(b => b.Blueprint.ComponentsArray.OfType<EldritchBlastEssenceComponent>())
                .FirstOrDefault() is { } essence)
                this.SpellLevel = Math.Max(essence.EquivalentSpellLevel, this.BaseEquivalentSpellLevel);
            else this.SpellLevel = this.BaseEquivalentSpellLevel;
        }
    }

    internal class EldritchBlastEssenceComponent : UnitFactComponentDelegate
    {
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
    }

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
}
