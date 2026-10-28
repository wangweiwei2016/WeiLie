using UnityEngine;
using UnityEngine.UI;

namespace Hunt.UI
{
    /// <summary>组队成员槽位 UI - 头像+昵称+队长标识+自己退出按钮+空位</summary>
    public class TeamMemberSlotUI : MonoBehaviour
    {
        public Image avatarImage;
        public Text nicknameText;
        public GameObject leaderTag;
        public Button leaveBtn;
        public GameObject emptyMask;

        public void SetData(long playerId, bool isLeader, bool isSelf)
        {
            if (emptyMask != null) emptyMask.SetActive(false);
            if (leaderTag != null) leaderTag.SetActive(isLeader);

            long myId = Hunt.Core.PlayerDataManager.Instance.CurrentPlayerId;
            var p = Hunt.Core.PlayerDataManager.Instance.GetPlayer(playerId);
            if (nicknameText != null)
                nicknameText.text = p?.Nickname ?? (playerId == myId ? "我" : $"玩家_{playerId}");

            if (leaveBtn != null) leaveBtn.gameObject.SetActive(isSelf && !isLeader);
        }

        public void SetEmpty()
        {
            if (emptyMask != null) emptyMask.SetActive(true);
            if (leaderTag != null) leaderTag.SetActive(false);
            if (nicknameText != null) nicknameText.text = "";
            if (leaveBtn != null) leaveBtn.gameObject.SetActive(false);
        }
    }
}
