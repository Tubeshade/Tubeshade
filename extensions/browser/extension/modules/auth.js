"use strict";

export {oidcAuthenticate};

async function oidcAuthenticate() {
    const url = await beginCodeFlow();
    await completeCodeFlow(url);
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
    });
}