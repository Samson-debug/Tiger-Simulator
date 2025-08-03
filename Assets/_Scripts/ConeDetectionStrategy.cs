using UnityEngine;

public class ConeDetectionStrategy : IDetectionStrategy
{
    readonly float detectionAngle;
    readonly float detectionRadius;
        
    public ConeDetectionStrategy(float detectionAngle, float detectionRadius)
    {
        this.detectionAngle = detectionAngle;
        this.detectionRadius = detectionRadius;
    }
    
    public bool Execute(Transform player, Transform detector)
    {
        Vector3 vectorToPlayer = player.position - detector.position;
        float angleToPlayer = Vector3.Angle(detector.forward, vectorToPlayer);
            
        //if Player is (not in angle & in outer Radi) & ( not in outer Radi)
        // && Player is in outer Radi but not in angle
        if(angleToPlayer > detectionAngle/2 || vectorToPlayer.magnitude > detectionRadius)
            return false;
            
        return true;
    }
        
    /*public bool Execute(Transform player, Transform detector, CountdownTimer timer)
    {
        if(timer.IsRunning) return false;
            
        Vector3 vectorToPlayer = player.position - detector.position;
        float angleToPlayer = Vector3.Angle(detector.forward, vectorToPlayer);
            
        //if Player is (not in angle & in outer Radi) & ( not in outer Radi)
        // && Player is in outer Radi but not in angle
        if(angleToPlayer > detectionAngle/2 || vectorToPlayer.magnitude > detectionRadius)
            return false;
            
        timer.Start();
        return true;
    }*/
        
        
}