# Galaxus Virtual Try-On — Chrome Extension

A Manifest V3 Chrome extension that adds a **side-panel virtual try-on** to every
Galaxus storefront. On a product page it auto-detects the item image; you drop a photo
of yourself and get a generated try-on from the [companion API](../api).

## How it works

1. Active on all Galaxus domains: `galaxus.ch`, `.de`, `.at`, `.fr`, `.it`, `.nl`, `.be`, `.lu`
   (including `www.` and other subdomains).
2. The toolbar icon opens a **side panel** (`chrome.sidePanel`). It's enabled only on
   Galaxus tabs.
3. A content script reads the product image + title from the page's Open Graph tags
   (`og:image` / `og:title`) — locale-independent and stable across the React storefront.
4. You **drop your photo** into the panel. Clicking *Generate* sends
   `{ person: <your photo, base64>, garment: <product image URL>, prompt }` to the API's
   `POST /api/vto/try-on` endpoint and shows the result image.

You can also upload your own garment image, edit the prompt, and change the API base URL
(⚙ settings) — it defaults to `http://localhost:5056` and is saved in `chrome.storage`.

## Prerequisites

The API must be running and reachable from the browser:

```bash
dotnet run --project ../api
```

The API allows any origin (open CORS), so the extension can call it directly.

## Install (load unpacked)

1. Start the API (above).
2. Open `chrome://extensions`.
3. Toggle **Developer mode** (top-right).
4. Click **Load unpacked** and select this `chrome-extension/` folder.
5. Navigate to any Galaxus product page and click the extension's toolbar icon to open
   the side panel.

> If you installed the extension while a Galaxus tab was already open, reload that tab so
> the content script attaches (a one-off injected fallback also covers most cases).

## Files

| File | Purpose |
| ---- | ------- |
| `manifest.json` | MV3 manifest — domains, permissions, side panel, content script |
| `background.js` | Service worker — opens the panel on icon click; enables it on Galaxus tabs only |
| `content.js` | Extracts the product (garment) image + title from the page |
| `sidepanel.html` / `.css` / `.js` | The side-panel UI and try-on logic |
| `icons/` | Toolbar icons |

## Notes

- Result image URLs from the API are signed and expire after ~10 minutes.
- For best results use a clear, full-body photo on a neutral background.
- To target a deployed API instead of localhost, open ⚙ and set the API base URL
  (its host is covered by the API's open CORS).
