$(function () {
    run();
    window.refresh += run;

    function run() {
        setAsCollapsible($(".inheritance")[0], true);
        setAsCollapsible($(".inheritedMembers")[0], false);
        setSiblingAsLast($(".inheritedMembers")[0]);
    }

    /*
     * Set element as last sibling.
     */
    function setSiblingAsLast(element) {
        if (!element) return;

        const parent = element.parentElement;
        parent.appendChild(element);
    }

    /*
     * Set element as collapsible.
     */
    function setAsCollapsible(element, showBottom) {
        if (!element || element.children.length < 0) return;

        // Add collapsible class.
        element.classList.add("collapsible");
        element.classList.add("closed");

        // Add click event.
        const children = Array.from(element.children);
        element.addEventListener("click", function (evt) {
            if (evt.target.tagName == "A") return;
            element.classList.toggle("closed");
            children.forEach(c => c.classList.remove("initial"));
        });

        // Initial state.
        if (showBottom) {
            element.classList.add("show-bottom");
            children.slice(-4, -1).forEach(c => c.classList.add("initial"));
            children[1].classList.add("initial");
        } else {
            children.slice(1, 6).forEach(c => c.classList.add("initial"));
        }
    }
});
