"use strict";

export {getMaximumResolution, setMaximumResolution, setPreferredResolution};

const resolutionKey = "maximumVideoResolution";
const resolutionCookie = "Preferences.MaximumVideoResolution";
const cookieMaxAge = 60 * 60 * 24 * 365;

function getMaximumResolution() {
    return localStorage.getItem(resolutionKey);
}

function setMaximumResolution(resolution) {
    if (resolution === null) {
        localStorage.removeItem(resolutionKey);
        setResolutionCookie(null);
    } else if (typeof resolution === "string") {
        localStorage.setItem(resolutionKey, resolution);
        setResolutionCookie(resolution);
    } else {
        throw new TypeError(`Expected resolution to be a string, but got ${typeof resolution}`);
    }
}

function setResolutionCookie(resolution) {
    document.cookie = resolution === null
        ? `${resolutionCookie}=; path=/; max-age=0; samesite=lax`
        : `${resolutionCookie}=${encodeURIComponent(resolution)}; path=/; max-age=${cookieMaxAge}; samesite=lax`;
}

function setPreferredResolution(select, fileId) {
    setResolutionCookie(getMaximumResolution());

    if (fileId !== null && fileId.length > 0) {
        for (let index = 0; index < select.options.length; index++){
            if (select.options[index].value.endsWith(fileId)){
                select.selectedIndex = index;
                select.dispatchEvent(new Event("change"));
                return;
            }
        }
    }

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
            select.dispatchEvent(new Event("change"));
            return;
        }
    }
}
