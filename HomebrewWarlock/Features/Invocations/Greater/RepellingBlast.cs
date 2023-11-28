using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;

using MicroWrath.BlueprintInitializationContext;

using Owlcat.Runtime.Core.Utils;

namespace HomebrewWarlock.Features.Invocations.Greater
{
    internal class RepellingBlastAction : ContextAction
    {
        internal ContextDiceValue Distance = Default.ContextDiceValue;
        internal ActionList EndedPrematurely = Default.ActionList;
        internal AbilitySharedValue DamageDiceShared;

        public override string GetCaption() => $"Repelling blast: {this.Distance?.Calculate(base.Context)}";

        public override void RunAction()
        {
            if (base.Context is null ||
                base.Target is null ||
                base.Context.MaybeCaster is not { } caster)
            {
                return;
            }

            if (GameHelper.IsAttackingGreenNPC(caster, base.Target.Unit)) return;
            
            var targetPoint = base.Target.Point;
            var direction = targetPoint - caster.Position;
            var distance = this.Distance.Calculate(base.Context);
            var distanceFeet = distance * 5;

            // "World" units appear to be meters
            var vector = direction.normalized * (distanceFeet.Feet().Meters);

            var expectedDestination = base.Target.Unit.Position + vector;
            var obstaclePosition = ObstacleAnalyzer.TraceAlongNavmesh(base.Target.Unit.Position, expectedDestination);

            var distance2D = GeometryUtils.Distance2D(base.Target.Unit.Position, expectedDestination);
            var obstacleDistance2D = GeometryUtils.Distance2D(base.Target.Unit.Position, obstaclePosition);

            // Note: Feet.Value is a float -> integer cast
            var distance2DFeet = distance2D.MetersToFeet().Value;
            var obstacleDistance2DFeet = obstacleDistance2D.MetersToFeet().Value;

            MicroLogger.Debug(sb =>
            {
                sb.AppendLine($"Yeet {distance2DFeet} ({distanceFeet})ft:");
                sb.AppendLine($"  distance2D: {distance2D}");
                sb.AppendLine($"  distance2DFeet: {distance2DFeet}");
                sb.AppendLine($"  vector.magnitude: {vector.magnitude}");
                sb.AppendLine($"  {base.Target.Unit.Position} -> {expectedDestination}:");
                sb.Append($"    {(expectedDestination - base.Target.Unit.Position).magnitude}");
            });

            if (distanceFeet < 5) return;

            if (!base.Target.Unit.Descriptor.State.Prone.Active)
            {
                this.Target.Unit.State.Prone.ShouldBeActive = true;

                if (base.Target.Unit.CanBeKnockedOff())
                    EventBus.RaiseEvent<IKnockOffHandler>(h => h.HandleKnockOff(caster, this.Target.Unit), true);
            }

            // UnitPartForceMove.Push distance unit = 5 feet
            var _ = base.Target.Unit.Ensure<UnitPartForceMove>().Push(direction, distance, false);

            base.Context[this.DamageDiceShared] = (obstacleDistance2DFeet + 1) / 10;

            if (distance2D > obstacleDistance2D)
            {
                MicroLogger.Debug(sb =>
                {
                    sb.AppendLine($"Yeet into obstacle {obstacleDistance2DFeet}ft");
                    sb.AppendLine($"  obstacleDistance2D: {obstacleDistance2D}");
                    sb.AppendLine($"  obstacleDistance2DFeet: {obstacleDistance2DFeet}");
                    sb.AppendLine($"  magnitdue: {(obstaclePosition - base.Target.Unit.Position).magnitude}");
                    sb.AppendLine($"  {base.Target.Unit.Position} -> {obstaclePosition}:");
                    sb.Append($"    {(obstaclePosition - base.Target.Unit.Position).magnitude}");
                });

                if ((obstacleDistance2DFeet + 1) > 10)
                    EndedPrematurely.Run();
            }
        }
    }

    internal static class RepellingBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Repelling Blast";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Essence</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 6" +
            Environment.NewLine +
            "This eldritch essence invocation allows you to change your eldritch blast into a repelling blast. " +
            "Any Medium or smaller creature struck by a repelling blast must make a Reflex save or be hurled " +
            "1d6×5 feet (1d6 squares) directly away from you and knocked prone by the energy of the attack." +
            Environment.NewLine +
            "If the creature strikes a solid object, it stops prematurely, taking 1d6 points of damage per 10 feet " +
            "hurled, and it is still knocked prone." +
            Environment.NewLine +
            "Movement from this blast does not provoke attacks of opportunity.";

        [LocalizedString]
        internal const string LocalizedSavingThrow = "Reflex negates";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var onHit = context.NewBlueprint<BlueprintAbility>(GeneratedGuid.Get("RepellingBlastYeet"))
                .Map(ability =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_RepellingBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Greater_RepellingBlast_Description;
                    ability.LocalizedSavingThrow = LocalizedStrings.Features_Invocations_Greater_RepellingBlast_LocalizedSavingThrow;
                    ability.m_Icon = Sprites.RepellingBlast;

                    ability.Hidden = true;

                    ability.AddComponent<AbilityEffectRunAction>(c =>
                    {
                        ability.AddInvocationComponents(6);
                        ability.Type = AbilityType.SpellLike;

                        ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                        ability.CanTargetEnemies = true;

                        c.SavingThrowType = SavingThrowType.Reflex;

                        var rpAction = new RepellingBlastAction();
                        
                        rpAction.Distance.DiceType = DiceType.D6;
                        rpAction.Distance.DiceCountValue = 1;
                        rpAction.DamageDiceShared = AbilitySharedValue.DamageBonus;

                        rpAction.EndedPrematurely.Add(GameActions.ContextActionDealDamage(a =>
                        {
                            a.DamageType.Type = DamageType.Untyped;
                            a.Value.DiceType = DiceType.D6;
                            a.Value.DiceCountValue.ValueType = ContextValueType.Shared;
                            a.Value.DiceCountValue.ValueShared = AbilitySharedValue.DamageBonus;
                        }));

                        c.Actions.Add(GameActions.ContextActionConditionalSaved(a =>
                        {
                            a.Failed.Add(rpAction);
                        }));
                    });

                    return ability;
                });

            var essenceBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("RepellingBlastEssenceBuff"))
                .Combine(onHit)
                .Map(bps =>
                {
                    var (buff, onHit) = bps;

                    buff.AddComponent<EldritchBlastEssence>(c =>
                    {
                        c.EquivalentSpellLevel = 6;

                        c.Actions.Add(GameActions.ContextActionCastSpell(a =>
                        {
                            a.m_Spell = onHit.ToReference();
                            a.MarkAsChild = true;
                        }));
                    });

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.StayOnDeath;

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("RepellingBlastToggleAbility"))
                .Combine(essenceBuff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_RepellingBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Greater_RepellingBlast_Description;
                    ability.m_Icon = Sprites.RepellingBlast;

                    ability.m_Buff = buff.ToReference();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("RepellingBlastFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c =>
                    {
                        c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() }; 
                    });

                    var prerequisite = feature.AddPrerequisiteFeature(
                        GeneratedGuid.EldritchBlastPrerequisiteFeature.ToMicroBlueprint<BlueprintFeature>());

                    prerequisite.HideInUI = true;

                    return feature;
                });

            return feature;
        }
    }
}
