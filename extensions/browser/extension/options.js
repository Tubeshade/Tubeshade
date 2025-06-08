async function restoreOptions() {
    let config = await browser.storage.sync.get("server_url");
    document.querySelector("#server_url").value = config.server_url;
}

async function saveOptions(event) {
    event.preventDefault();
    await browser.storage.sync.set({
        server_url: document.querySelector("#server_url").value,
    });
}

document.addEventListener("DOMContentLoaded", restoreOptions);
document.querySelector("form").addEventListener("submit", saveOptions);
