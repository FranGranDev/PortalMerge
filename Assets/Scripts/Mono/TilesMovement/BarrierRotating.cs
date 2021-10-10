using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BarrierRotating : BarrierMove
{

    protected override void MoveToStart()
    {
        _rig
            .DORotate(StartPoint.rotation.eulerAngles, EndRoadTime)
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
            .DORotate(EndPoint.rotation.eulerAngles, StartRoadTime)
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
