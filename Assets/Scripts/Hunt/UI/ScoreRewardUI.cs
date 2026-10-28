using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hunt.Config;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>
    /// §5.6 积分奖励界面
    /// - 普通档位奖励：团队积分解锁
    /// - 循环奖励：达最高档后每 finalCycleScore 分循环
    /// - 三态：未解锁(隐藏按钮) / 未领取(可领) / 已领取(置灰)
    /// </summary>
    public class ScoreRewardUI : MonoBehaviour
    {
        [Header("普通奖励区域")]
        public Transform normalRewardRoot;
        public GameObject normalRewardItemPrefab;

        [Header("循环奖励区域")]
        public Text cycleStatusText;
        public Image cycleProgressBar;
        public Text cycleProgressText;
        public Button cycleClaimBtn;

        [Header("顶部信息")]
        public Text teamScoreText;
        public Button closeBtn;

        private Font _fallbackFont;

        private void OnEnable()
        {
            BindUI();
            _fallbackFont = Hunt.Utils.HuntFontHelper.SafeFont;

            ScoreManager.Instance.OnTeamScoreChanged += (_, _) => Refresh();
            ScoreRewardManager.Instance.OnRewardClaimed += _ => Refresh();

            Refresh();
        }

        /// <summary>显式绑定按钮事件 —— 必须在 Bootstrap 字段赋值完后调用</summary>
        public void BindUI()
        {
            if (closeBtn != null)      { closeBtn.onClick.RemoveAllListeners(); closeBtn.onClick.AddListener(() => gameObject.SetActive(false)); }
            if (cycleClaimBtn != null) { cycleClaimBtn.onClick.RemoveAllListeners(); cycleClaimBtn.onClick.AddListener(OnClaimCycle); }
        }

        private void OnDisable()
        {
            if (closeBtn != null)      closeBtn.onClick.RemoveAllListeners();
            if (cycleClaimBtn != null) cycleClaimBtn.onClick.RemoveAllListeners();
            ScoreManager.Instance.OnTeamScoreChanged -= (_, _) => Refresh();
            ScoreRewardManager.Instance.OnRewardClaimed -= _ => Refresh();
        }

        private void Refresh()
        {
            long myId = PlayerDataManager.Instance.CurrentPlayerId;

            if (teamScoreText != null)
                teamScoreText.text = $"团队积分: {ScoreManager.Instance.GetTeamScoreByPlayer(myId)}";

            RefreshNormalRewards(myId);
            RefreshCycle(myId);
        }

        private void RefreshNormalRewards(long myId)
        {
            if (normalRewardRoot == null) return;
            foreach (Transform c in normalRewardRoot) Destroy(c.gameObject);

            var cfgs = ConfigLoader.Instance.GetScoreRewards();
            var claimed = ScoreRewardManager.Instance.GetClaimedNormalSet(myId);
            int teamScore = ScoreManager.Instance.GetTeamScoreByPlayer(myId);

            foreach (var cfg in cfgs)
            {
                var go = Instantiate(normalRewardItemPrefab, normalRewardRoot);
                var item = go.GetComponent<ScoreRewardItemUI>();
                if (item != null)
                {
                    bool unlocked = teamScore >= cfg.score;
                    bool claimedThis = claimed.Contains(cfg.id);
                    item.SetData(cfg, unlocked, claimedThis, _fallbackFont);
                    item.OnClaimClicked += (c) =>
                    {
                        ScoreRewardManager.Instance.ClaimNormalReward(myId, c.id);
                        Refresh();
                    };
                }
            }
        }

        private void RefreshCycle(long myId)
        {
            var globalCfg = HuntActivityManager.Instance.GlobalCfg;
            if (globalCfg == null) return;

            var (completed, progress) = ScoreRewardManager.Instance.GetCycleProgress(myId);
            var claimed = ScoreRewardManager.Instance.GetClaimedCycles(myId);
            bool canClaim = claimed.Count < completed;

            if (cycleStatusText != null)
            {
                cycleStatusText.font = _fallbackFont;
                cycleStatusText.text = completed > 0
                    ? $"循环奖励: 已完成 {completed} 轮 / 已领取 {claimed.Count} 轮"
                    : "循环奖励: 需先达成普通最高档";
            }
            if (cycleProgressBar != null) cycleProgressBar.fillAmount = progress;
            if (cycleProgressText != null)
            {
                cycleProgressText.font = _fallbackFont;
                cycleProgressText.text = completed > 0
                    ? $"下一轮进度 {progress * 100:0}%"
                    : "等待团队积分提升";
            }
            if (cycleClaimBtn != null)
            {
                cycleClaimBtn.gameObject.SetActive(completed > 0);
                cycleClaimBtn.interactable = canClaim;
            }
        }

        private void OnClaimCycle()
        {
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            var (completed, _) = ScoreRewardManager.Instance.GetCycleProgress(myId);
            var claimed = ScoreRewardManager.Instance.GetClaimedCycles(myId);
            int targetRound = claimed.Count + 1;
            if (targetRound <= completed)
                ScoreRewardManager.Instance.ClaimCycleReward(myId, targetRound);
            Refresh();
        }
    }

    public class ScoreRewardItemUI : MonoBehaviour
    {
        public Text descText;
        public Text scoreText;
        public Text rewardText;
        public Button claimBtn;
        public Text claimedText;
        public Image unlockedMask;
        public ActHuntScoreRewardCfg Data;

        public System.Action<ActHuntScoreRewardCfg> OnClaimClicked;

        private void Awake()
        {
            if (claimBtn != null) claimBtn.onClick.AddListener(() => OnClaimClicked?.Invoke(Data));
        }

        private void OnDestroy()
        {
            if (claimBtn != null) claimBtn.onClick.RemoveAllListeners();
        }

        public void SetData(ActHuntScoreRewardCfg cfg, bool unlocked, bool claimed, Font fallbackFont)
        {
            Data = cfg;
            if (descText != null)   { descText.font   = fallbackFont; descText.text   = cfg.desc; }
            if (scoreText != null)  { scoreText.font  = fallbackFont; scoreText.text  = $"团队积分 ≥ {cfg.score}"; }
            if (rewardText != null) { rewardText.font = fallbackFont; rewardText.text = cfg.reward; }

            if (claimBtn != null)    claimBtn.gameObject.SetActive(unlocked && !claimed);
            if (claimedText != null) claimedText.gameObject.SetActive(claimed);
            if (unlockedMask != null) unlockedMask.gameObject.SetActive(!unlocked);
        }
    }
}
