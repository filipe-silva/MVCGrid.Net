// Boots the .NET WebAssembly runtime and wires the MVCGrid client script to it.
//
// The trick: MVCGrid.js is unchanged. It makes exactly one kind of network call —
// a jQuery $.ajax GET to `<handlerPath> + location.search` — and navigates to an
// export URL. We intercept both and answer them from the in-browser .NET engine, so
// there is no server anywhere.

import { dotnet } from './_framework/dotnet.js';

const { getAssemblyExports, getConfig } = await dotnet.create();
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const M = exports.MVCGrid.WasmExample.Interop;

// 1) Inject the grid shell + preloaded first page (the @Html.MVCGrid equivalent).
document.getElementById('grid-host').innerHTML = M.GetBasePageHtml('peopleGrid', location.href);

// 2) Install the intercepts BEFORE MVCGrid.init() runs.
installShim(M);

// 3) Inject the (in-sync, token-substituted) client script. Its `$(function(){ MVCGrid.init() })`
//    runs on eval (DOM is already ready), finds the container and binds the toolbar.
const script = document.createElement('script');
script.textContent = M.GetClientScript();
document.body.appendChild(script);

// NB: we deliberately do NOT call dotnet.run(). That would execute Program.Main and then
// *exit* the runtime, after which further [JSExport] calls fail with "runtime already
// exited". Grid registration is lazy (Interop.EnsureInitialized), so the entry point isn't
// needed — leaving the runtime resident to answer every subsequent render call.

function installShim(M) {
    // --- data requests ---
    // Prepend ("+") a catch-all ("*") jQuery transport. It handles only requests to our
    // handler path and returns undefined for everything else (so normal AJAX still works).
    jQuery.ajaxTransport('+*', function (options) {
        const url = options.url || '';
        const path = url.split('?')[0].replace(/^\.?\//, '');
        if (path !== 'mvcgrid') {
            return; // not ours
        }
        return {
            send: function (headers, complete) {
                try {
                    complete(200, 'success', { text: M.RenderData(url) });
                } catch (e) {
                    complete(500, 'error', { text: String((e && e.message) || e) });
                }
            },
            abort: function () { }
        };
    });

    // --- export ---
    // MVCGrid.js exports by navigating to `location.href = getExportUrl(...)`, which would
    // 404 on a static host. Intercept the click (capture phase, before MVCGrid's own
    // handler), render the CSV in-browser and trigger a Blob download instead.
    document.addEventListener('click', function (e) {
        const btn = e.target.closest && e.target.closest('[data-mvcgrid-type="export"]');
        if (!btn) {
            return;
        }
        e.preventDefault();
        e.stopImmediatePropagation();

        const name = btn.getAttribute('data-mvcgrid-name');
        const url = window.MVCGrid.getExportUrl(name); // same URL MVCGrid would have navigated to
        const res = JSON.parse(M.RenderExportJson(url));

        const blob = new Blob([res.content], { type: res.contentType || 'text/csv' });
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = res.fileName || 'export.csv';
        document.body.appendChild(a);
        a.click();
        setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 0);
    }, true);
}
