using System;
using Hunt.Config;
using Hunt.Model;
using UnityEngine;

namespace Hunt.Core
{
    /// <summary>
    /// 攻击控制器
    /// 管理三种攻击模式：普通长按 / 狂暴（速度翻倍）/ 锁定（自动追踪）
    /// </summary>
    public class AttackController
    {
        public static AttackController Instance { get; } = new AttackController();

        private float _attackTimer;
        private bool _isAttacking;
        private bool _isFiring;
        private bool _isLocking;
        private HuntMonsterInstance _lockTarget;
        private long _fireEndTime;
        private long _lockEndTime;

        private int CurrentAttackIntervalMs
        {
            get
            {
                var cfg = HuntActivityManager.Instance.GlobalCfg;
                if (cfg == null) return 500;
                int interval = cfg.attackInterval;
                if (_isFiring)
                    interval = Math.Max(interval / 2, cfg.minAttackInterval);
                return interval;
            }
        }

        public event Action OnAttackTriggered;
        public event Action<KillResult> OnKillResult;

        // ============ 外部控制 ============

        public void StartContinuousAttack() => _isAttacking = true;
        public void StopContinuousAttack()  => _isAttacking = false;

        public void UseFireItem()
        {
            var cfg = HuntActivityManager.Instance.GlobalCfg;
            if (cfg == null) return;
            if (!ItemManager.Instance.ConsumeItem(PlayerDataManager.Instance.CurrentPlayerId, cfg.fireItemID)) return;
            _isFiring = true;
            _fireEndTime = ServerTime.NowMs + cfg.itemDuration;
        }

        public void UseLockItem(HuntMonsterInstance target)
        {
            var cfg = HuntActivityManager.Instance.GlobalCfg;
            if (cfg == null) return;
            if (!ItemManager.Instance.ConsumeItem(PlayerDataManager.Instance.CurrentPlayerId, cfg.lockItemID)) return;
            _isLocking = true;
            _lockTarget = target;
            _lockEndTime = ServerTime.NowMs + cfg.itemDuration;
        }

        public void ClearLock()
        {
            _isLocking = false;
            _lockTarget = null;
        }

        public bool IsFiring => _isFiring;
        public bool IsLocking => _isLocking;
        public HuntMonsterInstance LockTarget => _lockTarget;

        // ============ 每帧驱动 ============

        public void Update(float deltaTime, long playerId, HuntRoom room, int digimonQuality)
        {
            // 道具到期
            long now = ServerTime.NowMs;
            if (_isFiring && now >= _fireEndTime) _isFiring = false;
            if (_isLocking && now >= _lockEndTime) ClearLock();

            if (room == null) return;

            // 锁定模式 → 自动攻击锁定目标（或自动续锁）
            if (_isLocking)
            {
                if (_lockTarget == null || !_lockTarget.IsAlive)
                {
                    _lockTarget = FindNextLockTarget(room);
                    if (_lockTarget == null) { ClearLock(); return; }
                }
                PerformAttack(playerId, room, _lockTarget, digimonQuality);
                return;
            }

            // 普通长按模式
            if (!_isAttacking) return;

            _attackTimer += deltaTime * 1000f;
            if (_attackTimer >= CurrentAttackIntervalMs)
            {
                _attackTimer = 0;
                var target = FindNearestMonster(room);
                if (target != null)
                    PerformAttack(playerId, room, target, digimonQuality);
            }
        }

        // ============ 内部 ============

        private void PerformAttack(long playerId, HuntRoom room,
                                   HuntMonsterInstance target, int digimonQuality)
        {
            var result = HuntRoomManager.Instance.ExecuteAttack(
                playerId, room.MultipleId, target, digimonQuality);
            OnAttackTriggered?.Invoke();
            if (result != null) OnKillResult?.Invoke(result);
        }

        private HuntMonsterInstance FindNearestMonster(HuntRoom room)
        {
            foreach (var m in room.Monsters)
                if (m.IsAlive) return m;
            return null;
        }

        private HuntMonsterInstance FindNextLockTarget(HuntRoom room)
        {
            if (room == null) return null;
            int preferType = _lockTarget?.Type ?? -1;

            // 先找同类型
            foreach (var m in room.Monsters)
                if (m.IsAlive && m.Type == preferType) return m;

            // 再找任意
            foreach (var m in room.Monsters)
                if (m.IsAlive) return m;

            return null;
        }
    }
}
