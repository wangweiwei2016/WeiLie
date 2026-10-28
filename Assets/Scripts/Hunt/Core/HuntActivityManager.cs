using System;
using Hunt.Config;

namespace Hunt.Core
{
    /// <summary>
    /// 活动生命周期管理器 - 单例
    /// 控制活动阶段流转、时间驱动、阶段事件派发
    /// </summary>
    public class HuntActivityManager
    {
        public static HuntActivityManager Instance { get; } = new HuntActivityManager();

        public ActHuntGlobalCfg GlobalCfg { get; private set; }
        public HuntActivityStage CurrentStage { get; private set; } = HuntActivityStage.None;

        /// <summary>阶段变更事件：新阶段</summary>
        public event Action<HuntActivityStage> OnStageChanged;

        public void Init(ActHuntGlobalCfg cfg)
        {
            GlobalCfg = cfg;
            UpdateStage();
        }

        /// <summary>
        /// 每帧/定时调用，检查并推进阶段
        /// </summary>
        public void UpdateStage()
        {
            long now = ServerTime.NowMs;
            HuntActivityStage nextStage = DetermineStage(now);

            if (nextStage != CurrentStage)
            {
                var prev = CurrentStage;
                CurrentStage = nextStage;
                OnStageChanged?.Invoke(nextStage);

                // 阶段进入时触发特定逻辑
                switch (nextStage)
                {
                    case HuntActivityStage.TeamBuilding:
                        OnEnterTeamBuilding();
                        break;
                    case HuntActivityStage.Hunting:
                        OnEnterHunting();
                        break;
                    case HuntActivityStage.Ended:
                        OnEnterEnded();
                        break;
                }
            }
        }

        private HuntActivityStage DetermineStage(long now)
        {
            if (GlobalCfg == null) return HuntActivityStage.None;

            if (now < GlobalCfg.teamStartTime) return HuntActivityStage.None;

            // 单人模式跳过组队阶段
            if (GlobalCfg.gameType == 1)
            {
                if (now < GlobalCfg.activityStartTime) return HuntActivityStage.TeamBuilding;
            }
            else
            {
                if (now < GlobalCfg.teamEndTime) return HuntActivityStage.TeamBuilding;
            }

            if (now < GlobalCfg.activityEndTime) return HuntActivityStage.Hunting;
            return HuntActivityStage.Ended;
        }

        public long GetRemainingMs()
        {
            if (GlobalCfg == null) return 0;
            long target = CurrentStage switch
            {
                HuntActivityStage.TeamBuilding => GlobalCfg.teamEndTime,
                HuntActivityStage.Hunting       => GlobalCfg.activityEndTime,
                HuntActivityStage.None          => GlobalCfg.teamStartTime,
                _ => 0
            };
            return Math.Max(0, target - ServerTime.NowMs);
        }

        // ============ 阶段进入回调 ============
        private void OnEnterTeamBuilding()
        {
            TeamManager.Instance?.OnTeamBuildingStart();
        }

        private void OnEnterHunting()
        {
            TeamManager.Instance?.OnHuntingStart();
            HuntRoomManager.Instance?.OnHuntingStart();
            ScoreManager.Instance?.OnHuntingStart();
            ItemManager.Instance?.GrantInitialItems(PlayerDataManager.Instance.CurrentPlayerId);
        }

        private void OnEnterEnded()
        {
            SettlementManager.Instance?.Finalize();
        }
    }

    /// <summary>服务器时间工具 - 客户端与服务器的时间差校准</summary>
    public static class ServerTime
    {
        public static long OffsetMs { get; set; } = 0;

        public static long NowMs
            => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + OffsetMs;
    }
}
