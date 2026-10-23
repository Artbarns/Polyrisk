using System;
using System.Collections.Generic;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Non-visual event queue shared by the UI presenters. It records every fact,
    /// serializes one major sequence at a time, and leaves timing/animation to the view.
    /// </summary>
    public sealed class GameEventPresenter : IDisposable
    {
        private const int HistoryLimit = 50;
        private readonly List<GameEvent> history = new List<GameEvent>();
        private long enqueueOrder;
        private bool disposed;

        private struct PendingEvent
        {
            public GameEvent Event;
            public long Order;
        }

        private readonly List<PendingEvent> orderedPending = new List<PendingEvent>();

        public int LocalPlayerId { get; set; } = -1;
        public bool ReducedMotion { get; set; }
        public bool FastOtherPlayers { get; set; } = true;
        public bool PreserveChronology { get; set; }
        public Func<GameEvent, bool> ShouldQueue { get; set; }
        public GameEvent Current { get; private set; }
        public GamePresentationBeat CurrentBeat { get; private set; }
        public event Action<GameEvent> TraceRecorded;

        public IReadOnlyList<GameEvent> History => history.AsReadOnly();
        public int PendingCount => orderedPending.Count;

        public GameEventPresenter()
        {
            GameEvents.OnGameEventPublished += Enqueue;
        }

        public void Dispose()
        {
            if (disposed) return;
            GameEvents.OnGameEventPublished -= Enqueue;
            disposed = true;
            orderedPending.Clear();
            Current = null;
            TraceRecorded = null;
        }

        public void Enqueue(GameEvent gameEvent)
        {
            if (disposed || gameEvent == null) return;
            history.Add(gameEvent);
            if (history.Count > HistoryLimit) history.RemoveAt(0);

            bool shouldQueue = ShouldQueue == null || ShouldQueue(gameEvent);
            bool merged = false;
            if (shouldQueue && gameEvent.Coalescible)
            {
                // Synchronous split payments can arrive after the first event has
                // already started presenting. Keep one receipt in the active beat.
                if (Current != null && Current.CanCoalesceWith(gameEvent))
                {
                    Current = Current.MergeForPresentation(gameEvent);
                    merged = true;
                }

                for (int i = orderedPending.Count - 1; i >= 0; i--)
                {
                    if (merged) break;
                    if (!orderedPending[i].Event.CanCoalesceWith(gameEvent)) continue;
                    var pendingEvent = orderedPending[i];
                    pendingEvent.Event = pendingEvent.Event.MergeForPresentation(gameEvent);
                    orderedPending[i] = pendingEvent;
                    merged = true;
                    break;
                }
            }

            if (!merged && shouldQueue)
                orderedPending.Add(new PendingEvent { Event = gameEvent, Order = ++enqueueOrder });

            // Let consumers observe the event only after the queue contains its matching item.
            TraceRecorded?.Invoke(gameEvent);
        }

        /// <summary>Starts the highest-priority queued item. Active feedback is never preempted.</summary>
        public bool TryBeginNext(out GameEvent gameEvent)
        {
            gameEvent = null;
            if (disposed || Current != null || orderedPending.Count == 0) return false;

            int nextIndex = 0;
            for (int i = 1; i < orderedPending.Count; i++)
            {
                var candidate = orderedPending[i];
                var selected = orderedPending[nextIndex];
                if (PreserveChronology ? candidate.Order < selected.Order
                    : candidate.Event.Priority > selected.Event.Priority
                    || (candidate.Event.Priority == selected.Event.Priority && candidate.Order < selected.Order))
                    nextIndex = i;
            }

            Current = orderedPending[nextIndex].Event;
            orderedPending.RemoveAt(nextIndex);
            CurrentBeat = GamePresentationBeat.Announcement;
            gameEvent = Current;
            return true;
        }

        /// <summary>Moves to the next choreography beat; returns false after Trace completes.</summary>
        public bool AdvanceBeat()
        {
            if (Current == null) return false;
            if (CurrentBeat >= GamePresentationBeat.Trace)
            {
                Current = null;
                return false;
            }
            CurrentBeat++;
            return true;
        }

        /// <summary>Tap-to-skip ends presentation only; the confirmed event and its trace remain.</summary>
        public void SkipCurrent() => Current = null;

        public float GetBeatDurationSeconds(GameEvent gameEvent, GamePresentationBeat beat)
        {
            if (gameEvent == null) return 0f;
            if (ReducedMotion) return beat == GamePresentationBeat.Anticipation ? 0f : 0.07f;

            float duration;
            switch (gameEvent.Priority)
            {
                case GameEventPriority.Critical:
                    duration = beat == GamePresentationBeat.Announcement ? 0.20f
                        : beat == GamePresentationBeat.Anticipation ? 0.38f
                        : beat == GamePresentationBeat.Impact ? 0.42f
                        : beat == GamePresentationBeat.Transfer ? 0.62f : 0.18f;
                    break;
                case GameEventPriority.Major:
                    duration = beat == GamePresentationBeat.Announcement ? 0.18f
                        : beat == GamePresentationBeat.Anticipation ? 0.44f
                        : beat == GamePresentationBeat.Impact ? 0.28f
                        : beat == GamePresentationBeat.Transfer ? 0.55f : 0.16f;
                    break;
                case GameEventPriority.Ambient:
                    duration = beat == GamePresentationBeat.Announcement ? 0.06f
                        : beat == GamePresentationBeat.Anticipation ? 0f
                        : beat == GamePresentationBeat.Impact ? 0.07f
                        : beat == GamePresentationBeat.Transfer ? 0.10f : 0.06f;
                    break;
                default:
                    duration = beat == GamePresentationBeat.Announcement ? 0.10f
                        : beat == GamePresentationBeat.Anticipation ? 0.20f
                        : beat == GamePresentationBeat.Impact ? 0.16f
                        : beat == GamePresentationBeat.Transfer ? 0.25f : 0.10f;
                    break;
            }

            if (FastOtherPlayers && LocalPlayerId >= 0 && gameEvent.ActorId >= 0
                && gameEvent.ActorId != LocalPlayerId)
                duration *= 0.6f;
            return duration;
        }
    }
}
