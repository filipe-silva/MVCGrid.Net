// MVCGrid.Net WebAssembly showcase — a single-page app.
//
// The .NET runtime boots ONCE. The MVCGrid engine runs in the browser (via the
// MVCGrid.Wasm host) and feeds the unchanged MVCGrid.js client script. A jQuery
// ajaxTransport answers MVCGrid.js's one AJAX call from WASM; a capture-phase click
// handler turns exports into Blob downloads. A hash router swaps demos/docs into the
// content pane without ever rebooting the runtime.

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

renderNav();
window.addEventListener('hashchange', route);
route();

// ---------------------------------------------------------------- routing

function route() {
    const hash = location.hash || '#/';
    setActiveNav(hash);

    if (hash.startsWith('#/demo/')) {
        const key = hash.substring('#/demo/'.length).split('?')[0];
        const demo = byKey[key];
        if (demo) { mountDemo(demo); return; }
    }
    if (hash.startsWith('#/docs/')) {
        renderDoc(hash.substring('#/docs/'.length).split('?')[0]);
        return;
    }
    if (hash.startsWith('#/detail')) {
        const id = new URLSearchParams(hash.split('?')[1] || '').get('id') || '';
        renderDetail(id);
        return;
    }
    renderHome();
}

// ---------------------------------------------------------------- nav

function renderNav() {
    const groups = {};
    const order = [];
    catalog.forEach(d => {
        if (!groups[d.category]) { groups[d.category] = []; order.push(d.category); }
        groups[d.category].push(d);
    });

    let html = '<li><a href="#/">Home</a></li>';
    html += '<li class="nav-header">Docs</li>';
    html += '<li><a href="#/docs/getting-started">Getting started</a></li>';
    html += '<li><a href="#/docs/documentation">Documentation</a></li>';
    order.forEach(cat => {
        html += `<li class="nav-header">${esc(cat)}</li>`;
        groups[cat].forEach(d => {
            html += `<li><a href="#/demo/${esc(d.key)}">${esc(d.title)}</a></li>`;
        });
    });
    document.getElementById('nav').innerHTML = html;
}

function setActiveNav(hash) {
    document.querySelectorAll('#nav li').forEach(li => li.classList.remove('active'));
    const link = document.querySelector(`#nav a[href="${hash}"]`);
    if (link) link.parentElement.classList.add('active');
}

// ---------------------------------------------------------------- pages

function renderHome() {
    document.getElementById('content').innerHTML = `
        <h1>MVCGrid.Net</h1>
        <p class="lead">A server-side data grid for ASP.NET — AJAX paging, sorting, filtering,
        column visibility and CSV export.</p>
        <p>Everything on this site is running <strong>entirely in your browser</strong> via .NET
        WebAssembly. The same grid engine that powers the ASP.NET&nbsp;Core and classic MVC hosts
        is executing client-side here (the <code>MVCGrid.Wasm</code> host feeding the unchanged
        <code>MVCGrid.js</code>), so the interactive demos need no server.</p>
        <p>Pick a demo from the sidebar, or start with the
        <a href="#/demo/overview">Overview</a> grid. Each demo shows the exact grid configuration
        code below it.</p>
        <p class="text-muted">Grids are defined once in the shared <code>MVCGrid.Example.Common</code>
        library and reused by all three example hosts.</p>`;
}

function renderDetail(id) {
    document.getElementById('content').innerHTML = `
        <h1>Person ${esc(id)}</h1>
        <p>This is the detail page linked from a grid's <strong>View</strong> column — the link was
        built by the grid's <code>UrlHelper.Action(...)</code> call and routed here client-side.</p>
        <p><a class="btn btn-default" onclick="history.back()">&laquo; Back</a></p>`;
}

function mountDemo(demo) {
    // Drop any leftover ?grid-state from the previous demo so it doesn't leak into this one.
    history.replaceState(null, '', location.pathname + location.hash);

    const content = document.getElementById('content');
    content.innerHTML = `
        <h1>${esc(demo.title)}</h1>
        <p class="demo-blurb">${esc(demo.blurb)}</p>
        ${toolbar(demo)}
        <div id="grid-host"></div>
        <div class="code-panel">
            <h4>Grid configuration</h4>
            <pre><code id="snippet">loading…</code></pre>
        </div>`;

    document.getElementById('grid-host').innerHTML = M.GetBasePageHtml(demo.gridName, location.href);
    window.MVCGrid.init();

    document.getElementById('snippet').textContent = M.GetCodeSnippet(demo.gridName) || '(source not found)';
}

