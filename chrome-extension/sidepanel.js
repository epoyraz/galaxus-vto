"use strict";

const DEFAULT_API_BASE = "http://localhost:5056";
const DEFAULT_PROMPT =
  "The person of image 1, maintaining exactly their face and pose, wearing the garments of image 2.";

const el = (id) => document.getElementById(id);

// --- State ---------------------------------------------------------------
let garmentUrl = null;   // product image URL detected from the page
let garmentFile = null;  // optional user-supplied garment image
let personFile = null;   // user's photo
let promptDirty = false; // true once the user edits the prompt manually

// --- Settings (API base URL persisted in chrome.storage) -----------------
async function getApiBase() {
  const { apiBase } = await chrome.storage.local.get("apiBase");
  return (apiBase || DEFAULT_API_BASE).replace(/\/+$/, "");
}

el("settingsToggle").addEventListener("click", () => el("settings").classList.toggle("hidden"));
el("apiBase").addEventListener("change", (e) => {
  chrome.storage.local.set({ apiBase: e.target.value.trim() });
});

// --- Product / garment detection -----------------------------------------
async function getActiveTab() {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  return tab || null;
}

async function detectProduct() {
  const tab = await getActiveTab();
  if (!tab) return null;

  // First try the content script (declared on Galaxus domains)...
  try {
    const res = await chrome.tabs.sendMessage(tab.id, { type: "GALAXUS_TRYON_GET_PRODUCT" });
    if (res && res.imageUrl) return res;
  } catch {
    // content script not present (e.g. tab opened before install) — fall through
  }

  // ...then fall back to injecting a one-off extraction script.
  try {
    const [{ result } = {}] = await chrome.scripting.executeScript({
      target: { tabId: tab.id },
      func: () => {
        const m = (s) => document.querySelector(s)?.getAttribute("content")?.trim() || null;
        return {
          imageUrl: m('meta[property="og:image"]') || m('meta[property="og:image:url"]'),
          title: m('meta[property="og:title"]') || document.title || "",
          pageUrl: location.href,
        };
      },
    });
    return result || null;
  } catch {
    return null;
  }
}

function renderGarment() {
  const img = el("garmentImg");
  const box = el("garmentBox");
  const hint = el("garmentHint");
  const src = garmentFile ? URL.createObjectURL(garmentFile) : garmentUrl;

  if (src) {
    img.src = src;
    img.hidden = false;
    box.classList.remove("empty");
    hint.hidden = true;
  } else {
    img.hidden = true;
    box.classList.add("empty");
    hint.hidden = false;
  }
  updateGenerateEnabled();
}

async function refreshFromPage() {
  el("garmentHint").textContent = "Detecting…";
  const product = await detectProduct();

  if (product && product.imageUrl) {
    garmentUrl = product.imageUrl;
    garmentFile = null; // page detection wins unless user overrides again
    el("garmentTitle").textContent = product.title || "";
    if (!promptDirty && product.title) {
      el("prompt").value =
        `The person of image 1, maintaining exactly their face and pose, wearing the ${product.title} of image 2.`;
    }
  } else {
    garmentUrl = null;
    el("garmentTitle").textContent = "";
    el("garmentHint").textContent =
      "Couldn't detect a product. Open a Galaxus product page, or upload a garment image below.";
  }
  renderGarment();
}

// --- Person photo --------------------------------------------------------
function setPersonFile(file) {
  if (!file || !file.type.startsWith("image/")) return;
  personFile = file;
  const img = el("personImg");
  img.src = URL.createObjectURL(file);
  img.hidden = false;
  el("personHint").hidden = true;
  updateGenerateEnabled();
}

const personDrop = el("personDrop");
el("personFile").addEventListener("change", (e) => setPersonFile(e.target.files?.[0]));
["dragover", "dragenter"].forEach((ev) =>
  personDrop.addEventListener(ev, (e) => { e.preventDefault(); personDrop.classList.add("dragover"); }));
["dragleave", "drop"].forEach((ev) =>
  personDrop.addEventListener(ev, () => personDrop.classList.remove("dragover")));
personDrop.addEventListener("drop", (e) => {
  e.preventDefault();
  setPersonFile(e.dataTransfer?.files?.[0]);
});

// --- Garment override upload ---------------------------------------------
el("garmentFile").addEventListener("change", (e) => {
  const file = e.target.files?.[0];
  if (file && file.type.startsWith("image/")) {
    garmentFile = file;
    el("garmentTitle").textContent = file.name;
    renderGarment();
  }
});

// --- Prompt --------------------------------------------------------------
el("prompt").value = DEFAULT_PROMPT;
el("prompt").addEventListener("input", () => { promptDirty = true; });

// --- Generate ------------------------------------------------------------
function updateGenerateEnabled() {
  el("generateBtn").disabled = !(personFile && (garmentUrl || garmentFile));
}

function fileToBase64(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result).split(",")[1]);
    reader.onerror = reject;
    reader.readAsDataURL(file);
  });
}

el("generateBtn").addEventListener("click", async () => {
  const result = el("result");
  result.innerHTML = '<div class="spinner"></div><p class="status">Generating your try-on… (a few seconds)</p>';
  el("generateBtn").disabled = true;

  try {
    const apiBase = await getApiBase();
    const payload = {
      prompt: el("prompt").value.trim() || DEFAULT_PROMPT,
      person: await fileToBase64(personFile),
      garment: garmentFile ? await fileToBase64(garmentFile) : garmentUrl,
      outputFormat: "webp",
    };

    const res = await fetch(`${apiBase}/api/vto/try-on`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });

    const data = await res.json().catch(() => ({}));

    if (!res.ok) {
      const msg = data.detail || data.title || `Request failed (HTTP ${res.status}).`;
      result.innerHTML = `<p class="error">${msg}</p>`;
    } else if (data.imageUrl) {
      result.innerHTML =
        `<img src="${data.imageUrl}" alt="Try-on result" />` +
        `<a href="${data.imageUrl}" target="_blank" rel="noopener">Open full size ↗</a>`;
    } else {
      result.innerHTML = `<p class="status">Status: ${data.status || "unknown"} (no image returned)</p>`;
    }
  } catch (err) {
    result.innerHTML =
      `<p class="error">Could not reach the API. Is it running, and is the API base URL correct (⚙)?<br><small>${err}</small></p>`;
  } finally {
    updateGenerateEnabled();
  }
});

// --- Wire up + initial load ----------------------------------------------
el("detectBtn").addEventListener("click", refreshFromPage);
chrome.tabs.onActivated.addListener(refreshFromPage);
chrome.tabs.onUpdated.addListener((_id, info) => { if (info.status === "complete") refreshFromPage(); });

(async () => {
  el("apiBase").value = await getApiBase();
  refreshFromPage();
})();
