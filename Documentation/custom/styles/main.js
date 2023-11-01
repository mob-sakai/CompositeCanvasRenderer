$(function () {
  collapsibleSections();

  window.refresh = function (_) {
    collapsibleSections();
  };

  function addClassToElements(elements, ...classNames) {
    for (var i = 0; i < elements.length; i++) {
      for (var j = 0; j < classNames.length; j++) {
        elements[i].classList.add(classNames[j]);
      }
    }
  }

  function setAsLastSibling(elements) {
    for (var i = 0; i < elements.length; i++) {
      const element = elements[i];
      const parent = element.parentElement;

      parent.appendChild(element);
    }
  }

  //Enable collapsible sections: applies to divs that are children of a collapsible class element
  function collapsibleSections() {
    addClassToElements(
      document.getElementsByClassName("inheritance"),
      "collapsible",
      "show-bottom",
    );
    addClassToElements(
      document.getElementsByClassName("inheritedMembers"),
      "collapsible",
    );
    setAsLastSibling(document.getElementsByClassName("inheritedMembers"));

    var collapsibles = document.getElementsByClassName("collapsible");
    for (var i = 0; i < collapsibles.length; i++) {
      if (collapsibles[i].children.length > 6) {
        collapsibles[i].classList.add("closed");
        if (collapsibles[i].classList.contains("show-bottom")) {
          const count = collapsibles[i].children.length;
          for (var j = count - 1; j > count - 4; j--) {
            collapsibles[i].children[j].classList.add("initial");
          }
          collapsibles[i].children[1].classList.add("initial");
        } else {
          for (var j = 1; j < 6; j++) {
            collapsibles[i].children[j].classList.add("initial");
          }
        }
      }
      collapsibles[i].addEventListener("click", function (evt) {
        if (evt.target.tagName != "A") {
          this.classList.toggle("closed");
          for (var j = 1; j < this.children.length; j++) {
            this.children[j].classList.remove("initial");
          }
        }
      });
    }
  }
});
