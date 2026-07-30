// MVCGrid.Net WebAssembly showcase — a single-page app.
//
// The .NET runtime boots ONCE. The MVCGrid engine runs in the browser (via the
// MVCGrid.Wasm host) and feeds the unchanged MVCGrid.js client script. A jQuery
// ajaxTransport answers MVCGrid.js's one AJAX call from WASM; a capture-phase click
// handler turns exports into Blob downloads. A hash router swaps pages into the
// content pane without ever rebooting the runtime.
//
// The pages replicate the classic MVC example (MVCGrid.Example.MvcWeb): same top
// navbar, same demo layout (sidebar + content), same panels and SyntaxHighlighter
// code styling. The toolbar markup comes from the shared MVCGrid.Example.Common
// DemoToolbar, so it is byte-identical to what the _MVCGridToolbar partial renders.

import { dotnet } from './_framework/dotnet.js';

const { getAssemblyExports, getConfig } = await dotnet.create();
const config = getConfig();
const M = (await getAssemblyExports(config.mainAssemblyName)).MVCGrid.Example.Wasm.Interop;

// Client loading hooks referenced by some demos (CustomLoading/Export). Define no-ops.
window.showLoading = window.showLoading || function () { };
window.hideLoading = window.hideLoading || function () { };

// Inject the (in-sync, token-substituted) MVCGrid.js once. It defines window.MVCGrid.
(function () {
    const s = document.createElement('script');
    s.textContent = M.GetClientScript();
    document.body.appendChild(s);
})();

installShim();

const catalog = JSON.parse(M.GetCatalogJson());
const byKey = {};
catalog.forEach(d => { byKey[d.key] = d; });

const HOME_NOTE = `<p class="text-muted">This site is running <strong>entirely in your browser</strong> via .NET
    WebAssembly — the same grid engine that powers the ASP.NET&nbsp;Core and classic MVC hosts is
    executing client-side here, so the interactive demos need no server.</p>`;

// Keep the column-visibility dropdown open while checking fields (the classic
// example does this with $('li').click(stopPropagation) on the home page).
jQuery('#content').on('click', '.dropdown-menu li', function (e) { e.stopPropagation(); });

window.addEventListener('hashchange', route);
route();

// ---------------------------------------------------------------- routing

function route() {
    const hash = location.hash || '#/';

    if (hash.startsWith('#/demo/')) {
        const key = hash.substring('#/demo/'.length).split('?')[0];
        const demo = byKey[key];
        if (demo) { renderDemoPage(demo); return; }
    }
    if (hash === '#/demos' || hash.startsWith('#/demos?')) {
        renderDemoPage(byKey['employees'] || catalog[0]);
        return;
    }
    if (hash.startsWith('#/gettingstarted') || hash.startsWith('#/docs/getting-started')) { renderGettingStarted(); return; }
    if (hash.startsWith('#/documentation') || hash.startsWith('#/docs/')) { renderDocumentation(); return; }
    if (hash.startsWith('#/download')) { renderDownload(); return; }
    if (hash.startsWith('#/support')) { renderSupport(); return; }
    if (hash.startsWith('#/detail')) {
        const id = new URLSearchParams(hash.split('?')[1] || '').get('id') || '';
        renderDetail(id);
        return;
    }
    renderHome();
}

function setPage(section, title, html) {
    document.title = title ? title + ' - MVCGrid.Net' : 'MVCGrid.Net';
    document.querySelectorAll('#topnav li').forEach(li => {
        li.classList.toggle('active', li.getAttribute('data-nav') === section);
    });
    document.getElementById('content').innerHTML = html;
    if (window.SyntaxHighlighter) { SyntaxHighlighter.highlight(); }
}

// ---------------------------------------------------------------- pages

function renderHome() {
    // Shared body (StaticPages/Pages/Home.html); the example grid is rendered in-process,
    // so init() must run after the HTML is in the DOM (same as the demo pages).
    history.replaceState(null, '', location.pathname + location.hash);
    setPage('home', 'Home Page', M.GetHomeHtml(HOME_NOTE, location.href));
    window.MVCGrid.init();
}

function renderDetail(id) {
    setPage('demos', 'Detail', M.GetDetailHtml(id));
}

// ---------------------------------------------------------------- demos (sidebar layout, like _DemoLayout.cshtml)

function renderDemoPage(demo) {
    // Shared body (StaticPages/Pages/Demo.html); the grid is rendered in-process, so init()
    // must run after the HTML is in the DOM. Drop any leftover ?grid-state from the previous
    // demo first so it doesn't leak into this one.
    history.replaceState(null, '', location.pathname + location.hash);
    setPage('demos', demo.title, M.GetDemoHtml(demo.key, location.href));
    window.MVCGrid.init();
}

// ---------------------------------------------------------------- docs pages

// Getting Started and Download bodies come from the shared MVCGrid.Example.Common
// StaticPages, so they are identical to what the Razor hosts render.
function renderGettingStarted() {
    setPage('gettingstarted', 'Getting Started', M.GetGettingStartedHtml('#/demos'));
}

function renderDownload() {
    setPage('download', 'Download', M.GetDownloadHtml());
}

function renderDocumentation() {
    // Shared body too; the four doc grids are rendered in-process by the WASM engine,
    // so init() must run after the HTML is in the DOM (same as the demo pages).
    history.replaceState(null, '', location.pathname + location.hash);
    setPage('documentation', 'Documentation', M.GetDocumentationHtml(location.href));
    window.MVCGrid.init();
}

function renderSupport() {
    setPage('support', 'Support', M.GetSupportHtml());
}

// ---------------------------------------------------------------- interop shim

function installShim() {
    // data requests: MVCGrid.js does $.ajax GET to "mvcgrid" + location.search.
    jQuery.ajaxTransport('+*', function (options) {
        const url = options.url || '';
        const path = url.split('?')[0].replace(/^\.?\//, '');
        if (path !== 'mvcgrid') return; // not ours
        return {
            send: function (headers, complete) {
                try { complete(200, 'success', { text: M.RenderData(url) }); }
                catch (e) { complete(500, 'error', { text: String((e && e.message) || e) }); }
            },
            abort: function () { }
        };
    });

    // export: intercept the click before MVCGrid's handler and download from WASM.
    document.addEventListener('click', function (e) {
        const btn = e.target.closest && e.target.closest('[data-mvcgrid-type="export"]');
        if (!btn) return;
        e.preventDefault();
        e.stopImmediatePropagation();
        const name = btn.getAttribute('data-mvcgrid-name');
        const res = JSON.parse(M.RenderExportJson(window.MVCGrid.getExportUrl(name)));
        const blob = new Blob([res.content], { type: res.contentType || 'text/csv' });
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = res.fileName || 'export.csv';
        document.body.appendChild(a);
        a.click();
        setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 0);
    }, true);
}
