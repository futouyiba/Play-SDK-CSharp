using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LeanCloud.Play
{
    public class MultiplayerMgr : MonoBehaviour
    {
        public GameObject CharacterPrefab;

        public const string PREFAB_ID = "prefabId";
        
        /// <summary>
        /// the index for "who owns which seat, aka which sofa".
        /// </summary>
        public const string SEAT_OWNER_DATA = "SeatData";
        public const string POSITION = "position";
        public const string SEAT_ID = "seatId";
        public const string CUSHION_ID = "cushionId";

        public const string
            ACTOR_ID = "actorId"; // actorId is the player's id in the game, from what we see now it's got no use.

        public const string APPLIER = "applier";

        public static MultiplayerMgr Instance { get; private set; }
        public Client client;
        
        /// <summary>
        /// for 0814 we test it only in a single room for "xiugou"
        /// </summary>
        public string _roomName = "xiugou";

        public Dictionary<int, PlayerCharacter> PlayerCharacters = new Dictionary<int, PlayerCharacter>();

        private async void Start()
        {
            MultiplayerMgr.Instance = this;
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

            var userId = System.Environment.MachineName + Random.Range(0, 100);
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

            // init character of my self
            if (!client.Player.IsMaster)
            {
                return;
            }

            OnPlayerRoomJoined(client.Player);
            client.Room.CustomProperties.Add(SEAT_OWNER_DATA, new PlayObject());
        }

        private void OnRoomCustomPropertiesChanged(PlayObject changedProps)
        {
            if (changedProps.ContainsKey(SEAT_OWNER_DATA))
            {
                var seatOwnerData = changedProps.GetPlayObject(SEAT_OWNER_DATA);
                foreach (var kvPair in seatOwnerData)
                {
                    var seatId = (int)kvPair.Key;
                    var seatOwnerId = (int)kvPair.Value;
                    // deal with the circumstance that former seat owner get outside the seat.
                    var formerOwnerId = client.Room.CustomProperties.GetPlayObject(SEAT_OWNER_DATA).GetInt(seatId);
                    
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
            if (PlayerCharacters.ContainsKey(player.ActorId))
            {
                Destroy(PlayerCharacters[player.ActorId].gameObject);
                PlayerCharacters.Remove(player.ActorId);
            }
        }

        private void OnCustomEvent(byte eventType, PlayObject eventData, int senderId)
        {
            Debug.LogFormat("custom event {0} from {1}", eventType, senderId);
            switch (eventType)
            {
                // case EventType.EVENT_APPLY_FOR_MOVE:
                //     PlayerCharacters[senderId].ReceiveMoveTo(new Vector3(eventData.GetFloat("x"),
                //         eventData.GetFloat("y"), eventData.GetFloat("z")));
                //     break;
                case EventType.EVENT_APPLY_CUSHION:
                    if (!client.Player.IsMaster)
                    {
                        return;
                    }

                    var seatId = eventData.GetInt(SEAT_ID);
                    var cushionId = eventData.GetInt(CUSHION_ID);
                    if (client.Room.CustomProperties.GetPlayObject(SEAT_OWNER_DATA)
                        .TryGetInt(seatId, out int seatOwnerId))
                    {
                        if (seatOwnerId == senderId)
                        {
                            var alreadyHasPlayerOnCusion = client.Room.PlayerList.Exists(playerData =>
                            {
                                if (playerData.CustomProperties.TryGetInt(CUSHION_ID, out int oldSeatId)))
                                {
                                    if (oldSeatId == seatId)
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                        return false;      
                                }
                            });
                        }
                    }
                    else
                    {
                        client.Room.CustomProperties.GetPlayObject(SEAT_OWNER_DATA).Add(seatId, senderId);
                    }

                    break;
                case EventType.EVENT_APPROVE_REQUEST_OWNER:

                    break;

                case EventType.EVENT_APPROVE_CUSHION_RESULT:
                    if (!client.Player.IsMaster)
                    {
                        return;
                    }

                    if (senderId == client.Room.CustomProperties.GetPlayObject(SEAT_OWNER_DATA)
                            .GetInt(eventData.GetInt(SEAT_ID)))
                    {
                        PlayerCharacters[eventData.GetInt(APPLIER)].GoCushionApproved(eventData.GetInt(SEAT_ID),
                            eventData.GetInt(CUSHION_ID));
                    }

                    break;
                case EventType.EVENT_CONFIRMED_ENTER_CUSHION:

                    break;
            }
        }


        private void OnPlayerCustomPropertiesChanged(Player player, PlayObject changedProps)
        {
            if (changedProps.ContainsKey(PREFAB_ID))
            {
                // basically this should not happen.
                if (PlayerCharacters.ContainsKey(player.ActorId)) return;
                var newCharGo = GameObject.Instantiate(CharacterPrefab, new Vector3(
                    changedProps.GetFloat("x"),
                    changedProps.GetFloat("y"), changedProps.GetFloat("z")), Quaternion.identity);
                var playerCharacter = newCharGo.AddComponent<PlayerCharacter>();
                playerCharacter.cachedActorId = player.ActorId;
                PlayerCharacters.Add(player.ActorId, playerCharacter);
                newCharGo.GetComponentInChildren<TextMeshPro>().text = player.UserId;
                if (player.IsLocal)
                {
                    newCharGo.GetComponent<MeshRenderer>().material.color = Color.yellow;
                }

                return;
            }
            if (!PlayerCharacters.ContainsKey(player.ActorId)) return;

            if (changedProps.ContainsKey("x") || changedProps.ContainsKey("y") || changedProps.ContainsKey("z"))
            {
                // ReSharper disable once Unity.NoNullPropagation
                player.PlayerCharacter?.ReceiveMoveTo(new Vector3(player.CustomProperties.GetFloat("x"),
                    player.CustomProperties.GetFloat("y"), player.CustomProperties.GetFloat("z")));
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
            try
            {
                // 发送自定义事件
                await client.SendEvent(eventId, eventData, options);
            }
            catch (PlayException e)
            {
                // 发送事件错误
                Debug.LogErrorFormat("{0}, {1}", e.Code, e.Detail);
            }
        }

        private void OnPlayerRoomJoined(Player newPlayer)
        {
            Debug.Log($"new player: {newPlayer.UserId}");

            if (!client.Player.IsMaster) return;
            var props = new PlayObject()
            {
                {SEAT_ID, -1},
                {CUSHION_ID, -1}
            };

            if (newPlayer.IsMaster)
            {
                props.Add(PREFAB_ID, 4);
            }
            else
            {
                props.Add(PREFAB_ID, Random.Range(2, 3));
            }

            props.Add("x", Random.Range(-3f, 3f));
            props.Add("y", 1);
            props.Add("z", Random.Range(-3f, 3f));
            newPlayer.SetCustomProperties(props);
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