using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LeanCloud.Play
{
    public class MultiplayerMgr : MonoBehaviour
    {
        public const string PREFAB_ID = "prefabId";
        public const string SEAT_DATA = "SeatData";
        public const string POSITION = "position";
        public const string SEAT_ID = "seatId";
        public const string CUSHION_ID = "cushionId";
        public const string ACTOR_ID = "actorId";

        public static MultiplayerMgr Instance { get; private set; }
        public Client client;
        public string _roomName = "xiugou";
        
        public Dictionary<int, PlayerCharacter> PlayerCharacters = new Dictionary<int, PlayerCharacter>();

        private async void Start()
        {
            this.Instance = this;
            LeanCloud.Common.Logger.LogDelegate = (level, log) =>
            {
                if (level == LeanCloud.Common.LogLevel.Debug)
                {
                    Debug.LogFormat("[DEBUG] {0}", log);
                }
                else if (level == LeanCloud.Common.LogLevel.Warn)
                {
                    Debug.LogFormat("[WARN] {0}", log);
                }
                else if (level == LeanCloud.Common.LogLevel.Error)
                {
                    Debug.LogFormat("[ERROR] {0}", log);
                }
            };
            
            // App Id
            var APP_ID = "g2b0X6OmlNy7e4QqVERbgRJR-gzGzoHsz";
            // App Key
            var APP_KEY = "CM91rNV8cPVHKraoFQaopMVT";
            // 域名
            var playServer = "https://g2b0x6om.lc-cn-n1-shared.com";

            var userId = System.Environment.MachineName;
            client = new Client(APP_ID, APP_KEY, userId, playServer: playServer);
            await client.Connect();
            Debug.Log("connected to lean play");
            await client.JoinOrCreateRoom(_roomName);
            Debug.Log("joined room");
            
            client.OnPlayerRoomJoined += OnPlayerRoomJoined;
            client.OnPlayerCustomPropertiesChanged += OnPlayerCustomPropertiesChanged;
            client.OnCustomEvent += OnCustomEvent;
            client.OnPlayerRoomLeft += OnPlayerRoomLeft;
            client.OnMasterSwitched += OnMasterSwitched;
            client.OnRoomCustomPropertiesChanged += OnRoomCustomPropertiesChanged;
        }

        private void OnRoomCustomPropertiesChanged(PlayObject obj)
        {
            foreach (var kvPair in obj)
            {
                Debug.Log("room property changed: " + kvPair.Key + " " + kvPair.Value);
                switch (kvPair.Key)
                {
                    case SEAT_DATA:
                        
                        break;
                }
            }
        }

        private void OnMasterSwitched(Player obj)
        {
            Debug.LogFormat("master switched to {0}", obj.UserId);
        }

        private void OnPlayerRoomLeft(Player player)
        {
            Debug.Log($"player {player.UserId} left room");
            
        }

        private void OnCustomEvent(byte arg1, PlayObject arg2, int arg3)
        {
            throw new NotImplementedException();
        }

        private void OnPlayerCustomPropertiesChanged(Player player, PlayObject changedProps)
        {
            foreach (var kv in changedProps)
            {
                Debug.Log($"prop changed {kv.Key} {kv.Value}, of player {player.UserId}");

                switch (kv.Key)
                {
                    case POSITION:
                        // var pos = Vector3. kv.Value;
                        // PlayerCharacters[player.ActorId].transform.position = pos;
                        break;
                }
            }
            if (player.IsLocal)
            {
                Debug.Log("above player is local");
            }

            
        }
        
        public async Task BroadcastEvent(byte eventId, PlayObject eventData)
        {
            var options = new SendEventOptions()
            {
                ReceiverGroup = ReceiverGroup.All
            };
            try {
                // 发送自定义事件
                await client.SendEvent(eventId, eventData, options);
            } catch (PlayException e) {
                // 发送事件错误
                Debug.LogErrorFormat("{0}, {1}", e.Code, e.Detail);
            }
        }

        private void OnPlayerRoomJoined(Player newPlayer)
        {
            Debug.Log($"new player: { newPlayer.UserId }");
            if (client.Player.IsMaster)
            {
                var playerList = client.Room.PlayerList;
                for (int i = 0; i < playerList.Count; i++)
                {
                    var player = playerList[i];
                    var props = new PlayObject();

                    if (player.IsMaster)
                    {
                        props.Add(PREFAB_ID, 4);
                    }
                    else
                    {
                        props.Add(PREFAB_ID, Random.Range(0, 3));
                    }
                    props.Add(POSITION, new Vector3(0, 0, 0));
                }
            }
        }

        private async Task OnApplicationQuit()
        {
            try
            {
                await client.LeaveRoom();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}