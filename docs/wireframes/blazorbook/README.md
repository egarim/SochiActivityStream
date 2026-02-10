# BlazorBook Wireframes Checklist

Store exported wireframes for the Facebook-style BlazorBook experience here. For each surface produce low- and mid-fidelity variants in PNG (or PDF when needed) along with source files (Figma, Draw.io, Excalidraw).

## Required Assets
- `feed-low-fidelity.png`
- `feed-mid-fidelity.png`
- `profile-low-fidelity.png`
- `profile-mid-fidelity.png`
- `messaging-low-fidelity.png`
- `messaging-mid-fidelity.png`
- `notifications-low-fidelity.png`
- `notifications-mid-fidelity.png`
- `search-low-fidelity.png`
- `search-mid-fidelity.png`
- `navigation-map.drawio` (route + dialog graph referenced by navigation blueprint)

Place corresponding editable sources inside a `sources/` subfolder once available.

## X-Themed Overrides

1. The canonical tokens for the dark/"X" experience now live in `wwwroot/app.css`. You can tune colors, spacing, and sidebar dimensions by updating the `--bb-*` variables near the top of that file—everything from the nav height (`--bb-nav-height`) to the accent glow (`--bb-accent-soft`) is centralized there.
2. Shared components consume those tokens through `SocialKit.Components/wwwroot/socialkit.css`. Override specific component surfaces by copying the selectors you need (for example, `.sk-post-card` or `.sk-create-post`) into a new stylesheet that loads after `app.css`, or add a second theme file that redefines the same `--bb-*` variables before other CSS runs.
3. Layout-specific helpers such as `x-shell`, `x-shell__rail`, and `x-top-nav` are also defined in `wwwroot/app.css`. If you need a variation for a feature (for instance, a popup rail), layer new selectors onto those helpers so the document flow remains consistent.
