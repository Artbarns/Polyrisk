using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Systems;

namespace PolyRisk.Visuals
{
    /// <summary>
    /// Handles the movement of player pawns along the board path.
    /// Uses Coroutines for smooth hopping movement.
    /// </summary>
    public class PawnMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Movement speed in units per second.")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Height of the hop arc.")]
        [SerializeField] private float arcHeight = 0.5f;

        /// <summary>
        /// Moves the pawn along a sequence of waypoints.
        /// </summary>
        /// <param name="waypoints">List of world positions to visit.</param>
        /// <param name="onMovementFinished">Callback when destination is reached.</param>
        public void MoveAlongPath(List<Vector3> waypoints, Action onMovementFinished)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                onMovementFinished?.Invoke();
                return;
            }

            // Stress test (100% IA) : sauter le hop case par case, qui impose au moins 1 frame
            // réelle par case traversée indépendamment de Time.timeScale. Téléportation directe.
            if (GameBootstrapper.SkipMovementAnimation)
            {
                transform.position = waypoints[waypoints.Count - 1];
                onMovementFinished?.Invoke();
                return;
            }

            StartCoroutine(MoveRoutine(waypoints, onMovementFinished));
        }

        private IEnumerator MoveRoutine(List<Vector3> waypoints, Action onFinished)
        {
            // We iterate through each waypoint, moving from current pos to waypoint.
            // Note: 'transform.position' updates at the end of each segment.
            
            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector3 targetPos = waypoints[i];
                Vector3 startPos = transform.position;
                
                // Calculate distance only on XZ plane to avoid vertical distance affecting speed weirdly?
                // Or just total distance. Total is fine.
                float distance = Vector3.Distance(startPos, targetPos);
                
                // If distance is near zero, skip
                if (distance < 0.01f) continue;

                // Duration based on speed
                float duration = distance / moveSpeed;
                
                // Ensure duration isn't zero
                if (duration <= 0.01f) duration = 0.1f;

                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    // Linear interpolation for X and Z
                    Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

                    // Parabolic arc for Y (Jump)
                    // Sin(t * PI) goes from 0 to 1 to 0 over t=0..1
                    // Only add arc if distance is significant (don't hop in place)
                    if (distance > 0.5f)
                    {
                        float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
                        currentPos.y += heightOffset;
                    }

                    transform.position = currentPos;
                    yield return null;
                }

                // Snap to target to avoid drift
                transform.position = targetPos;
            }

            onFinished?.Invoke();
        }
    }
}
