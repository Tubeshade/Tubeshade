"use strict";

export {getAccessToken, oidcAuthenticate};

async function getAccessToken() {
    let {
        access_token,
        access_token_expires_at
    } = await browser.storage.session.get(["access_token", "access_token_expires_at"]);

    if (access_token === undefined || access_token_expires_at === undefined) {
        console.debug("Access token not found, starting authentication flow");
        access_token = await oidcAuthenticate();
    } else if (access_token_expires_at <= Date.now()) {
        console.debug("Access token expired");
        const {refresh_token} = await browser.storage.local.get("refresh_token");

        if (refresh_token === undefined) {
            console.debug("Refresh token not found, starting authentication flow");
            access_token = await oidcAuthenticate();
        } else {
            console.debug("Refreshing access token");
            access_token = await refreshAccessToken(refresh_token);
        }
    }

    return access_token;
}

async function oidcAuthenticate() {
    const url = await beginCodeFlow();
    return await completeCodeFlow(url);
}

async function beginCodeFlow() {
    const scopes = ["openid", "email", "profile", "offline_access"];
    const config = await browser.storage.sync.get();

    const auth_state = crypto.randomUUID();
    await browser.storage.session.set({"auth_state": auth_state});

    const metadataResponse = await fetch(new Request(
        `${config.oidc_authority}/.well-known/openid-configuration`,
        {
            method: "GET",
            headers: {
                "Accept": "application/json",
            }
        }
    ));

    const openid_metadata = await metadataResponse.json();
    console.log(openid_metadata);

    const requestParams = new URLSearchParams({
        "client_id": config.oidc_client_id,
        "redirect_uri": browser.identity.getRedirectURL(),
        "scope": scopes.join(" "),
        "response_type": "code",
        "state": auth_state,
    })

    return await browser.identity.launchWebAuthFlow({
        interactive: true,
        url: `${openid_metadata.authorization_endpoint}?${requestParams.toString()}`,
    });
}

async function completeCodeFlow(redirectUrl) {
    const config = await browser.storage.sync.get();

    console.log(redirectUrl);
    const authorizationCode = new URL(redirectUrl).searchParams.get("code");

    let tokenRequest = new Request(
        `${config.oidc_authority}/protocol/openid-connect/token`, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
            },
            body: new URLSearchParams({
                "grant_type": "authorization_code",
                "client_id": config.oidc_client_id,
                "code": authorizationCode,
                "redirect_uri": browser.identity.getRedirectURL(),
            }),
            mode: "cors",
        })

    let tokenResponse = await fetch(tokenRequest);
    console.log(tokenResponse);
    let responseBody = await tokenResponse.json();

    await browser.storage.local.set({
        "refresh_token": responseBody.refresh_token,
        "refresh_expires_in": responseBody.refresh_expires_in,
        "refresh_scope": responseBody.scope,
    });

    await browser.storage.session.set({
        "access_token": responseBody.access_token,
        "access_token_expires_in": responseBody.expires_in,
        "access_token_expires_at": Date.now() + (responseBody.expires_in * 1000),
    });

    return responseBody.access_token;
}

async function refreshAccessToken(refresh_token) {
    const config = await browser.storage.sync.get();

    let tokenRequest = new Request(
        `${config.oidc_authority}/protocol/openid-connect/token`, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
            },
            body: new URLSearchParams({
                "grant_type": "refresh_token",
                "client_id": config.oidc_client_id,
                "refresh_token": refresh_token,
            }),
            mode: "cors",
        })

    let tokenResponse = await fetch(tokenRequest);
    console.log(tokenResponse);
    let responseBody = await tokenResponse.json();

    await browser.storage.session.set({
        "access_token": responseBody.access_token,
        "access_token_expires_in": responseBody.expires_in,
        "access_token_expires_at": Date.now() + (responseBody.expires_in * 1000),
    });

    return responseBody.access_token;
}