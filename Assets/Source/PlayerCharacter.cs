using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace LeanCloud.Play
{
    public class PlayerCharacter:MonoBehaviour
    {
        public int cachedActorId;
        
        public int ComputedActorId => MultiplayerMgr.Instance.PlayerCharacters.ContainsValue(this)
            ? MultiplayerMgr.Instance.PlayerCharacters.Single(x => x.Value == this).Key
            : -1;

        public Player PlayerModel => MultiplayerMgr.Instance.client.Room.GetPlayer(ComputedActorId);
        
        public void SendMoveFromClick(Vector3 destination)
        {
            var actorId = MultiplayerMgr.Instance.client.Player.ActorId;
            var position = destination; //todo this should be fucking removed.
            var eventData = new PlayObject()
            {
                {"actorId", actorId},
                {MultiplayerMgr.POSITION, "Hello World"},
                {"x", position.x},
                {"y", position.y},
                {"z", position.z}
            };
                    
            MultiplayerMgr.Instance.BroadcastEvent(EventType.EVENT_APPLY_FOR_MOVE, eventData).Start();
        }
        
        public void ReceiveMoveTo(Vector3 destination)
        {
            var navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.destination = destination;
            navMeshAgent.stoppingDistance = 0.2f;
        }

        private void OnTriggerEnter(Collider cushionCollider)
        {
            if (!PlayerModel.IsLocal) return;
            
            var cushion = cushionCollider.GetComponent<Cushion>();
            if (cushion == null) return;
            
            cushion.transform.GetChild(0).gameObject.SetActive(true);
            
            var button = cushionCollider.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener( () =>
                {
                    var eventData = new PlayObject()
                    {
                        { "cushionId", cushion.cushionId },
                        { "seatId", cushion.seatId }
                    };
                    var options = new SendEventOptions()
                    {
                        ReceiverGroup = ReceiverGroup.MasterClient
                    };
                    
                } );
            }
        }

        private void OnTriggerExit(Collider cushionCollider)
        {
            if (!PlayerModel.IsLocal) return;
            
            var cushion = cushionCollider.GetComponent<Cushion>();
            if (cushion == null) return;
            var button = cushionCollider.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }                
            cushion.transform.GetChild(0).gameObject.SetActive(false);
            
        
        }

        public void GoCushionApproved(int seatId, int cushionId)
        {
            
        }
    }
}