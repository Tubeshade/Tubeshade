'use strict';

console.log("Starting background.js")

let browserType = getBrowser();

function getBrowser() {
    if (typeof chrome !== 'undefined') {
        if (typeof browser !== 'undefined') {
            return browser;
        } else {
            return chrome;
        }
    } else {
        console.log('Failed to detect browser');
        throw 'browser detection error';
    }
}

let config = {
    "redirectUrl": browserType.identity.getRedirectURL(),
    "clientId": "tubeshade-browser-extension",
    "scopes": ["openid", "email", "profile"],
    "baseUrl": "https://keycloak.services.home.vmelnalksnis.lv/realms/Home"
};

console.log(config)

const authUrl = `${config.baseUrl}/protocol/openid-connect/auth?client_id=${encodeURIComponent(config.clientId)}&response_type=token&redirect_uri=${encodeURIComponent(config.redirectUrl)}&scope=${encodeURIComponent(config.scopes.join(' '))}`;

function extractAccessToken(redirectUri) {
    console.log(redirectUri);
    let m = redirectUri.match(/[#?](.*)/);
    if (!m || m.length < 1)
        return null;
    let params = new URLSearchParams(m[1].split("#")[0]);
    console.log(params);
    return params.get("access_token");
}

/**
 Validate the token contained in redirectURL.
 This follows essentially the process here:
 https://developers.google.com/identity/protocols/OAuth2UserAgent#tokeninfo-validation
 - make a GET request to the validation URL, including the access token
 - if the response is 200, and contains an "aud" property, and that property
 matches the clientID, then the response is valid
 - otherwise it is not valid

 Note that the Google page talks about an "audience" property, but in fact
 it seems to be "aud".
 */
async function validate(redirectURL) {
    const accessToken = extractAccessToken(redirectURL);
    if (!accessToken) {
        throw "Authorization failure";
    }

    await browser.storage.session.set({"access_token": accessToken});

    const validationURL = `${config.baseUrl}/protocol/openid-connect/token?access_token=${accessToken}`;
    const validationRequest = new Request(validationURL, {
        method: "GET",
    });

    function checkResponse(response) {
        return new Promise((resolve, reject) => {
            if (response.status !== 200) {
                reject("Token validation error");
            }
            response.json().then((json) => {
                if (json.aud && (json.aud === config.clientId)) {
                    console.log("Valid auth token")
                    resolve(accessToken);
                } else {
                    reject("Token validation error");
                }
            });
        });
    }

    return fetch(validationRequest).then(checkResponse);
}

/**
 Authenticate and authorize using browser.identity.launchWebAuthFlow().
 If successful, this resolves with a redirectURL string that contains
 an access token.
 */
function authorize() {

    console.log(authUrl)
    return browser.identity.launchWebAuthFlow({
        interactive: true,
        url: authUrl
    });
}

function getAccessToken() {
    return authorize().then(validate);
}

getAccessToken();

let cookieListenerEnabled = false;
let cookieSyncInProgress = false;

syncCookies();
handleContinuousCookieSync(true);

browserType.runtime.onStartup.addListener(() => {
    browserType.storage.local.get("continuousSync", data => {
        handleContinuousCookieSync(data?.continuousSync?.checked || true);
    });
});

function handleContinuousCookieSync(enabled) {
    if (enabled === true) {
        browserType.cookies.onChanged.addListener(onCookieChange);
        cookieListenerEnabled = true;
        console.log("Enabled continuous cookie sync");
    } else {
        browserType.cookies.onChanged.removeListener(onCookieChange);
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
    const path = "api/v1.0/cookies";
    const cookieLines = await getCookieLines();
    const requestData = {
        libraryId: "f63dfbec-5536-44af-af62-6ad8d1734f46",
        domain: "youtube.com",
        cookie: cookieLines.join('\n'),
    }

    await sendData(path, requestData, "POST");
}

async function getCookieLines() {
    const acceptableDomains = ['.youtube.com', 'youtube.com', 'www.youtube.com'];
    let cookieStores = await browserType.cookies.getAllCookieStores();
    let cookieLines = [
        '# Netscape HTTP Cookie File',
        '# https://curl.haxx.se/rfc/cookie_spec.html',
        '# This is a generated file! Do not edit.\n',
    ];

    for (let i = 0; i < cookieStores.length; i++) {
        const cookieStore = cookieStores[i];
        let allCookiesStore = await browserType.cookies.getAll({
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
    const baseUrl =  await browser.storage.sync.get("server_url");
    console.log(baseUrl);
    const url = `${baseUrl.server_url}/${path}`;
    console.log(`Sending ${method} request to ${url}`);

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json",
                "Authorization": `Bearer ${(await browser.storage.session.get("access_token")).access_token}`,
                mode: "cors",
            },
            body: JSON.stringify(payload),
        });

        if (response.status !== 204) {
            console.error(await response.text());
            return null;
        }

        return await response.json();
    } catch (error) {
        console.log(error);
        return null;
    }
}
