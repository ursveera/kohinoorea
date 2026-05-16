(function () {
  function initHomeHeroStack() {
    var stage = document.querySelector("[data-home-hero-stage]");
    if (!stage || stage.hasAttribute("data-stage-bound")) {
      return;
    }

    var cards = Array.prototype.slice.call(stage.querySelectorAll("[data-stage-card]"));
    if (cards.length < 2) {
      return;
    }

    stage.setAttribute("data-stage-bound", "true");
    var order = cards.slice();

    function render() {
      order.forEach(function (card, index) {
        card.classList.remove("is-back", "is-middle", "is-front");

        if (index === order.length - 1) {
          card.classList.add("is-front");
        } else if (index === order.length - 2) {
          card.classList.add("is-middle");
        } else {
          card.classList.add("is-back");
        }
      });
    }

    function moveToFront(card) {
      var cardIndex = order.indexOf(card);
      if (cardIndex === -1) {
        return;
      }

      if (cardIndex === order.length - 1) {
        order.unshift(order.pop());
      } else {
        order.splice(cardIndex, 1);
        order.push(card);
      }

      render();
    }

    cards.forEach(function (card) {
      card.addEventListener("click", function () {
        moveToFront(card);
      });

      card.addEventListener("keydown", function (event) {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          moveToFront(card);
        }
      });
    });

    render();
  }

  window.kohinoorHeroInit = initHomeHeroStack;
  initHomeHeroStack();
}());