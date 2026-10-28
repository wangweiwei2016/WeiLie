using UnityEngine;
using UnityEngine.UI;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>
    /// §5.2 组队界面总控
    /// 内含推荐/邀请/好友 三个 Tab + 队伍成员显示 + 倒计时 + 开始狩猎
    /// </summary>
    public class TeamBuildingUI : MonoBehaviour
    {
        [Header("Tab 按钮")]
        public Button tabRecommendBtn;
        public Button tabInviteBtn;
        public Button tabFriendBtn;
        public GameObject panelRecommend;
        public GameObject panelInvite;
        public GameObject panelFriend;

        [Header("队伍展示")]
        public Transform memberRoot;
        public GameObject memberSlotPrefab;
        public Button leaveTeamBtn;
        public GameObject leaveConfirmDialog;

        [Header("倒计时 & 开始")]
        public Text countdownText;
        public Button startBtn;

        [Header("子 UI")]
        public RecommendListUI recommendUI;
        public InviteListUI inviteUI;
        public FriendListUI friendUI;

        private void OnEnable()
        {
            BindUI();
            TeamManager.Instance.OnTeamUpdated      += _ => RefreshTeamUI();
            TeamManager.Instance.OnInviteReceived   += _ => ShowInvite();
            HuntActivityManager.Instance.OnStageChanged += OnStageChanged;

            RefreshAll();
            ShowRecommend();
        }

        /// <summary>显式绑定按钮事件 —— 必须在 Bootstrap 字段赋值完后调用</summary>
        public void BindUI()
        {
            if (tabRecommendBtn != null) tabRecommendBtn.onClick.RemoveAllListeners();
            if (tabInviteBtn != null)    tabInviteBtn.onClick.RemoveAllListeners();
            if (tabFriendBtn != null)    tabFriendBtn.onClick.RemoveAllListeners();
            if (leaveTeamBtn != null)    leaveTeamBtn.onClick.RemoveAllListeners();
            if (startBtn != null)        startBtn.onClick.RemoveAllListeners();

            if (tabRecommendBtn != null) tabRecommendBtn.onClick.AddListener(ShowRecommend);
            if (tabInviteBtn != null)    tabInviteBtn.onClick.AddListener(ShowInvite);
            if (tabFriendBtn != null)    tabFriendBtn.onClick.AddListener(ShowFriend);
            if (leaveTeamBtn != null)    leaveTeamBtn.onClick.AddListener(OnLeaveClicked);
            if (startBtn != null)        startBtn.onClick.AddListener(OnStartClicked);
        }

        private void OnDisable()
        {
            TeamManager.Instance.OnTeamUpdated      -= _ => RefreshTeamUI();
            TeamManager.Instance.OnInviteReceived   -= _ => ShowInvite();
            HuntActivityManager.Instance.OnStageChanged -= OnStageChanged;

            if (tabRecommendBtn != null) tabRecommendBtn.onClick.RemoveListener(ShowRecommend);
            if (tabInviteBtn != null)    tabInviteBtn.onClick.RemoveListener(ShowInvite);
            if (tabFriendBtn != null)    tabFriendBtn.onClick.RemoveListener(ShowFriend);
            if (leaveTeamBtn != null)    leaveTeamBtn.onClick.RemoveListener(OnLeaveClicked);
            if (startBtn != null)        startBtn.onClick.RemoveListener(OnStartClicked);
        }

        // ============ 主流程 ============

        private void RefreshAll()
        {
            RefreshTeamUI();
            recommendUI?.Refresh();
            inviteUI?.Refresh();
            friendUI?.Refresh();
        }

        private void RefreshTeamUI()
        {
            if (memberRoot == null || memberSlotPrefab == null) return;
            foreach (Transform c in memberRoot) Destroy(c.gameObject);

            var team = TeamManager.Instance.GetPlayerTeam(PlayerDataManager.Instance.CurrentPlayerId);
            int limit = TeamManager.Instance.TeamMateLimit;
            int count = team?.MemberIds.Count ?? 0;

            if (team != null)
            {
                foreach (var mid in team.MemberIds)
                {
                    var go = Instantiate(memberSlotPrefab, memberRoot);
                    var slot = go.GetComponent<TeamMemberSlotUI>();
                    bool isLeader = team.LeaderId == mid;
                    bool isSelf   = mid == PlayerDataManager.Instance.CurrentPlayerId;
                    slot?.SetData(mid, isLeader, isSelf);
                }
            }
            for (int i = count; i < limit; i++)
            {
                var go = Instantiate(memberSlotPrefab, memberRoot);
                go.GetComponent<TeamMemberSlotUI>()?.SetEmpty();
            }

            if (leaveTeamBtn != null)
                leaveTeamBtn.gameObject.SetActive(count > 1);

            if (TeamManager.Instance.TeamBuildingLocked)
                gameObject.SetActive(false);
        }

        // ============ Tab 切换 ============

        private void ShowRecommend()
        {
            if (panelRecommend != null) panelRecommend.SetActive(true);
            if (panelInvite != null)    panelInvite.SetActive(false);
            if (panelFriend != null)    panelFriend.SetActive(false);
        }

        private void ShowInvite()
        {
            if (panelRecommend != null) panelRecommend.SetActive(false);
            if (panelInvite != null)    panelInvite.SetActive(true);
            if (panelFriend != null)    panelFriend.SetActive(false);
            inviteUI?.Refresh();
        }

        private void ShowFriend()
        {
            if (panelRecommend != null) panelRecommend.SetActive(false);
            if (panelInvite != null)    panelInvite.SetActive(false);
            if (panelFriend != null)    panelFriend.SetActive(true);
            friendUI?.Refresh();
        }

        // ============ 按钮 ============

        private void OnLeaveClicked()
        {
            if (leaveConfirmDialog != null) leaveConfirmDialog.SetActive(true);
        }

        public void ConfirmLeaveTeam()
        {
            TeamManager.Instance.LeaveTeam(PlayerDataManager.Instance.CurrentPlayerId);
            if (leaveConfirmDialog != null) leaveConfirmDialog.SetActive(false);
        }

        private void OnStartClicked()
        {
            HuntUIManager.Instance.OpenDigimonSelect();
            gameObject.SetActive(false);
        }

        private void OnStageChanged(Hunt.Config.HuntActivityStage stage)
        {
            if (stage == Hunt.Config.HuntActivityStage.Hunting)
                TeamManager.Instance.OnHuntingStart();
        }

        private void Update()
        {
            if (countdownText == null) return;
            long remain = HuntActivityManager.Instance.GetRemainingMs();
            System.TimeSpan ts = System.TimeSpan.FromMilliseconds(remain);
            countdownText.text = $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
        }
    }
}
