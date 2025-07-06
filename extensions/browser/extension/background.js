"use strict";

import {getAccessToken} from "./modules/auth.js";

console.log("Starting background.js")

browser.runtime.onInstalled.addListener(async function (_details) {
    await browser.runtime.openOptionsPage()
})

let cookieListenerEnabled = false;
let cookieSyncInProgress = false;
handleContinuousCookieSync(true);

browser.runtime.onStartup.addListener(() => {
    browser.storage.local.get("continuousSync", data => {
        handleContinuousCookieSync(data?.continuousSync?.checked || true);
    });
});

function handleContinuousCookieSync(enabled) {
    if (enabled === true) {
        browser.cookies.onChanged.addListener(onCookieChange);
        cookieListenerEnabled = true;
        console.log("Enabled continuous cookie sync");
    } else {
        browser.cookies.onChanged.removeListener(onCookieChange);
        cookieListenerEnabled = false;
        console.log("Disabled continuous cookie sync");
    }
}

function onCookieChange(changeInfo) {
    console.log(`Cookie change event detected for cookie ${changeInfo.cookie?.name} in domain ${changeInfo.cookie?.domain}`);

    if (!cookieSyncInProgress) {
        cookieSyncInProgress = true;
        syncCookies();
        setTimeout(() => cookieSyncInProgress = false, 10_000);
    } else {
        console.log("Skipping cookie change event, already syncing");
    }
}

async function syncCookies() {
    const config = await browser.storage.sync.get();
    const path = "api/v1.0/cookies";
    const cookieLines = await getCookieLines();
    const requestData = {
        libraryId: config.library_id,
        domain: "youtube.com",
        cookie: cookieLines.join('\n'),
    }

    await sendData(path, requestData, "POST");
}

async function getCookieLines() {
    const acceptableDomains = ['.youtube.com', 'youtube.com', 'www.youtube.com'];
    let cookieStores = await browser.cookies.getAllCookieStores();
    let cookieLines = [
        '# Netscape HTTP Cookie File',
        '# https://curl.haxx.se/rfc/cookie_spec.html',
        '# This is a generated file! Do not edit.\n',
    ];

    for (let i = 0; i < cookieStores.length; i++) {
        const cookieStore = cookieStores[i];
        let allCookiesStore = await browser.cookies.getAll({
            domain: '.youtube.com',
            storeId: cookieStore['id'],
        });
        for (let j = 0; j < allCookiesStore.length; j++) {
            const cookie = allCookiesStore[j];
            if (acceptableDomains.includes(cookie.domain)) {
                cookieLines.push(buildCookieLine(cookie));
            }
        }
    }

    return cookieLines;
}

function buildCookieLine(cookie) {
    // 2nd argument controls subdomains, and must match leading dot in domain
    let includeSubdomains = cookie.domain.startsWith('.') ? 'TRUE' : 'FALSE';

    return [
        cookie.domain,
        includeSubdomains,
        cookie.path,
        cookie.httpOnly.toString().toUpperCase(),
        Math.trunc(cookie.expirationDate) || 0,
        cookie.name,
        cookie.value,
    ].join('\t');
}

async function sendData(path, payload, method) {
    const baseUrl = await browser.storage.sync.get("server_url");
    const accessToken = await getAccessToken();
    console.log(baseUrl);

    if (baseUrl.server_url === undefined) {
        console.log("Tubeshade server URL not configured");
        return null;
    }

    const url = `${baseUrl.server_url}/${path}`;
    console.log(`Sending ${method} request to ${url}`);

    try {
        await fetch(url, {
            method: method,
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json",
                "Authorization": `Bearer ${accessToken}`,
                mode: "cors",
            },
            body: JSON.stringify(payload),
        });
    } catch (error) {
        console.log(error);
        return null;
    }
}
