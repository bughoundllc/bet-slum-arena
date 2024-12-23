using bet_slum.CombatArena;
using UnityEngine;

public static class CollisionHelper
{
    public static bool IsPrimaryForceInCollision(Rigidbody selfRb, Collision collision)
    {
        if(collision.rigidbody == null) return false;
        Vector3 collisionNormal = collision.GetContact(0).impulse.normalized;
        float selfContribution = Vector3.Dot(selfRb.linearVelocity.normalized, collisionNormal);
        float otherContribution = Vector3.Dot(collision.rigidbody.linearVelocity.normalized, collisionNormal);
        return selfContribution > otherContribution;
    }
}