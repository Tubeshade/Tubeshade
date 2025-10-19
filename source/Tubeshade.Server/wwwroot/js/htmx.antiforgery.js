// SPDX-License-Identifier: MIT
// Copyright © 2021 Khalid Abuhakmeh
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

if (!document.body.attributes.__htmx_antiforgery) {
    document.addEventListener("htmx:configRequest", evt => {
        if (evt.detail.verb.toUpperCase() === "GET") {
            return;
        }

        let antiForgery = htmx.config.antiForgery;
        if (!antiForgery) {
            return;
        }

        if (antiForgery.headerName) {
            evt.detail.headers[antiForgery.headerName] = antiForgery.requestToken;
        }

        if (!evt.detail.parameters[antiForgery.formFieldName]) {
            evt.detail.parameters[antiForgery.formFieldName] = antiForgery.requestToken;
        }
    });

    document.addEventListener("htmx:afterOnLoad", evt => {
        if (!evt.detail.boosted) {
            return;
        }

        const parser = new DOMParser();
        const html = parser.parseFromString(evt.detail.xhr.responseText, 'text/html');
        const selector = 'meta[name=htmx-config]';
        const config = html.querySelector(selector);
        if (!config) {
            return;
        }

        const current = document.querySelector(selector);
        const key = 'antiForgery';
        htmx.config[key] = JSON.parse(config.attributes['content'].value)[key];
        current.replaceWith(config);
    });

    document.body.attributes.__htmx_antiforgery = true;
}
