using UnityEngine;
using UnityEngine.UI;
using Hunt.Config;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>
    /// §5.7 排行榜界面
    /// - 4 种榜单：团队区服 / 团队全服 / 个人区服 / 个人全服
    /// - 团队榜显示队员数，个人榜只显示自己
    /// - 顶部展示自己当前排名 + 奖励
    /// </summary>
    public class RankUI : MonoBehaviour
    {
        [Header("Tab 切换")]
        public Button teamServerBtn;
        public Button teamAllServerBtn;
        public Button personalServerBtn;
        public Button personalAllServerBtn;

        [Header("排行榜内容")]
        public Transform rankListRoot;
        public GameObject rankItemPrefab;

        [Header("我的排名")]
        public Text myRankText;
        public Text myScoreText;
        public Text myRewardText;

        [Header("关闭")]
        public Button closeBtn;

        private HuntRankType _currentType = HuntRankType.PersonalServer;
        private Font _fallbackFont;

        private void OnEnable()
        {
            BindUI();
            _fallbackFont = Hunt.Utils.HuntFontHelper.SafeFont;

            RankManager.Instance.OnRankUpdated += OnRankUpdated;

            bool isPersonal = HuntActivityManager.Instance.GlobalCfg?.gameType == 1;
            _currentType = isPersonal ? HuntRankType.PersonalServer : HuntRankType.TeamServer;
            RequestAndRefresh();
        }

        /// <summary>显式绑定按钮事件 —— 必须在 Bootstrap 字段赋值完后调用</summary>
        public void BindUI()
        {
            if (teamServerBtn != null)       teamServerBtn.onClick.RemoveAllListeners();
            if (teamAllServerBtn != null)    teamAllServerBtn.onClick.RemoveAllListeners();
            if (personalServerBtn != null)   personalServerBtn.onClick.RemoveAllListeners();
            if (personalAllServerBtn != null) personalAllServerBtn.onClick.RemoveAllListeners();
            if (closeBtn != null)             closeBtn.onClick.RemoveAllListeners();

            if (teamServerBtn != null)       teamServerBtn.onClick.AddListener(() => SwitchType(HuntRankType.TeamServer));
            if (teamAllServerBtn != null)    teamAllServerBtn.onClick.AddListener(() => SwitchType(HuntRankType.TeamAllServer));
            if (personalServerBtn != null)   personalServerBtn.onClick.AddListener(() => SwitchType(HuntRankType.PersonalServer));
            if (personalAllServerBtn != null) personalAllServerBtn.onClick.AddListener(() => SwitchType(HuntRankType.PersonalAllServer));
            if (closeBtn != null)             closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            if (teamServerBtn != null)       teamServerBtn.onClick.RemoveAllListeners();
            if (teamAllServerBtn != null)    teamAllServerBtn.onClick.RemoveAllListeners();
            if (personalServerBtn != null)   personalServerBtn.onClick.RemoveAllListeners();
            if (personalAllServerBtn != null) personalAllServerBtn.onClick.RemoveAllListeners();
            if (closeBtn != null)             closeBtn.onClick.RemoveAllListeners();
            RankManager.Instance.OnRankUpdated -= OnRankUpdated;
        }

        private void SwitchType(HuntRankType type)
        {
            _currentType = type;
            RequestAndRefresh();
        }

        /// <summary>主动请求刷新排行榜（初始化 / Tab 切换时调用）</summary>
        private void RequestAndRefresh()
        {
            RankManager.Instance.RequestRank((int)_currentType);
            // RequestRank 内部触发 OnRankUpdated 事件后会自动调 RefreshUI，
            // 这里不用再手动调，避免重复渲染
        }

        /// <summary>仅从缓存刷新 UI（由 OnRankUpdated 事件回调）</summary>
        private void RefreshUI()
        {
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            var myRank = RankManager.Instance.GetMyRank((int)_currentType, myId);

            if (myRankText != null)
            {
                myRankText.font = _fallbackFont;
                myRankText.text = myRank != null ? $"我的排名：#{myRank.Rank}" : "未上榜";
            }
            if (myScoreText != null)
            {
                myScoreText.font = _fallbackFont;
                myScoreText.text = myRank != null ? $"积分：{myRank.Score}" : "";
            }
            if (myRewardText != null)
            {
                myRewardText.font = _fallbackFont;
                string desc = myRank != null
                    ? RankManager.Instance.GetRankRewardDesc((int)_currentType, myRank.Rank) : "";
                myRewardText.text = string.IsNullOrEmpty(desc) ? "暂无奖励" : $"奖励：{desc}";
            }

            if (rankListRoot != null && rankItemPrefab != null)
            {
                foreach (Transform c in rankListRoot) Destroy(c.gameObject);
                var list = RankManager.Instance.GetRank((int)_currentType);
                bool isTeam = _currentType == HuntRankType.TeamServer || _currentType == HuntRankType.TeamAllServer;

                for (int i = 0; i < Mathf.Min(list.Count, 50); i++)
                {
                    var go = Instantiate(rankItemPrefab, rankListRoot);
                    var item = go.GetComponent<RankItemUI>();
                    item?.SetData(list[i].Rank, list[i].Id, list[i].Score, isTeam, _fallbackFont);
                }
            }
        }

        private void OnRankUpdated(int rankType)
        {
            // 只在当前显示的榜单类型被更新时才刷新 UI，避免 Tab 切换时的残影
            if (rankType == (int)_currentType) RefreshUI();
        }
    }

    public class RankItemUI : MonoBehaviour
    {
        public Text rankText;
        public Text nameText;
        public Text scoreText;
        public Text memberCountText;

        public void SetData(int rank, long id, int score, bool isTeam, Font fallbackFont)
        {
            if (rankText != null)
            {
                rankText.font = fallbackFont;
                rankText.text = $"#{rank}";
            }
            if (nameText != null)
            {
                nameText.font = fallbackFont;
                nameText.text = isTeam ? $"队伍_{id}" : $"玩家_{id}";
            }
            if (scoreText != null)
            {
                scoreText.font = fallbackFont;
                scoreText.text = score.ToString();
            }
            if (memberCountText != null)
            {
                memberCountText.font = fallbackFont;
                if (isTeam)
                {
                    var team = Hunt.Core.TeamManager.Instance.GetTeam(id);
                    memberCountText.text = team != null ? $"队员 {team.MemberIds.Count} 人" : "";
                }
                else
                {
                    memberCountText.text = "";
                }
            }
        }
    }
}
