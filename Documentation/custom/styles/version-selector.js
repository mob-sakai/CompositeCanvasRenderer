$(function () {
    run();
    window.refresh += run;

    function run() {
        // Find metadata.
        const offline = location.hostname === "localhost" || location.hostname === "127.0.0.1";
        const requestURL = '../metadata.json';
        $.getJSON(requestURL, m => {
            if (offline) {
                generateVersionSelector(m);
            } else {
                $.getJSON(m.metadataUrl, generateVersionSelector)
            }
        });

        // Open/close version selector.
        $(document).click(function (e) {
            if (e.target.id == 'component-select-current-display')
                $('#component-select-current-display').toggleClass('component-select__current--is-active');
            else
                $('#component-select-current-display').removeClass('component-select__current--is-active');
        });
    }

    function generateVersionSelector(metadata) {
        if (!metadata) return;

        metadata.versions.forEach(v => {
            $('#version-select-ul').append($(`
            <a style="color:#000;" href="${v.url}">
                <li class="component-select__option" style='justify-content:space-between;'>
                    ${v.version} <span style="color:#aaa;">${v.unity}+</span>
                </li>
            </a>
        `));
        });
    }
});
