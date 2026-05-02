# Font Awesome — locally hosted

This folder is set up to hold Font Awesome 6 Free webfonts so the demo
does not require an internet connection at runtime.

## Quick install (one minute)

1. Download **Font Awesome Free for Web** from:
   https://fontawesome.com/download

2. Extract the ZIP. You'll get a folder like `fontawesome-free-7.x.x-web/`.

3. Copy these two subfolders into this directory, preserving their names:

   - `css/`       → `/fonts/fontawesome/css/`       (must contain `all.min.css`)
   - `webfonts/`  → `/fonts/fontawesome/webfonts/`  (the `.woff2` files)

4. Done. The site template already references `/fonts/fontawesome/css/all.min.css`.

## Fallback

If you skip this step, the demo still works — icons simply won't render.
The layout doesn't depend on icon presence.

## Why local?

Two reasons:

- **Offline development.** The demo runs without a network connection.
- **Principle.** Pageless architecture favours self-contained projects.
  The MySqlExpress README is in the same spirit: one `.cs` file, drop it
  in, ship. A demo that silently pulls from a CDN on first request would
  undercut that story.

The old demo project loaded Font Awesome from a CDN; this one does not.
