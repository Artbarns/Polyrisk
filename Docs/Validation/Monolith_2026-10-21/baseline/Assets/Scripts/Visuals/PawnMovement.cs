using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Systems;
using PolyRisk.UI;

namespace PolyRisk.Visuals
{
    /// <summary>
    /// Handles the movement of player pawns along the board path.
    /// Uses Coroutines for smooth hopping movement.
    /// </summary>
    public class PawnMovement : MonoBehaviour
    {
        private const float LandingBounceDuration = 0.08f;
        private const float LandingBounceHeight = 0.055f;
        private const float LandingBounceHorizontalScale = 0.025f;
        private const float LandingBounceVerticalSquash = 0.06f;

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
            bool reducedMotion = UIUXRuntime.ReducedMotion;
            Vector3 baseScale = transform.localScale;
            bool hasMoved = false;
            
            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector3 targetPos = waypoints[i];
                Vector3 startPos = transform.position;
                
                // Calculate distance only on XZ plane to avoid vertical distance affecting speed weirdly?
                // Or just total distance. Total is fine.
                float distance = Vector3.Distance(startPos, targetPos);
                
                // If distance is near zero, skip
                if (distance < 0.01f) continue;
                hasMoved = true;

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
                    if (!reducedMotion && distance > 0.5f)
                    {
                        float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
                        currentPos.y += heightOffset;
                    }

                    // A short squash-and-pop on the next hop makes each tile landing
                    // legible without adding time between intermediate waypoints.
                    float landingBounce = !reducedMotion && i > 0
                        ? Mathf.Sin(Mathf.Clamp01(elapsed / LandingBounceDuration) * Mathf.PI)
                        : 0f;
                    currentPos.y += landingBounce * LandingBounceHeight;

                    transform.position = currentPos;
                    ApplyLandingBounceScale(baseScale, landingBounce);
                    yield return null;
                }

                // Snap to target to avoid drift
                transform.position = targetPos;
                transform.localScale = baseScale;
            }

            // The last tile has no following hop to carry its bounce, so give only
            // the final landing a brief 80 ms pulse before advancing the game loop.
            if (!reducedMotion && hasMoved)
            {
                Vector3 finalPosition = waypoints[waypoints.Count - 1];
                float elapsed = 0f;
                while (elapsed < LandingBounceDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / LandingBounceDuration);
                    float landingBounce = Mathf.Sin(progress * Mathf.PI);
                    transform.position = finalPosition + Vector3.up * (landingBounce * LandingBounceHeight);
                    ApplyLandingBounceScale(baseScale, landingBounce);
                    yield return null;
                }

                transform.position = finalPosition;
            }

            transform.localScale = baseScale;
            onFinished?.Invoke();
        }

        private void ApplyLandingBounceScale(Vector3 baseScale, float bounce)
        {
            float horizontalScale = 1f + LandingBounceHorizontalScale * bounce;
            float verticalScale = 1f - LandingBounceVerticalSquash * bounce;
            transform.localScale = new Vector3(
                baseScale.x * horizontalScale,
                baseScale.y * verticalScale,
                baseScale.z * horizontalScale);
        }
    }
}
