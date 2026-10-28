using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>§5.2.3 好友邀请列表 - 优先在线 + 队伍缺口</summary>
    public class FriendListUI : MonoBehaviour
    {
        public Transform listRoot;
        public GameObject itemPrefab;
        public Text emptyText;

        private readonly List<FriendInfo> _mockFriends = new()
        {
            new FriendInfo { Id = 3001, Nickname = "好友A", Online = true,  TeamCount = 2 },
            new FriendInfo { Id = 3002, Nickname = "好友B", Online = true,  TeamCount = 3 },
            new FriendInfo { Id = 3003, Nickname = "好友C", Online = false, TeamCount = 1 },
        };

        public void Refresh()
        {
            if (listRoot == null || itemPrefab == null) return;
            foreach (Transform c in listRoot) Destroy(c.gameObject);

            // 优先在线 + 队伍缺口大
            _mockFriends.Sort((a, b) =>
            {
                if (a.Online != b.Online) return a.Online ? -1 : 1;
                return b.TeamCount.CompareTo(a.TeamCount);
            });

            if (_mockFriends.Count == 0)
            {
                if (emptyText != null) emptyText.gameObject.SetActive(true);
                return;
            }
            if (emptyText != null) emptyText.gameObject.SetActive(false);

            foreach (var f in _mockFriends)
            {
                var go = Instantiate(itemPrefab, listRoot);
                go.GetComponent<FriendItemUI>()?.SetData(f);
            }
        }
    }

    public class FriendItemUI : MonoBehaviour
    {
        public Text nicknameText;
        public Text onlineStatusText;
        public Text teamCountText;
        public Button inviteBtn;

        private FriendInfo _friend;

        private void Awake()
        {
            if (inviteBtn != null) inviteBtn.onClick.AddListener(OnInvite);
        }

        private void OnDestroy()
        {
            if (inviteBtn != null) inviteBtn.onClick.RemoveListener(OnInvite);
        }

        public void SetData(FriendInfo friend)
        {
            _friend = friend;
            if (nicknameText != null)    nicknameText.text    = friend.Nickname;
            if (onlineStatusText != null) onlineStatusText.text = friend.Online ? "[在线]" : "[离线]";
            if (teamCountText != null)   teamCountText.text   = $"队伍 {friend.TeamCount}/4";
            if (inviteBtn != null)       inviteBtn.interactable = friend.Online;
        }

        private void OnInvite()
        {
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            long invId = TeamManager.Instance.SendInvite(myId, _friend.Id);
            if (invId > 0 && inviteBtn != null) inviteBtn.interactable = false;
        }
    }

    public class FriendInfo { public long Id; public string Nickname; public bool Online; public int TeamCount; }
}
