using UnityEngine;
using UnityEngine.UI;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>
    /// §5.1 活动入口 + 红点
    /// 有可领取积分奖励 → 显示红点
    /// 点击按活动阶段打开对应面板
    /// </summary>
    public class HuntEntryController : MonoBehaviour
    {
        public Button entryBtn;
        public RedDotHelper redDot;

        /// <summary>
        /// 显式绑定按钮事件 —— 必须在所有 Inspector 字段赋值完后调用。
        /// Unity AddComponent<T>() 会立即同步触发 OnEnable，
        /// 但此时外部还没来得及给 entryBtn/redDot 赋值，
        /// 所以把 Button 相关的绑定从 OnEnable 拆出来放到这里。
        /// </summary>
        public void BindUI()
        {
            if (entryBtn != null)
            {
                entryBtn.onClick.RemoveAllListeners(); // 防重复
                entryBtn.onClick.AddListener(OnEntryClicked);
            }
        }

        private void OnEnable()
        {
            BindUI();
            // 这里只绑静态单例事件（不依赖 Inspector 字段赋值）
            ScoreRewardManager.Instance.OnRewardClaimed += _ => UpdateRedDot();
            ScoreManager.Instance.OnTeamScoreChanged   += (_, _) => UpdateRedDot();
        }

        private void OnDisable()
        {
            if (entryBtn != null) entryBtn.onClick.RemoveListener(OnEntryClicked);
            ScoreRewardManager.Instance.OnRewardClaimed -= _ => UpdateRedDot();
            ScoreManager.Instance.OnTeamScoreChanged   -= (_, _) => UpdateRedDot();
        }

        private void Start() { UpdateRedDot(); }

        private void OnEntryClicked()
        {
            var stage = HuntActivityManager.Instance.CurrentStage;
            switch (stage)
            {
                case Hunt.Config.HuntActivityStage.TeamBuilding:
                    HuntUIManager.Instance.OpenTeamBuilding();
                    break;
                case Hunt.Config.HuntActivityStage.Hunting:
                    // 已选过数码兽 → 进猎场选择；否则先进数码兽选择
                    long myId = PlayerDataManager.Instance.CurrentPlayerId;
                    var p = PlayerDataManager.Instance.GetPlayer(myId);
                    if (p != null && p.DigimonId > 0)
                        HuntUIManager.Instance.OpenRoomSelect();
                    else
                        HuntUIManager.Instance.OpenDigimonSelect();
                    break;
                case Hunt.Config.HuntActivityStage.Ended:
                    HuntUIManager.Instance.OpenRank();
                    break;
                default:
                    Debug.LogWarning($"[HuntEntryController] 当前阶段 {stage}，不处理点击");
                    break;
            }
        }

        private void UpdateRedDot()
        {
            if (redDot == null) return;
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            bool hasNormal = ScoreRewardManager.Instance.GetClaimableRewards(myId).Count > 0;
            var (completed, _) = ScoreRewardManager.Instance.GetCycleProgress(myId);
            bool hasCycle = ScoreRewardManager.Instance.GetClaimedCycles(myId).Count < completed;
            redDot.SetVisible(hasNormal || hasCycle);
        }
    }
}
