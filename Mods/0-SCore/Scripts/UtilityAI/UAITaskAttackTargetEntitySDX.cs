﻿using UnityEngine;

// using this namespace is necessary for Utilities AI Tasks
//       <task class="AttackTargetEntitySDX, SCore" action_index="0" /> 
// The game adds UAI.UAITask to the class name for discover.
namespace UAI
{
    public class UAITaskAttackTargetEntitySDX : UAITaskAttackTargetEntity
    {
        // Default to action 0.
        private int _actionIndex = 0;
        private string _buffThrottle = "buffReload2";
        private int _targetTimeout = 20;

        protected override void initializeParameters()
        {
            base.initializeParameters();
            if (Parameters.ContainsKey("action_index")) _actionIndex = int.Parse(Parameters["action_index"]);
            if (Parameters.ContainsKey("buff_throttle")) _buffThrottle = Parameters["buff_throttle"];
            if (Parameters.ContainsKey("target_timeout")) _targetTimeout = int.Parse(Parameters["target_timeout"]);
        }

        public override void Start(Context _context)
        {
            // Reset crouching.
            SCoreUtils.SetCrouching(_context);
            this.attackTimeout = _context.Self.GetAttackTimeoutTicks();

            EntityAlive entityAlive = UAIUtils.ConvertToEntityAlive(_context.ActionData.Target);
            if (entityAlive != null)
            {
                _context.Self.SetLookPosition( entityAlive.getHeadPosition());
                _context.Self.RotateTo(entityAlive, 30f, 30f);
                _context.Self.SetAttackTarget(entityAlive, 1200);
            }

            if (_context.ActionData.Target.GetType() == typeof(Vector3))
            {
                Vector3 vector = (Vector3)_context.ActionData.Target;
                // Center the vector so its looking directly at the middle.
                vector = EntityUtilities.CenterPosition(vector);
                _context.Self.IsBreakingBlocks = true;
                _context.Self.SetLookPosition(vector );
                _context.Self.RotateTo(vector.x, vector.y, vector.z, 45f, 45f);
                EntityUtilities.Stop(_context.Self.entityId);
            }

            _context.ActionData.Started = true;
            _context.ActionData.Executing = true;
        }

        public override void Stop(Context _context)
        {
            _context.Self.IsBreakingBlocks = false;
            _context.Self.IsBreakingDoors = false;
            base.Stop(_context);
        }

        public override void Update(Context _context)
        {

            if (!_context.Self.onGround || _context.Self.Climbing)
                return;

            Vector3 position = Vector3.zero;

            var entityAlive = UAIUtils.ConvertToEntityAlive(_context.ActionData.Target);
            if (entityAlive != null)
            {
                // Am I the target? Check if I have an attack or revenge target
                if (entityAlive.entityId == _context.Self.entityId)
                {
                    //  Am I being attacked? 
                    var attacker = EntityUtilities.GetAttackOrRevengeTarget(_context.Self.entityId) as EntityAlive;
                    if (attacker == null)
                    {
                        entityAlive = attacker;
                    }
                }
                if (entityAlive.IsDead())
                {
                    Stop(_context);
                    return;
                }

                _context.Self.SetLookPosition(entityAlive.getHeadPosition());
                _context.Self.RotateTo(entityAlive, 30f, 30f);

                position = entityAlive.position;
            }

            if (_context.ActionData.Target is Vector3 vector)
            {
                position = vector;
                _context.Self.SetLookPosition( position );
                var targetType = GameManager.Instance.World.GetBlock(new Vector3i(position));
                if (targetType.Equals(BlockValue.Air))
                {
                    this.Stop(_context);
                    return;
                }
            }

            // Reloading
            if (_context.Self.Buffs.HasBuff(_buffThrottle))
                return;


            // Check the range on the item action
            ItemActionRanged.ItemActionDataRanged itemActionData = null;
            var itemAction = _context.Self.inventory.holdingItem.Actions[_actionIndex];
            var distance = ((itemAction != null) ? Utils.FastMax(0.8f, itemAction.Range) : 1.095f);
            if (itemAction is ItemActionRanged itemActionRanged)
            {
                itemActionData = _context.Self.inventory.holdingItemData.actionData[_actionIndex] as ItemActionRanged.ItemActionDataRanged;
                if (itemActionData != null)
                {
                    var range = itemActionRanged.GetRange(itemActionData);
                    distance = Utils.FastMax(0.8f, range);

                }
            }
            var minDistance = distance * distance;
            var a = position - _context.Self.position;

            //not within range ?
            if (a.sqrMagnitude > minDistance)
            {
                // If we are out of range, it's probably a very small amount, so this will step forward, but not if we are staying.
                if (EntityUtilities.GetCurrentOrder(_context.Self.entityId) != EntityUtilities.Orders.Stay)
                    _context.Self.moveHelper.SetMoveTo(position, true);
                Stop(_context);
            }

            attackTimeout--;
            if (attackTimeout > 0)
                return;

            this.attackTimeout = _context.Self.GetAttackTimeoutTicks();

            // Action Index = 1 is Use, 0 is Attack.
            switch (_actionIndex)
            {
                case 0:
                    if (!_context.Self.Attack(false)) return;
                    _context.Self.Attack(true);
                    break;
                case 1:
                    if (!_context.Self.Use(false)) return;
                    _context.Self.Use(true);
                    break;
                default:
                    var entityAliveSDX = _context.Self as EntityAliveSDX;
                    if (entityAliveSDX)
                    {
                        if (!entityAliveSDX.ExecuteAction(false, _actionIndex)) return;
                        entityAliveSDX.ExecuteAction(true, _actionIndex);
                    }
                    break;
            }
        }
    }
}