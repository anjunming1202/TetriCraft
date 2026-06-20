using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Executes one-shot callbacks at a specified future GameTick.
/// Flames use this for self-scheduling after their first random tick fires.
/// </summary>
public class ScheduledTickManager : MonoBehaviour
{
    // Each queue slot stores (callback, world-position) pairs keyed by target GameTick.
    // The world position is optional (default = Vector3.zero) and used by the debugger.
    private readonly SortedList<uint, List<(Action cb, Vector3 pos)>> queue = new();

    /// <summary>Fired when a new entry is scheduled. Parameters: (targetTick, worldPos).</summary>
    public event Action<uint, Vector3> OnEntryScheduled;

    /// <summary>Fired just before an entry's callback is invoked. Parameters: (targetTick, worldPos).</summary>
    public event Action<uint, Vector3> OnEntryFired;

    public void Clear() => queue.Clear();

    /// <summary>
    /// Schedules <paramref name="callback"/> to run after <paramref name="delayTicks"/> game ticks.
    /// Supply <paramref name="worldPos"/> so the debugger can visualize the entry's position.
    /// </summary>
    public void Schedule(Action callback, int delayTicks, Vector3 worldPos = default)
    {
        uint targetTick = TickManager.GameTick + (uint)Mathf.Max(1, delayTicks);

        if (!queue.TryGetValue(targetTick, out var list))
            queue[targetTick] = list = new List<(Action, Vector3)>();
        list.Add((callback, worldPos));

        OnEntryScheduled?.Invoke(targetTick, worldPos);
    }

    public void OnUpdate()
    {
        if (!TickManager.IsGameTickUpdate) return;

        while (queue.Count > 0 && queue.Keys[0] <= TickManager.GameTick)
        {
            uint tick = queue.Keys[0];

            // Copy to avoid re-entrancy issues when a callback calls Schedule() again.
            var entries = new List<(Action cb, Vector3 pos)>(queue.Values[0]);
            queue.RemoveAt(0);

            foreach (var (cb, pos) in entries)
            {
                OnEntryFired?.Invoke(tick, pos);
                cb?.Invoke();
            }
        }
    }
}
