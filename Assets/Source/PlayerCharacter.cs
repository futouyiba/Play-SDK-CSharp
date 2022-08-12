using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace LeanCloud.Play
{
    public class PlayerCharacter:MonoBehaviour
    {
        public void MoveTo(Vector3 destination)
        {
            var navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.destination = destination;
            navMeshAgent.stoppingDistance = 0.2f;
        }

        private void OnTriggerEnter(Collider other)
        {
            var button = other.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener( () =>
                {
                    MultiplayerMgr.Instance.client;
                    
                    MultiplayerMgr.Instance.BroadcastEvent(EventDefine.EVENT_APPLY_MOVE, );
                } );
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var button = other.GetComponentInChildren<Button>();
            
        }
    }
}