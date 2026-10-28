using System;
using System.Collections.Generic;
using Hunt.Config;
using Hunt.Model;

namespace Hunt.Core
{
    /// <summary>
    /// 组队管理器
    /// 处理组队邀请、队伍合并（4种场景）、系统自动补齐
    /// </summary>
    public class TeamManager
    {
        public static TeamManager Instance { get; } = new TeamManager();

        private readonly Dictionary<long, HuntTeam> _teams = new Dictionary<long, HuntTeam>();
        private readonly Dictionary<long, long> _playerTeamMap = new Dictionary<long, long>();
        private readonly Dictionary<long, HuntInvite> _pendingInvites = new Dictionary<long, HuntInvite>();
        private long _nextTeamId = 1;
        private long _nextInviteId = 1;

        public bool TeamBuildingLocked { get; private set; }

        public event Action<long> OnTeamUpdated;    // TeamId
        public event Action<long> OnInviteReceived;  // 被邀请者 PlayerId

        public int TeamMateLimit
            => HuntActivityManager.Instance.GlobalCfg?.teamMateNum ?? 4;

        // ============ 查询 ============

        public HuntTeam GetTeam(long teamId)
            => _teams.TryGetValue(teamId, out var t) ? t : null;

        public HuntTeam GetPlayerTeam(long playerId)
        {
            if (_playerTeamMap.TryGetValue(playerId, out var tid))
                return GetTeam(tid);
            return null;
        }

        public bool IsInTeam(long playerId) => _playerTeamMap.ContainsKey(playerId);

        public int GetTeamMemberCount(long teamId)
            => GetTeam(teamId)?.MemberIds.Count ?? 0;

        public List<HuntTeam> GetAllTeams() => new List<HuntTeam>(_teams.Values);

        // ============ 组队阶段 ============

        public void OnTeamBuildingStart()
        {
            TeamBuildingLocked = false;
        }

        public void OnHuntingStart()
        {
            TeamBuildingLocked = true;
        }

        // ============ 队伍操作 ============

        public HuntTeam CreateTeam(long leaderId)
        {
            var team = new HuntTeam { TeamId = _nextTeamId++, LeaderId = leaderId };
            team.MemberIds.Add(leaderId);
            _teams[team.TeamId] = team;
            _playerTeamMap[leaderId] = team.TeamId;
            OnTeamUpdated?.Invoke(team.TeamId);
            return team;
        }

        public void LeaveTeam(long playerId)
        {
            if (TeamBuildingLocked) return;
            if (!_playerTeamMap.TryGetValue(playerId, out var teamId)) return;

            var team = _teams[teamId];
            team.MemberIds.Remove(playerId);
            _playerTeamMap.Remove(playerId);

            if (team.LeaderId == playerId && team.MemberIds.Count > 0)
                team.LeaderId = team.MemberIds[0];

            if (team.MemberIds.Count == 0)
                _teams.Remove(teamId);

            OnTeamUpdated?.Invoke(teamId);
        }

        // ============ 邀请系统 ============

        public long SendInvite(long inviterId, long targetId)
        {
            if (inviterId == targetId) return 0;
            if (TeamBuildingLocked) return 0;

            var invite = new HuntInvite
            {
                InviteId = _nextInviteId++,
                InviterId = inviterId,
                TargetId = targetId,
                Status = InviteStatus.Pending,
                CreateTime = ServerTime.NowMs,
                ExpireTime = ServerTime.NowMs + 5 * 60 * 1000
            };
            _pendingInvites[invite.InviteId] = invite;
            OnInviteReceived?.Invoke(targetId);
            return invite.InviteId;
        }

