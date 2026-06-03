// Runs on Galaxus product pages. Extracts the product (garment) image and title.
// Uses Open Graph meta tags, which Galaxus populates consistently across all locales.

function extractProduct() {
  const meta = (selector) => document.querySelector(selector)?.getAttribute("content")?.trim() || null;

  let imageUrl =
    meta('meta[property="og:image"]') ||
    meta('meta[property="og:image:url"]') ||
    meta('meta[name="og:image"]');

  // Fallback: pick the largest <img> on the page (likely the product shot).
  if (!imageUrl) {
    let best = null;
    let bestArea = 0;
    for (const img of document.images) {
      const area = (img.naturalWidth || img.width) * (img.naturalHeight || img.height);
      if (area > bestArea && img.currentSrc) {
        bestArea = area;
        best = img.currentSrc;
      }
    }
    imageUrl = best;
  }

  const title =
    meta('meta[property="og:title"]') ||
    document.title?.replace(/\s*[|–-]\s*Galaxus.*$/i, "").trim() ||
    "";

  return { imageUrl, title, pageUrl: location.href };
}

chrome.runtime.onMessage.addListener((msg, _sender, sendResponse) => {
  if (msg?.type === "GALAXUS_TRYON_GET_PRODUCT") {
    sendResponse(extractProduct());
  }
});
