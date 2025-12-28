"use strict";

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', _event => selectUserTimeZone())
} else {
    selectUserTimeZone();
}

function selectUserTimeZone() {
    const userTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone.trim();

    document.querySelectorAll("select.tubeshade-time-zone-select").forEach((element, _key, _parent) => {
        element.value = userTimeZone;
    });
}