        /// <summary>核心：接受邀请 → 按文档 4 种场景处理队伍合并</summary>
        public (bool success, string reason) AcceptInvite(long inviteId, long accepterId)
        {
            if (!_pendingInvites.TryGetValue(inviteId, out var invite))
                return (false, "邀请不存在");

            if (invite.TargetId != accepterId) return (false, "非本人邀请");
            if (invite.Status != InviteStatus.Pending) return (false, "邀请已处理");

            if (TeamBuildingLocked)
            {
                invite.Status = InviteStatus.Invalid;
                return (false, "组队阶段已结束");
            }

            long inviterId = invite.InviterId;
            var inviterTeam  = GetPlayerTeam(inviterId);
            var accepterTeam = GetPlayerTeam(accepterId);

            // 同队直接成功
            if (inviterTeam != null && accepterTeam != null &&
                inviterTeam.TeamId == accepterTeam.TeamId)
            {
                invite.Status = InviteStatus.Accepted;
                return (true, "已在同一队伍");
            }

            // 双方都有队 → 合并
            if (inviterTeam != null && accepterTeam != null)
            {
                int merged = inviterTeam.MemberIds.Count + accepterTeam.MemberIds.Count;
                if (merged > TeamMateLimit)
                {
                    invite.Status = InviteStatus.Invalid;
                    return (false, "合并后超过队伍人数上限");
                }
                foreach (var m in accepterTeam.MemberIds)
                    AddMemberToTeam(inviterTeam, m);
                _teams.Remove(accepterTeam.TeamId);
                invite.Status = InviteStatus.Accepted;
                OnTeamUpdated?.Invoke(inviterTeam.TeamId);
                return (true, "队伍合并成功");
            }

            // 邀请方有队 + 被邀方无队 → 被邀方加入
            if (inviterTeam != null && accepterTeam == null)
            {
                if (inviterTeam.MemberIds.Count >= TeamMateLimit)
                {
                    invite.Status = InviteStatus.Invalid;
                    return (false, "邀请方队伍已满");
                }
                AddMemberToTeam(inviterTeam, accepterId);
                invite.Status = InviteStatus.Accepted;
                OnTeamUpdated?.Invoke(inviterTeam.TeamId);
                return (true, "加入成功");
            }

            // 邀请方无队 + 被邀方有队 → 邀请方加入被邀方队
            if (inviterTeam == null && accepterTeam != null)
            {
                if (accepterTeam.MemberIds.Count >= TeamMateLimit)
                {
                    invite.Status = InviteStatus.Invalid;
                    return (false, "队伍已满");
                }
                AddMemberToTeam(accepterTeam, inviterId);
                invite.Status = InviteStatus.Accepted;
                OnTeamUpdated?.Invoke(accepterTeam.TeamId);
                return (true, "加入对方队伍成功");
            }

            // 双方都没队 → 建队
            if (inviterTeam == null && accepterTeam == null)
            {
                var t = CreateTeam(inviterId);
                AddMemberToTeam(t, accepterId);
                invite.Status = InviteStatus.Accepted;
                return (true, "组队成功");
            }

            return (false, "未知错误");
        }

        public void RejectInvite(long inviteId)
        {
            if (_pendingInvites.TryGetValue(inviteId, out var inv))
                inv.Status = InviteStatus.Rejected;
        }

        public List<HuntInvite> GetPlayerInvites(long playerId)
        {
            var list = new List<HuntInvite>();
            foreach (var kv in _pendingInvites)
            {
                if (kv.Value.TargetId == playerId && kv.Value.Status == InviteStatus.Pending)
                    list.Add(kv.Value);
            }
            return list;
        }

        private void AddMemberToTeam(HuntTeam team, long playerId)
        {
            if (team.MemberIds.Contains(playerId)) return;
            team.MemberIds.Add(playerId);
            _playerTeamMap[playerId] = team.TeamId;
        }

        public void UpdateTick()
        {
            long now = ServerTime.NowMs;
            var toRemove = new List<long>();
            foreach (var kv in _pendingInvites)
            {
                if (kv.Value.Status == InviteStatus.Pending && kv.Value.ExpireTime < now)
                {
                    kv.Value.Status = InviteStatus.Expired;
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var id in toRemove) _pendingInvites.Remove(id);
        }
    }

    public enum InviteStatus
    {
        Pending  = 0,
        Accepted = 1,
        Rejected = 2,
        Expired  = 3,
        Invalid  = 4
    }

    public class HuntInvite
    {
        public long InviteId;
        public long InviterId;
        public long TargetId;
        public InviteStatus Status;
        public long CreateTime;
        public long ExpireTime;
    }
}
