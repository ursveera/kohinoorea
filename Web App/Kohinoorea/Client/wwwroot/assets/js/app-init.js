window.kohinoorInit = function () {
  if (typeof window.kohinoorThemeInit === "function") {
    window.kohinoorThemeInit();
  }

  if (typeof window.kohinoorAuthTabsInit === "function") {
    window.kohinoorAuthTabsInit();
  }

  if (typeof window.kohinoorHeroInit === "function") {
    window.kohinoorHeroInit();
  }

  if (typeof window.kohinoorCommerceInit === "function") {
    window.kohinoorCommerceInit();
  }

  if (typeof window.kohinoorHighlightTabsInit === "function") {
    window.kohinoorHighlightTabsInit();
  }

  if (typeof window.kohinoorAuthActivityInit === "function") {
    window.kohinoorAuthActivityInit();
  }
};

window.kohinoorAuthTouch = function () {
  localStorage.setItem("authLastActivityUtc", new Date().toISOString());
};

window.kohinoorAuthGetLastActivityUtc = function () {
  return localStorage.getItem("authLastActivityUtc");
};

window.kohinoorDownloadFile = function (fileName, content, mimeType) {
  var blob = new Blob([content], { type: mimeType || "application/octet-stream" });
  var objectUrl = URL.createObjectURL(blob);
  var anchor = document.createElement("a");
  anchor.href = objectUrl;
  anchor.download = fileName || "download";
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  setTimeout(function () {
    URL.revokeObjectURL(objectUrl);
  }, 0);
};

window.kohinoorAuthActivityInit = function () {
  if (window.__kohinoorAuthActivityBound) {
    return;
  }

  window.__kohinoorAuthActivityBound = true;
  var touch = window.kohinoorAuthTouch;
  ["click", "keydown", "mousemove", "scroll", "touchstart"].forEach(function (eventName) {
    window.addEventListener(eventName, touch, { passive: true });
  });

  document.addEventListener("visibilitychange", function () {
    if (!document.hidden) {
      touch();
    }
  });

  touch();
};

window.kohinoorHighlightTabsInit = function () {
  document.querySelectorAll(".hero-console-panel").forEach(function (panel) {
    var tabs = panel.querySelectorAll("[data-highlight-tab]");
    if (!tabs.length) {
      return;
    }

    tabs.forEach(function (tab) {
      if (tab.hasAttribute("data-highlight-bound")) {
        return;
      }

      tab.setAttribute("data-highlight-bound", "true");
      tab.addEventListener("click", function () {
        var target = tab.getAttribute("data-highlight-tab");

        tabs.forEach(function (item) {
          var isActive = item === tab;
          item.classList.toggle("active", isActive);
          item.setAttribute("aria-selected", isActive ? "true" : "false");
        });

        panel.querySelectorAll("[data-highlight-panel]").forEach(function (content) {
          content.classList.toggle("active", content.getAttribute("data-highlight-panel") === target);
        });
      });
    });
  });
};
