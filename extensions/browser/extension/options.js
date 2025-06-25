"use strict";

import {oidcAuthenticate} from "./modules/auth.js";

async function setDefaultOptions() {
    let config = await browser.storage.local.get();
    if (config.server_url === undefined) {
        await browser.storage.sync.set({"server_url": "http://localhost:5201"})
    }

    if (config.oidc_client_id === undefined) {
        await browser.storage.sync.set({"oidc_client_id": "tubeshade-browser-extension"})
    }
}

async function restoreOptions() {
    await setDefaultOptions();

    let config = await browser.storage.sync.get();
    document.querySelector("#server_url").value = config.server_url ?? "";
    document.querySelector("#oidc_authority").value = config.oidc_authority ?? "";
    document.querySelector("#oidc_client_id").value = config.oidc_client_id ?? "";
}

async function saveOptions() {
    await browser.storage.sync.set({
        server_url: document.querySelector("#server_url").value,
        library_id: document.querySelector("#library").value,
        oidc_authority: document.querySelector("#oidc_authority").value,
        oidc_client_id: document.querySelector("#oidc_client_id").value,
    });
}

async function verifyOptions() {
    await saveOptions();
    await oidcAuthenticate();

    let server_url = document.querySelector("#server_url").value;
    const response = await fetch(
        `${server_url}/api/v1.0/libraries`,
        {
            method: "GET",
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json",
                "Authorization": `Bearer ${(await browser.storage.session.get("access_token")).access_token}`,
                mode: "cors",
            },
        });
    const json = await response.json();

    const librarySelect = document.querySelector("select");
    for (let option = librarySelect.options.length - 1; option >= 1; option--) {
        librarySelect.remove(option);
    }

    for (var library of json) {
        const option = document.createElement("option");
        option.value = library.id;
        option.text = library.name;
        librarySelect.add(option, null);
    }
}

document.addEventListener("DOMContentLoaded", restoreOptions);
document.querySelector("form").addEventListener("submit", saveOptions);
document.querySelector("#verify_config").addEventListener("click", verifyOptions);
