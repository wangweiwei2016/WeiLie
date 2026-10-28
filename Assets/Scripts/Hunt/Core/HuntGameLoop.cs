using UnityEngine;
using Hunt.Config;

namespace Hunt.Core
{
    /// <summary>
    /// Unity 游戏循环驱动
    /// 挂在场景中的空 GameObject 上（建议与 HuntUIManager 共用同一 GameObject）
    /// 每帧推进各管理器 Update
    /// </summary>
    public class HuntGameLoop : MonoBehaviour
    {
        public static HuntGameLoop Instance { get; private set; }

        [Header("调试配置（仅测试时用，正式从服务端拉取）")]
        public long TestPlayerId = 10001;
        public string TestNickname = "测试玩家";
        public bool AutoInitGlobalCfg = true;
        public int TestGameType = 1; // 1 单人 2 组队

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitTestData();
        }

        private void Update()
        {
            // 1. 活动阶段推进
            HuntActivityManager.Instance.UpdateStage();
            TeamManager.Instance.UpdateTick();

            // 2. 所有活跃房间的刷怪 & 怪物移动
            foreach (var room in HuntRoomManager.Instance.GetAllActiveRooms())
            {
                HuntRoomManager.Instance.RoomUpdate(room, Time.deltaTime);
            }

            // 3. 当前玩家的攻击循环
            long myId = PlayerDataManager.Instance.CurrentPlayerId;
            if (myId == 0) return;

            var myRoom = HuntRoomManager.Instance.GetPlayerRoom(myId);
            if (myRoom != null &&
                HuntActivityManager.Instance.CurrentStage == HuntActivityStage.Hunting)
            {
                int quality = PlayerDataManager.Instance.GetCurrentDigimonQuality(myId);
                AttackController.Instance.Update(Time.deltaTime, myId, myRoom, quality);
            }
        }

        private void InitTestData()
        {
            PlayerDataManager.Instance.SetCurrentPlayer(TestPlayerId, TestNickname);

            if (AutoInitGlobalCfg && HuntActivityManager.Instance.GlobalCfg == null)
            {
                HuntActivityManager.Instance.Init(new ActHuntGlobalCfg
                {
                    id                 = 1,
                    gameType           = TestGameType,
                    teamMateNum        = 4,
                    maxPlayersPerRoom  = 4,
                    teamStartTime      = ServerTime.NowMs - 60000,
                    teamEndTime        = ServerTime.NowMs - 30000,
                    activityStartTime  = ServerTime.NowMs - 10000,
                    activityEndTime    = ServerTime.NowMs + 3600000, // 1 小时后
                    BulletCost         = 10,
                    attackInterval     = 500,
                    minAttackInterval  = 200,
                    fireItemID         = 9001,
                    lockItemID         = 9002,
                    itemDuration       = 30000,
                    finalCycleScore    = 10000,
                    finalCycleReward   = "8001_10",
                    initItems          = "9001_1#9002_1",
                    rankMailId         = 1001,
                    scoreMailId        = 1002
                });
            }

            // 单人模式自动建队
            if (TestGameType == 1 && !TeamManager.Instance.IsInTeam(TestPlayerId))
                TeamManager.Instance.CreateTeam(TestPlayerId);

            // 初始发放道具（活动进入 Hunting 时也会触发）
            // ItemManager.Instance.GrantInitialItems(TestPlayerId);
        }
    }
}
