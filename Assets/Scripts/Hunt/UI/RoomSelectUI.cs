using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hunt.Config;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>
    /// §5.4 猎场选择界面
    /// - 顶部：当前排行榜名次 + 奖励预览 + 奖励红点
    /// - 中央：猎场卡片列表（左右切换 + 分页指示器）
    /// - 底部：队伍数码兽展示 + 积分
    /// - 按钮：开始围猎 / 切换数码兽
    /// </summary>
    public class RoomSelectUI : MonoBehaviour
    {
        [Header("排行榜预览")]
        public Text rankText;
        public Text rankRewardPreviewText;
        public Button rankDetailBtn;
        public Button scoreRewardBtn;
        public GameObject scoreRewardRedDot;

        [Header("猎场列表")]
        public RectTransform roomScrollContent;
        public GameObject roomCardPrefab;
        public Button leftBtn;
        public Button rightBtn;
        public Transform dotIndicatorRoot;
        public GameObject dotPrefab;

        [Header("数码兽展示")]
        public Transform digimonDisplayRoot;
        public Button digimonSwitchBtn;

        [Header("开始围猎")]
        public Button startHuntBtn;

        private int _currentRoomIdx;
        private readonly List<RoomCardUI> _cards = new();
        private Font _fallbackFont;

        private void OnEnable()
        {
            BindUI();
            ScoreManager.Instance.OnPersonalScoreChanged += OnScoreChanged;
            ScoreManager.Instance.OnTeamScoreChanged     += OnScoreChanged;
            ScoreRewardManager.Instance.OnRewardClaimed += _ => RefreshAll();

            RefreshAll();
        }

        /// <summary>显式绑定按钮事件 —— 必须在 Bootstrap 字段赋值完后调用</summary>
        public void BindUI()
        {
            if (rankDetailBtn != null)    rankDetailBtn.onClick.RemoveAllListeners();
            if (scoreRewardBtn != null)   scoreRewardBtn.onClick.RemoveAllListeners();
            if (leftBtn != null)          leftBtn.onClick.RemoveAllListeners();
            if (rightBtn != null)         rightBtn.onClick.RemoveAllListeners();
            if (digimonSwitchBtn != null) digimonSwitchBtn.onClick.RemoveAllListeners();
            if (startHuntBtn != null)     startHuntBtn.onClick.RemoveAllListeners();

            if (rankDetailBtn != null)    rankDetailBtn.onClick.AddListener(() => HuntUIManager.Instance.OpenRank());
            if (scoreRewardBtn != null)   scoreRewardBtn.onClick.AddListener(() => HuntUIManager.Instance.OpenScoreReward());
            if (leftBtn != null)          leftBtn.onClick.AddListener(() => SwitchRoom(-1));
            if (rightBtn != null)         rightBtn.onClick.AddListener(() => SwitchRoom(1));
            if (digimonSwitchBtn != null) digimonSwitchBtn.onClick.AddListener(() => HuntUIManager.Instance.OpenDigimonSelect());
            if (startHuntBtn != null)     startHuntBtn.onClick.AddListener(OnStartHunt);
        }

        private void OnDisable()
        {
            if (rankDetailBtn != null)    rankDetailBtn.onClick.RemoveAllListeners();
            if (scoreRewardBtn != null)   scoreRewardBtn.onClick.RemoveAllListeners();
            if (leftBtn != null)          leftBtn.onClick.RemoveAllListeners();
            if (rightBtn != null)         rightBtn.onClick.RemoveAllListeners();
            if (digimonSwitchBtn != null) digimonSwitchBtn.onClick.RemoveAllListeners();
            if (startHuntBtn != null)     startHuntBtn.onClick.RemoveAllListeners();

            ScoreManager.Instance.OnPersonalScoreChanged -= OnScoreChanged;
            ScoreManager.Instance.OnTeamScoreChanged     -= OnScoreChanged;
            ScoreRewardManager.Instance.OnRewardClaimed -= _ => RefreshAll();
        }

        private void RefreshAll()
        {
            try
            {
                RefreshRankPreview();
                RefreshScoreRewardRedDot();
                RefreshRoomList();
                RefreshDigimonDisplay();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RoomSelectUI] RefreshAll 异常（可能是 Mock 数据/单例未就绪）: {e.Message}");
            }
        }

        private void RefreshRankPreview()
        {
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            int rankType = HuntActivityManager.Instance.GlobalCfg?.gameType == 1
                ? (int)HuntRankType.PersonalServer : (int)HuntRankType.TeamServer;

            RankManager.Instance.RequestRank(rankType);
            var entry = RankManager.Instance.GetMyRank(rankType, myId);

            if (rankText != null)
                rankText.text = entry != null ? $"当前排名：{entry.Rank}" : "未上榜";
            if (rankRewardPreviewText != null)
            {
                string desc = entry != null
                    ? RankManager.Instance.GetRankRewardDesc(rankType, entry.Rank) : "";
                rankRewardPreviewText.text = string.IsNullOrEmpty(desc) ? "暂无奖励" : desc;
            }
        }

        private void RefreshScoreRewardRedDot()
        {
            if (scoreRewardRedDot == null) return;
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            bool hasNormal = ScoreRewardManager.Instance.GetClaimableRewards(myId).Count > 0;
            var (completed, _) = ScoreRewardManager.Instance.GetCycleProgress(myId);
            bool hasCycle = ScoreRewardManager.Instance.GetClaimedCycles(myId).Count < completed;
            scoreRewardRedDot.SetActive(hasNormal || hasCycle);
        }

        private void RefreshRoomList()
        {
            if (roomScrollContent == null || roomCardPrefab == null || dotIndicatorRoot == null)
                return;

            foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
            _cards.Clear();
            foreach (Transform c in roomScrollContent) Destroy(c.gameObject);
            foreach (Transform c in dotIndicatorRoot) Destroy(c.gameObject);

            var cfgs = ConfigLoader.Instance.AllMultipleCfgs;
            if (cfgs.Count == 0) return;

            cfgs.Sort((a, b) => a.id.CompareTo(b.id));

            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            int totalTokens = ScoreManager.Instance.GetTotalTokensEarned(myId);

            for (int i = 0; i < cfgs.Count; i++)
            {
                var cfg = cfgs[i];
                var go = Instantiate(roomCardPrefab, roomScrollContent);
                var card = go.GetComponent<RoomCardUI>();
                if (card != null)
                {
                    bool unlocked = ConfigLoader.Instance.IsUnlocked(cfg.id, totalTokens);
                    float progress = ConfigLoader.Instance.GetUnlockProgress(cfg.id, totalTokens);
                    card.SetData(cfg, unlocked, progress);
                    _cards.Add(card);
                }
                var dotGo = Instantiate(dotPrefab, dotIndicatorRoot);
            }

            _currentRoomIdx = 0;
            UpdateCardHighlight();
        }

        private void SwitchRoom(int delta)
        {
            if (_cards.Count == 0) return;
            _currentRoomIdx = Mathf.Clamp(_currentRoomIdx + delta, 0, _cards.Count - 1);
            UpdateCardHighlight();
        }

        private void UpdateCardHighlight()
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].SetSelected(i == _currentRoomIdx);
            // 高亮 dot
            int idx = 0;
            foreach (Transform c in dotIndicatorRoot)
            {
                var img = c.GetComponent<Image>();
                if (img != null) img.color = idx == _currentRoomIdx ? Color.white : new Color(1, 1, 1, 0.3f);
                idx++;
            }
        }

        private void RefreshDigimonDisplay()
        {
            if (digimonDisplayRoot == null) return;
            foreach (Transform c in digimonDisplayRoot) Destroy(c.gameObject);

            var team = TeamManager.Instance.GetPlayerTeam(PlayerDataManager.Instance.CurrentPlayerId);
            if (team == null) return;

            _fallbackFont ??= Hunt.Utils.HuntFontHelper.SafeFont;

            foreach (var mid in team.MemberIds)
            {
                var p = PlayerDataManager.Instance.GetPlayer(mid);
                var go = new GameObject($"Displayer_{mid}");
                go.transform.SetParent(digimonDisplayRoot, false);
                var txt = go.AddComponent<Text>();
                txt.font = _fallbackFont;
                //txt.text = $"{p?.Nickname ?? mid}  积分:{ScoreManager.Instance.GetPersonalScore(mid)}";
                txt.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void OnScoreChanged(long _1, int _2) => RefreshRankPreview();

        private void OnStartHunt()
        {
            if (_cards.Count == 0) return;
            var cfg = ConfigLoader.Instance.GetMultipleCfg(_cards[_currentRoomIdx].ConfigId);
            if (cfg == null) return;

            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            int totalTokens = ScoreManager.Instance.GetTotalTokensEarned(myId);
            if (!ConfigLoader.Instance.IsUnlocked(cfg.id, totalTokens))
            {
                Debug.LogWarning("猎场未解锁");
                return;
            }

            HuntRoomManager.Instance.EnterRoom(myId, cfg.id, out string errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Debug.LogWarning($"进入猎场失败: {errorMsg}");
                return;
            }

            HuntUIManager.Instance.OpenHuntRoom();
            gameObject.SetActive(false);
        }
    }

    public class RoomCardUI : MonoBehaviour
    {
        public int ConfigId;
        public Text nameText;
        public Text multipleText;
        public Text unlockText;
        public Image lockedMask;
        public Image selectedFrame;

        public void SetData(HuntMultipleCfg cfg, bool unlocked, float unlockProgress)
        {
            ConfigId = cfg.id;
            if (nameText != null)     nameText.text     = cfg.name;
            if (multipleText != null) multipleText.text = ConfigLoader.FormatMultiple(cfg.multiple);
            if (lockedMask != null)   lockedMask.gameObject.SetActive(!unlocked);
            if (unlockText != null)
            {
                unlockText.text = unlocked
                    ? "已解锁"
                    : $"解锁需累计{cfg.unlock}代币 ({unlockProgress * 100:0}%)";
            }
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedFrame != null) selectedFrame.enabled = selected;
        }
    }
}
