(function () {
  var storageKey = "kohinoorTheme";
  var defaultTheme = "dark";
  var alternateTheme = "light";

  function normalizeTheme(themeName) {
    return themeName === "silver-blue" || themeName === alternateTheme ? alternateTheme : defaultTheme;
  }

  function applyTheme(themeName) {
    var theme = normalizeTheme(themeName);
    document.body.setAttribute("data-theme", theme);

    document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
      var isAlternate = theme === alternateTheme;
      button.textContent = isAlternate ? "Dark Theme" : "Light Theme";
      button.setAttribute("aria-pressed", isAlternate ? "true" : "false");
      button.setAttribute("title", isAlternate ? "Switch to the dark theme" : "Switch to the light theme");
    });
  }

  function getStoredTheme() {
    return normalizeTheme(sessionStorage.getItem(storageKey) || localStorage.getItem(storageKey) || defaultTheme);
  }

  function saveTheme(theme) {
    try {
      localStorage.setItem(storageKey, theme);
    } catch (error) {
      sessionStorage.setItem(storageKey, theme);
    }
  }

  function initThemeSwitch() {
    applyTheme(getStoredTheme());

    document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
      if (button.hasAttribute("data-theme-bound")) {
        return;
      }

      button.setAttribute("data-theme-bound", "true");
      button.addEventListener("click", function () {
        var currentTheme = normalizeTheme(document.body.getAttribute("data-theme") || defaultTheme);
        var nextTheme = currentTheme === alternateTheme ? defaultTheme : alternateTheme;
        saveTheme(nextTheme);
        applyTheme(nextTheme);
      });
    });
  }

  window.kohinoorThemeInit = initThemeSwitch;
  initThemeSwitch();
}());