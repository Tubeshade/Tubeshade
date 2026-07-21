"use strict";

document.body.addEventListener('htmx:configRequest', removeEmptyParameters);

function removeEmptyParameters(event) {
    const parameters = event.detail.parameters;
    const emptyKeys = [];

    for (const entry of parameters.entries()) {
        if (entry[1] === "") {
            emptyKeys.push(entry[0]);
        }
    }

    for (const key of emptyKeys) {
        parameters.delete(key);
    }
}
