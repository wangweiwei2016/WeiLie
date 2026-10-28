using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hunt.Core;

namespace Hunt.UI
{
    /// <summary>§5.2.2 收到的邀请列表 - 接受/拒绝</summary>
    public class InviteListUI : MonoBehaviour
    {
        public Transform listRoot;
        public GameObject itemPrefab;
        public Text emptyText;

        public void Refresh()
        {
            if (listRoot == null || itemPrefab == null) return;
            foreach (Transform c in listRoot) Destroy(c.gameObject);

            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            var invites = TeamManager.Instance.GetPlayerInvites(myId);

            if (invites.Count == 0)
            {
                if (emptyText != null) emptyText.gameObject.SetActive(true);
                return;
            }
            if (emptyText != null) emptyText.gameObject.SetActive(false);

            foreach (var inv in invites)
            {
                var go = Instantiate(itemPrefab, listRoot);
                var item = go.GetComponent<InviteItemUI>();
                item?.SetData(inv, this);
            }
        }
    }

    public class InviteItemUI : MonoBehaviour
    {
        public Text nicknameText;
        public Button acceptBtn;
        public Button rejectBtn;

        private HuntInvite _invite;
        private InviteListUI _parent;

        private void Awake()
        {
            if (acceptBtn != null) acceptBtn.onClick.AddListener(OnAccept);
            if (rejectBtn != null) rejectBtn.onClick.AddListener(OnReject);
        }

        private void OnDestroy()
        {
            if (acceptBtn != null) acceptBtn.onClick.RemoveListener(OnAccept);
            if (rejectBtn != null) rejectBtn.onClick.RemoveListener(OnReject);
        }

        public void SetData(HuntInvite invite, InviteListUI parent)
        {
            _invite = invite;
            _parent = parent;
            if (nicknameText != null) nicknameText.text = $"来自: 玩家_{invite.InviterId}";
        }

        private void OnAccept()
        {
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            var (ok, reason) = TeamManager.Instance.AcceptInvite(_invite.InviteId, myId);
            if (!ok) Debug.LogWarning($"[InviteItem] 接受失败: {reason}");
            _parent?.Refresh();
        }

        private void OnReject()
        {
            TeamManager.Instance.RejectInvite(_invite.InviteId);
            _parent?.Refresh();
        }
    }
}
