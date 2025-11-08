"use strict";

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', _event => updateTimes())
} else {
    updateTimes();
}

function updateTimes() {
    document.querySelectorAll("time").forEach((element, _key, _parent) => {
        const date = new Date(element.dateTime);
        element.innerText = `${date.getFullYear()}-${zeroPad(date.getMonth() + 1, 2)}-${zeroPad(date.getDate(), 2)}`;
    });
}

function zeroPad(value, length) {
    return `${value}`.padStart(length, "0");
}
