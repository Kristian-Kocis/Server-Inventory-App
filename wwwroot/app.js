const rows = document.querySelector("#server-rows");
const summaryFields = {
  total: document.querySelector("#total-count"),
  running: document.querySelector("#running-count"),
  stopped: document.querySelector("#stopped-count"),
  maintenance: document.querySelector("#maintenance-count")
};
const countLabel = document.querySelector("#server-count");
const tableMessage = document.querySelector("#table-message");
const searchInput = document.querySelector("#search");
const statusFilter = document.querySelector("#status-filter");
const dialog = document.querySelector("#server-dialog");
const form = document.querySelector("#server-form");
const formError = document.querySelector("#form-error");
const saveButton = document.querySelector("#save-server");

let servers = [];
let editingId = null;
let searchTimeout;
let csrfToken;

async function request(url, options) {
  const requestOptions = { credentials: "same-origin", ...options };
  const method = (requestOptions.method || "GET").toUpperCase();
  if (!["GET", "HEAD", "OPTIONS"].includes(method)) {
    requestOptions.headers = { ...requestOptions.headers, "X-CSRF-TOKEN": await getCsrfToken() };
  }
  const response = await fetch(url, requestOptions);
  if (response.status === 401) {
    window.location.replace("/login.html");
    throw new Error("Your session has expired. Sign in again.");
  }
  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try {
      const body = await response.json();
      message = body.detail || body.title || message;
      if (body.errors) {
        message = Object.values(body.errors).flat().join(" ");
      }
    } catch {
      // Keep the status-based message when the response has no JSON body.
    }
    throw new Error(message);
  }
  if (response.status === 204) return null;
  return response.json();
}

async function getCsrfToken() {
  if (!csrfToken) {
    const response = await fetch("/api/auth/csrf", { credentials: "same-origin" });
    if (!response.ok) throw new Error("Unable to initialize request security. Reload the page and try again.");
    csrfToken = (await response.json()).requestToken;
  }
  return csrfToken;
}

async function loadInventory() {
  tableMessage.textContent = "";
  try {
    const params = new URLSearchParams();
    if (searchInput.value.trim()) params.set("search", searchInput.value.trim());
    if (statusFilter.value) params.set("status", statusFilter.value);
    const query = params.size ? `?${params}` : "";
    const [summary, inventory] = await Promise.all([
      request("/api/servers/summary"),
      request(`/api/servers${query}`)
    ]);
    servers = inventory;
    summaryFields.total.textContent = summary.total;
    summaryFields.running.textContent = summary.running;
    summaryFields.stopped.textContent = summary.stopped;
    summaryFields.maintenance.textContent = summary.maintenance;
    countLabel.textContent = `${inventory.length} ${inventory.length === 1 ? "server" : "servers"}`;
    renderServers(inventory);
  } catch (error) {
    rows.innerHTML = '<tr><td class="loading-cell" colspan="6">Unable to load server inventory.</td></tr>';
    tableMessage.textContent = error.message;
  }
}

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, character => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
  })[character]);
}

function renderServers(inventory) {
  if (!inventory.length) {
    const hasFilters = searchInput.value.trim() || statusFilter.value;
    rows.innerHTML = hasFilters
      ? '<tr><td class="empty-cell" colspan="6"><div class="empty-title">No matching servers</div><div class="empty-description">Try another search or status filter.</div></td></tr>'
      : '<tr><td class="empty-cell" colspan="6"><div class="empty-title">Your inventory is empty</div><div class="empty-description">Add a server to start organizing your infrastructure.</div></td></tr>';
    return;
  }

  rows.innerHTML = inventory.map(server => {
    const statusClass = `status-${server.status.toLowerCase()}`;
    const initials = server.name.trim().split(/\s+/).slice(0, 2).map(word => word[0]).join("").toUpperCase();
    return `<tr>
      <td><div class="server-cell">
        <span class="server-avatar">${escapeHtml(initials)}</span>
        <div><div class="server-name">${escapeHtml(server.name)}</div><div class="server-host">${escapeHtml(server.hostname)}${server.ipAddress ? ` · ${escapeHtml(server.ipAddress)}` : ""}</div></div>
      </div></td>
      <td><span class="status-badge ${statusClass}">${escapeHtml(server.status)}</span></td>
      <td><span class="environment-tag">${escapeHtml(server.environment)}</span></td>
      <td>${escapeHtml(server.operatingSystem)}</td>
      <td>${escapeHtml(server.location || "—")}</td>
      <td><div class="action-buttons">
        <button class="action-button" type="button" data-edit="${server.id}" aria-label="Edit ${escapeHtml(server.name)}">Edit</button>
        <button class="action-button delete" type="button" data-delete="${server.id}" aria-label="Delete ${escapeHtml(server.name)}">Delete</button>
      </div></td>
    </tr>`;
  }).join("");
}

function openDialog(server = null) {
  editingId = server?.id ?? null;
  form.reset();
  formError.textContent = "";
  document.querySelector("#dialog-title").textContent = editingId ? "Edit server" : "Add a server";
  saveButton.textContent = editingId ? "Save changes" : "Save server";
  if (server) {
    for (const [key, value] of Object.entries(server)) {
      const field = form.elements.namedItem(key);
      if (field && value != null) field.value = value;
    }
  }
  dialog.showModal();
  form.elements.namedItem("name").focus();
}

document.querySelector("#add-server").addEventListener("click", () => openDialog());
document.querySelector("#close-dialog").addEventListener("click", () => dialog.close());
document.querySelector("#cancel-dialog").addEventListener("click", () => dialog.close());
document.querySelector("#logout").addEventListener("click", async () => {
  try {
    await request("/api/auth/logout", { method: "POST" });
    window.location.replace("/login.html");
  } catch (error) {
    tableMessage.textContent = error.message;
  }
});
searchInput.addEventListener("input", () => {
  clearTimeout(searchTimeout);
  searchTimeout = setTimeout(loadInventory, 180);
});
statusFilter.addEventListener("change", loadInventory);
document.addEventListener("keydown", event => {
  if (event.key === "/" && document.activeElement !== searchInput && !dialog.open) {
    event.preventDefault();
    searchInput.focus();
  }
});

rows.addEventListener("click", async event => {
  const editButton = event.target.closest("[data-edit]");
  const deleteButton = event.target.closest("[data-delete]");
  if (editButton) {
    const server = servers.find(item => item.id === Number(editButton.dataset.edit));
    if (server) openDialog(server);
  }
  if (deleteButton) {
    const id = Number(deleteButton.dataset.delete);
    const server = servers.find(item => item.id === id);
    if (!server || !confirm(`Delete "${server.name}" from the inventory?`)) return;
    try {
      await request(`/api/servers/${id}`, { method: "DELETE" });
      await loadInventory();
    } catch (error) {
      tableMessage.textContent = error.message;
    }
  }
});

form.addEventListener("submit", async event => {
  event.preventDefault();
  formError.textContent = "";
  saveButton.disabled = true;
  const body = Object.fromEntries(new FormData(form).entries());
  const url = editingId ? `/api/servers/${editingId}` : "/api/servers";
  try {
    await request(url, {
      method: editingId ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
    dialog.close();
    await loadInventory();
  } catch (error) {
    formError.textContent = error.message;
  } finally {
    saveButton.disabled = false;
  }
});

loadInventory();
