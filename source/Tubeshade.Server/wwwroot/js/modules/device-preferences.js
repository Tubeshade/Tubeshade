"use strict";

export {getMaximumResolution, setMaximumResolution, setPreferredResolution};

const resolutionKey = "maximumVideoResolution";

function getMaximumResolution() {
    return localStorage.getItem(resolutionKey);
}

function setMaximumResolution(resolution) {
    if (resolution === null) {
        localStorage.removeItem(resolutionKey);
    } else if (typeof resolution === "string") {
        localStorage.setItem(resolutionKey, resolution);
    } else {
        throw new TypeError(`Expected resolution to be a string, but got ${typeof resolution}`);
    }
}

function setPreferredResolution(select) {
    const preferred = getMaximumResolution();
    if (preferred === null) {
        return;
    }

    const resolution = parseInt(preferred);
    if (isNaN(resolution)) {
        return;
    }

    const available = Array
        .from(select.options)
        .map(option => parseInt(option.label.split("@")[0]))
        .filter(optionResolution => optionResolution <= resolution);

    if (available.length === 0) {
        return;
    }

    const selected = Math.max(...available).toString();

    for (let index = 0; index < select.options.length; index++) {
        if (select.options[index].label.startsWith(selected)) {
            select.selectedIndex = index;
            return;
        }
    }
}
