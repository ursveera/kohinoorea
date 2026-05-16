(function () {
  var storagePlanKey = "kohinoorSelectedPlan";
  var storageLoginKey = "kohinoorLoggedIn";
  var plans = {
    monthly: {
      key: "monthly",
      name: "Monthly Plan",
      price: "$99",
      description: "Monthly Plan - $99 for traders who want a lower-commitment entry point with full Kohinoor EA access and direct onboarding support.",
      support: "Standard onboarding and setup support after purchase.",
      launchCode: "Available on request",
      routeLabel: "Monthly Plan - $99",
      stripeUrl: "https://buy.stripe.com/REPLACE_MONTHLY_LINK"
    },
    annual: {
      key: "annual",
      name: "Annual Plan",
      price: "$299",
      description: "Annual Plan - $299. Saving $150, discount 33.33% off, with the extra 15% launch offer available through promo code LAUNCH26.",
      support: "Priority onboarding and launch-offer support after purchase.",
      launchCode: "LAUNCH26",
      routeLabel: "Annual Plan - $299",
      stripeUrl: "https://buy.stripe.com/REPLACE_ANNUAL_LINK"
    },
    lifetime: {
      key: "lifetime",
      name: "Lifetime Plan (4 Years)",
      price: "$799",
      description: "Lifetime Plan (4 Years) - $799. Saving $364, discount 31.25% off, for buyers who want a longer deployment window with promo code LAUNCH26 available.",
      support: "Priority support for long-term deployment and activation.",
      launchCode: "LAUNCH26",
      routeLabel: "Lifetime Plan (4 Years) - $799",
      stripeUrl: "https://buy.stripe.com/REPLACE_LIFETIME_LINK"
    },
    demo: {
      key: "demo",
      name: "Demo Access",
      price: "$0",
      description: "A demo-first route for new visitors who want to review the setup flow before choosing a paid plan.",
      support: "Demo guidance and onboarding direction before plan activation.",
      launchCode: "Not required",
      routeLabel: "Demo Access - $0"
    }
  };

  function getPlan(planKey) {
    return plans[planKey] || plans.annual;
  }

  function getQueryParam(name) {
    var params = new URLSearchParams(window.location.search);
    return params.get(name);
  }

  function setSelectedPlan(planKey) {
    sessionStorage.setItem(storagePlanKey, getPlan(planKey).key);
  }

  function getSelectedPlan() {
    return getPlan(getQueryParam("plan") || sessionStorage.getItem(storagePlanKey) || "annual");
  }

  function setLoggedIn(value) {
    sessionStorage.setItem(storageLoginKey, value ? "true" : "false");
  }

  function isLoggedIn() {
    return sessionStorage.getItem(storageLoginKey) === "true";
  }

  function setText(selector, value) {
    var element = document.querySelector(selector);
    if (element) {
      element.textContent = value;
    }
  }

  function setLink(selector, href) {
    var element = document.querySelector(selector);
    if (element) {
      element.setAttribute("href", href);
    }
  }

  function updateBuyStripeLink(plan) {
    var button = document.querySelector("[data-buy-stripe-link]");
    var note = document.querySelector("[data-buy-stripe-note]");
    if (!button) {
      return;
    }

    button.setAttribute("href", plan.stripeUrl || "#");
    button.textContent = "Purchase " + plan.name + " with Stripe";

    if (note) {
      note.textContent = "Stripe checkout opens a secure hosted payment page for " + plan.name + ". Update the Stripe payment link in assets/js/commerce-flow.js before going live.";
    }
  }

  function goTo(route, planKey) {
    window.location.href = route + "?plan=" + encodeURIComponent(planKey);
  }

  function initPricingFlow() {
    var selectedPlan = getSelectedPlan();
    setText("[data-flow-status]", isLoggedIn() ? "You are already in the demo client flow. Choosing a plan sends you directly to cart review." : "Choose a plan, register if you are new, log in, then review the cart before purchase.");

    document.querySelectorAll("[data-flow-plan]").forEach(function (element) {
      element.addEventListener("click", function (event) {
        var planKey = element.getAttribute("data-flow-plan") || selectedPlan.key;
        event.preventDefault();
        setSelectedPlan(planKey);
        if (isLoggedIn()) {
          goTo("/products", planKey);
          return;
        }
        goTo("/registration", planKey);
      });
    });
  }

  function initRegistrationFlow() {
    var selectedPlan = getSelectedPlan();
    setLoggedIn(false);
    setSelectedPlan(selectedPlan.key);
    setText("[data-selected-plan-name]", selectedPlan.name);
    setText("[data-selected-plan-price]", selectedPlan.price);
    setText("[data-selected-plan-description]", selectedPlan.description);

    var form = document.querySelector("[data-registration-form]");
    if (form) {
      form.addEventListener("submit", function (event) {
        event.preventDefault();
        goTo("/login", selectedPlan.key);
      });
    }
  }

  function initLoginFlow() {
    var selectedPlan = getSelectedPlan();
    setSelectedPlan(selectedPlan.key);
    setText("[data-login-plan-name]", selectedPlan.name);
    setText("[data-login-plan-price]", selectedPlan.price);
    setText("[data-login-plan-description]", selectedPlan.description);
    setLink("[data-login-register-link]", "/registration?plan=" + encodeURIComponent(selectedPlan.key));

    var form = document.querySelector("[data-login-form]");
    if (form) {
      form.addEventListener("submit", function (event) {
        event.preventDefault();
        setLoggedIn(true);
        goTo("/products", selectedPlan.key);
      });
    }
  }

  function initProductsFlow() {
    var selectedPlan = getSelectedPlan();
    setSelectedPlan(selectedPlan.key);
    setText("[data-products-selected-summary]", selectedPlan.name + " is selected. Add it to cart or choose another product.");

    document.querySelectorAll("[data-product-plan]").forEach(function (button) {
      button.addEventListener("click", function () {
        var planKey = button.getAttribute("data-product-plan") || selectedPlan.key;
        setSelectedPlan(planKey);
        goTo("/cart", planKey);
      });
    });
  }

  function initCartFlow() {
    var selectedPlan = getSelectedPlan();
    setSelectedPlan(selectedPlan.key);
    setText("[data-cart-plan-name]", selectedPlan.name);
    setText("[data-cart-plan-description]", selectedPlan.description);
    setText("[data-cart-plan-price]", selectedPlan.routeLabel);
    setText("[data-cart-total]", selectedPlan.routeLabel + (selectedPlan.launchCode === "LAUNCH26" ? " with LAUNCH26" : ""));
    setText("[data-cart-launch-code]", selectedPlan.launchCode);
    setText("[data-cart-support]", selectedPlan.support);
    setText("[data-cart-review]", selectedPlan.name + " is ready to move into the purchase request page.");
    setLink("[data-cart-buy-link]", "/buy?plan=" + encodeURIComponent(selectedPlan.key));
  }

  function initBuyFlow() {
    function syncBuyPlan(planKey) {
      var selectedPlan = getPlan(planKey);
      setSelectedPlan(selectedPlan.key);
      var selectedInput = document.querySelector('input[name="plan"][value="' + selectedPlan.key + '"]');
      if (selectedInput) {
        selectedInput.checked = true;
      }
      setLink("[data-buy-cart-link]", "/cart?plan=" + encodeURIComponent(selectedPlan.key));
      updateBuyStripeLink(selectedPlan);
    }

    syncBuyPlan(getSelectedPlan().key);

    document.querySelectorAll('input[name="plan"]').forEach(function (input) {
      input.addEventListener("change", function () {
        syncBuyPlan(input.value);
      });
    });
  }

  function initCommerceFlow() {
    var path = (window.location.pathname || "").toLowerCase();

    if (path === "/pricing") {
      initPricingFlow();
    }

    if (path === "/registration") {
      initRegistrationFlow();
    }

    if (path === "/login") {
      initLoginFlow();
    }

    if (path === "/products") {
      initProductsFlow();
    }

    if (path === "/cart") {
      initCartFlow();
    }

    if (path === "/buy") {
      initBuyFlow();
    }
  }

  window.kohinoorCommerceInit = initCommerceFlow;
  initCommerceFlow();
}());
