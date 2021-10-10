using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BarrierMoving : BarrierMove
{
    protected override void MoveToStart()
    {
        _rig
        .DOMove(StartPoint.position, EndRoadTime)
        .SetDelay(EndDelay)
        .SetEase(EndEase)
        .OnComplete(() => {
            if (!OneMove && Activated)
            {
                MoveToEnd();
                isMoveToEnd = true;
            }
        });
    }
    protected override void MoveToEnd()
    {
        _rig
            .DOMove(EndPoint.position, StartRoadTime)
            .SetDelay(StartDelay)
            .SetEase(StartEase)
            .OnComplete(() => {
                if (!OneMove && Activated)
                {
                    MoveToStart();
                    isMoveToEnd = false;
                }
            });
    }
}
