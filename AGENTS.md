# PolyRisk agent guidance

For motion/UI direction, read `Docs/Design/MOTION_LAB_MEMORY.md` for Arthur's recorded preferences and approved references. Keep it current when he explicitly asks to remember a design decision. Every proposed motion/UI direction needs a visual preview; this direction chat prepares handoffs and does not imply authorization to change runtime code.

Use these local skills automatically when the task matches:

- Runtime UI, UXML/USS, HUD, bottom sheets, responsive layout, or UI Toolkit motion: read `.agents/skills/ui-uitk/SKILL.md` first. This project is on Unity 2022.3.62f3 and already uses runtime UI Toolkit; preserve that architecture and verify APIs against the installed editor. The upstream skill's Unity 6 examples are references, not drop-in code.
- WebGL build size, clarity, load time, memory, or browser performance: read `.agents/skills/optimize-web/SKILL.md`. Treat it as an audit checklist only. It targets newer Unity and assumes a Unity CLI workflow; this project uses Unity 2022.3, so inspect the actual editor/version settings and available tools before applying advice. Never run its bulk settings changes without checking each setting, current project behavior, and a measured build comparison.
- TextMeshPro-specific work in the remaining legacy components: read `.agents/skills/optimize-text-mesh-pro/SKILL.md`. Most runtime UI uses UI Toolkit/TextCore, so do not apply TMP guidance to it.
- .NET lobby/server performance or reliability: read `.agents/skills/poly-risk-dotnet-backend/SKILL.md`.
- Board motion, interaction feedback, or animation performance: read `.agents/skills/poly-risk-motion/SKILL.md`.

Project-wide constraints:

- Keep game rules, economy, player data, and network protocol behavior unchanged during UI/presentation tasks unless Arthur explicitly asks for gameplay/backend behavior changes.
- Follow the specialized GDDs in `Docs/` as authoritative for gameplay.
- Preserve existing uncommitted work. Do not reset, clean, or overwrite unrelated changes.
- Profile and compare before/after for performance claims. Test actual WebGL output in a browser; Editor-only results do not prove browser performance. Do not claim physical-device coverage from desktop emulation.
- Prefer built-in Unity/UI Toolkit capabilities when they meet the need. PrimeTween is a candidate reference, not an installed dependency. Before adding it or any other runtime package, check Unity 2022.3, IL2CPP WebGL, iOS, license, payload, reduced-motion support, and measured benefit.
- `game-icons/icons` is an optional source for board-family icons, not an automatic bulk import. Curate one coherent artist style and preserve per-icon CC BY/CC0 attribution in the shipped credits.
- Do not migrate the working lobby to Nakama or another backend solely for optimization. First measure the current server and improve the existing implementation where evidence points.
- Do not create subagents unless Arthur explicitly asks for delegation.
