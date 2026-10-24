---
name: poly-risk-motion
description: Use when improving PolyRisk board visuals, pawn movement, UI animation, interaction feedback, transitions, or motion performance on mobile/WebGL.
---

# PolyRisk motion and visual feedback

Keep the established dark fintech visual direction and use the design requirements already recorded in the project docs. Prioritize clear state feedback and restrained motion over effects applied everywhere. Preserve the reduced-animation accessibility option.

## Workflow

1. Inspect the existing UI Toolkit USS/UXML, runtime transition helpers, board visuals, and current animation settings before proposing a library.
2. Prefer native USS transitions and existing project code when they can deliver the interaction. Do not add continuous glow, broad post-processing, or a second animation framework by default.
3. Treat [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween) as a candidate for a focused prototype only. Before adopting it, verify the exact Unity 2022.3 release, IL2CPP WebGL and iOS builds, licensing, allocations, cancellation/lifecycle behavior, and reduced-motion handling. Compare it with the current implementation and remove it if the measured result does not justify the dependency.
4. Animate only meaningful state changes: selected/arrived board tiles, property selection, successful actions, and count-up values. Keep motion interruptible and ensure final state is correct when animations are reduced or disabled.
5. Measure a Release WebGL build in-browser and on real mobile hardware when available. Check frame time, allocations, payload, and touch responsiveness before and after.

For sector icons, [game-icons/icons](https://github.com/game-icons/icons) is a source to curate from, not a Unity dependency. Choose a consistent author/style, recolor into the existing semantic family palette, optimize the delivered assets, and retain the required per-icon attribution in game credits.