function toolbar(demo) {
    const parts = [];
    const g = esc(demo.gridName);
    if (demo.search) {
        parts.push(`<input type="text" class="form-control" placeholder="search…"
            data-mvcgrid-type="additionalQueryOption" data-mvcgrid-option="${esc(demo.searchOption)}"
            data-mvcgrid-name="${g}" data-mvcgrid-apply-additional="keyup" />`);
    }
    if (demo.pageSize) {
        parts.push(`<label>Page size:
            <select class="form-control" data-mvcgrid-type="pageSize" data-mvcgrid-name="${g}">
                <option>10</option><option>25</option><option>50</option>
            </select></label>`);
    }
    if (demo.columnVisibility) {
        parts.push(`<div class="dropdown" style="display:inline-block;">
            <button class="btn btn-default dropdown-toggle" data-toggle="dropdown">Columns <span class="caret"></span></button>
            <ul class="dropdown-menu" data-mvcgrid-type="columnVisibilityList" data-mvcgrid-name="${g}"></ul>
        </div>`);
    }
    if (demo.export) {
        parts.push(`<button class="btn btn-default" data-mvcgrid-type="export" data-mvcgrid-name="${g}">Export CSV</button>`);
    }
    if (parts.length === 0) return '';
    return `<div class="toolbar form-inline">${parts.join(' ')}</div>`;
}

// ---------------------------------------------------------------- docs

function renderDoc(page) {
    const content = document.getElementById('content');
    if (page === 'getting-started') {
        content.innerHTML = `
            <h1>Getting started</h1>
            <p>Install the package for your host, register grids at startup, include the client
            script after jQuery, and render with <code>@Html.MVCGrid("GridName")</code>.</p>
            <h3>ASP.NET Core</h3>
            <pre><code>builder.Services.AddMVCGrid(o =&gt; { o.HandlerPath = "/mvcgrid"; });
// register grids into MVCGridDefinitionTable at startup
app.MapMVCGrid();</code></pre>
            <p>In your layout, after jQuery: <code>&lt;script src="/mvcgrid/script.js"&gt;&lt;/script&gt;</code>.</p>
            <h3>Classic ASP.NET MVC</h3>
            <p>Register the <code>MVCGridHandler.axd</code> handler in Web.config, include
            <code>~/MVCGridHandler.axd/script.js</code> after jQuery, and register grids in
            <code>Application_Start</code>.</p>
            <h3>Defining a grid</h3>
            <pre><code>MVCGridDefinitionTable.Add("MyGrid", new MVCGridBuilder&lt;Person&gt;()
    .WithAuthorizationType(AuthorizationType.AllowAnonymous)
    .WithSorting(true, "LastName")
    .WithPaging(true, 10)
    .AddColumns(cols =&gt; {
        cols.Add("FirstName").WithValueExpression(p =&gt; p.FirstName);
        cols.Add("LastName").WithValueExpression(p =&gt; p.LastName);
    })
    .WithRetrieveDataMethod(context =&gt; {
        // honour context.QueryOptions for sorting/paging/filtering
        return new QueryResult&lt;Person&gt; { Items = ..., TotalRecords = ... };
    }));</code></pre>`;
        return;
    }
    // documentation (default)
    content.innerHTML = `
        <h1>Documentation</h1>
        <h3>Key concepts</h3>
        <ul>
          <li><strong>RetrieveData owns all data access.</strong> Your callback honours
          <code>context.QueryOptions</code> for sorting/paging/filtering and sets
          <code>TotalRecords</code> when paging is enabled.</li>
          <li><strong>Two-level features.</strong> Sorting/filtering must be enabled at the grid
          level <em>and</em> per column.</li>
          <li><strong>Per-grid query-string prefixes</strong> let multiple grids share a page.</li>
        </ul>
        <h3>Client-side API (<code>MVCGrid.*</code>)</h3>
        <table class="table table-bordered table-condensed">
          <thead><tr><th>Method</th><th>Purpose</th></tr></thead>
          <tbody>
            <tr><td><code>reloadGrid(name)</code></td><td>Re-fetch and re-render the grid.</td></tr>
            <tr><td><code>setSort(name, col, dir)</code></td><td>Sort by a column.</td></tr>
            <tr><td><code>setPage(name, n)</code></td><td>Go to a page.</td></tr>
            <tr><td><code>setPageSize(name, n)</code></td><td>Change rows per page.</td></tr>
            <tr><td><code>setFilters(name, {})</code> / <code>getFilters(name)</code></td><td>Get/set column filters.</td></tr>
            <tr><td><code>setColumnVisibility(name, {})</code></td><td>Show/hide columns.</td></tr>
            <tr><td><code>getExportUrl(name)</code></td><td>URL for the CSV export.</td></tr>
          </tbody>
        </table>
        <h3>Declarative bindings (<code>data-mvcgrid-*</code>)</h3>
        <table class="table table-bordered table-condensed">
          <thead><tr><th>Attribute</th><th>Meaning</th></tr></thead>
          <tbody>
            <tr><td><code>data-mvcgrid-type</code></td><td>filter | additionalQueryOption | pageSize | export | columnVisibilityList</td></tr>
            <tr><td><code>data-mvcgrid-name</code></td><td>Which grid the control drives.</td></tr>
            <tr><td><code>data-mvcgrid-option</code></td><td>Column name (filter) or option name (additional query option).</td></tr>
            <tr><td><code>data-mvcgrid-apply-filter</code> / <code>-apply-additional</code></td><td>DOM event that applies it (e.g. <code>keyup</code>).</td></tr>
          </tbody>
        </table>
        <p class="text-muted">Every demo in the sidebar shows its own configuration code.</p>`;
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

function esc(s) {
    return String(s == null ? '' : s)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
