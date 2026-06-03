// Service worker: open the side panel from the toolbar icon, and only enable it
// on Galaxus domains (galaxus.ch / .de / .at / .fr / .it / .nl / .be / .lu).

const GALAXUS_HOST = /(^|\.)galaxus\.(ch|de|at|fr|it|nl|be|lu)$/i;

function isGalaxus(url) {
  try {
    return GALAXUS_HOST.test(new URL(url).hostname);
  } catch {
    return false;
  }
}

chrome.runtime.onInstalled.addListener(() => {
  // Clicking the toolbar icon toggles the side panel open on the active tab.
  chrome.sidePanel.setPanelBehavior({ openPanelOnActionClick: true }).catch(() => {});
});

async function updatePanelForTab(tabId, url) {
  try {
    if (isGalaxus(url)) {
      await chrome.sidePanel.setOptions({ tabId, path: "sidepanel.html", enabled: true });
    } else {
      await chrome.sidePanel.setOptions({ tabId, enabled: false });
    }
  } catch {
    // Tab may have been closed; ignore.
  }
}

chrome.tabs.onUpdated.addListener((tabId, info, tab) => {
  if (info.status === "complete" || info.url) {
    updatePanelForTab(tabId, tab.url || info.url || "");
  }
});

chrome.tabs.onActivated.addListener(async ({ tabId }) => {
  try {
    const tab = await chrome.tabs.get(tabId);
    updatePanelForTab(tabId, tab.url || "");
  } catch {
    // ignore
  }
});
