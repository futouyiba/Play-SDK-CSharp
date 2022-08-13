using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeanCloud.Play
{
    public class Cushion : MonoBehaviour
    {
        public int cushionId;
        public int seatId;

        // Start is called before the first frame update
        void Start()
        {
            var seat = transform.parent.GetComponent<Seat>();
            seatId = seat.seatId;
        }

    }
}
