using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>§5.2.1 推荐玩家列表（简化版，实际按 4 优先级排序）</summary>
    public class RecommendListUI : MonoBehaviour
    {
        public Transform listRoot;
        public GameObject itemPrefab;
        public Text emptyText;

        // Mock 推荐池
        private readonly List<MockPlayerInfo> _mockPool = new()
        {
            new MockPlayerInfo { Id = 2001, Nickname = "驯兽师小明", TeamCount = 2 },
            new MockPlayerInfo { Id = 2002, Nickname = "巴达兽",     TeamCount = 3 },
            new MockPlayerInfo { Id = 2003, Nickname = "迪路兽",     TeamCount = 1 },
            new MockPlayerInfo { Id = 2004, Nickname = "V仔兽",      TeamCount = 2 },
        };

        public void Refresh()
        {
            if (listRoot == null || itemPrefab == null) return;
            foreach (Transform c in listRoot) Destroy(c.gameObject);

            // 简化：队伍人数越多越优先邀请
            _mockPool.Sort((a, b) => b.TeamCount.CompareTo(a.TeamCount));

            if (_mockPool.Count == 0)
            {
                if (emptyText != null) emptyText.gameObject.SetActive(true);
                return;
            }
            if (emptyText != null) emptyText.gameObject.SetActive(false);

            foreach (var p in _mockPool)
            {
                var go = Instantiate(itemPrefab, listRoot);
                var item = go.GetComponent<RecommendItemUI>();
                item?.SetData(p.Id, p.Nickname);
            }
        }
    }

    public class RecommendItemUI : MonoBehaviour
    {
        public Text nicknameText;
        public Button inviteBtn;
        private long _targetId;

        private void Awake()
        {
            if (inviteBtn != null) inviteBtn.onClick.AddListener(OnInvite);
        }

        private void OnDestroy()
        {
            if (inviteBtn != null) inviteBtn.onClick.RemoveListener(OnInvite);
        }

        public void SetData(long id, string nickname)
        {
            _targetId = id;
            if (nicknameText != null) nicknameText.text = nickname;
            if (inviteBtn != null) inviteBtn.interactable = true;
        }

        private void OnInvite()
        {
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            long invId = TeamManager.Instance.SendInvite(myId, _targetId);
            if (invId > 0 && inviteBtn != null) inviteBtn.interactable = false;
        }
    }

    public class MockPlayerInfo { public long Id; public string Nickname; public int TeamCount; }
}
