"use strict";

// Images that are still loading occupy connections and bandwidth, which on slow connections delays htmx requests
// until the images have loaded. Removing the sources lets the browser cancel them.
const cancelledImages = new WeakMap();

document.addEventListener("htmx:beforeRequest", event => {
    const {target, xhr} = event.detail;
    if (!(target instanceof Element)) {
        return;
    }

    const images = Array
        .from(target.querySelectorAll("img"))
        .filter(image => !image.complete);

    images.forEach(cancelImage);
    if (images.length > 0) {
        cancelledImages.set(xhr, images);
    }
});

document.addEventListener("htmx:afterRequest", event => {
    const xhr = event.detail.xhr;
    const images = cancelledImages.get(xhr);
    if (images === undefined) {
        return;
    }

    cancelledImages.delete(xhr);
    images
        .filter(image => image.isConnected)
        .forEach(restoreImage);
});

document.addEventListener("htmx:historyRestore", () => {
    document
        .querySelectorAll("img[data-cancelled-src], img[data-cancelled-srcset]")
        .forEach(restoreImage);
});

document.addEventListener("htmx:beforeCleanupElement", event => {
    const element = event.target;
    if (element instanceof HTMLImageElement && !element.complete) {
        cancelImage(element);
    }
});

function cancelImage(image) {
    for (const name of ["srcset", "src"]) {
        const value = image.getAttribute(name);
        if (value !== null) {
            image.setAttribute(`data-cancelled-${name}`, value);
            image.removeAttribute(name);
        }
    }
}

function restoreImage(image) {
    for (const name of ["srcset", "src"]) {
        const value = image.getAttribute(`data-cancelled-${name}`);
        if (value !== null) {
            image.setAttribute(name, value);
            image.removeAttribute(`data-cancelled-${name}`);
        }
    }
}
