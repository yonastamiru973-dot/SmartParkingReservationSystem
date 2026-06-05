(function () {
    "use strict";

    var dataEl = document.getElementById("slot-data");
    if (!dataEl) return;

    var statusUrl = dataEl.getAttribute("data-status-url");
    var slots = [];
    try {
        slots = JSON.parse(dataEl.getAttribute("data-slots-json") || "[]");
    } catch (e) {
        slots = [];
    }

    var mapEl = document.getElementById("lot-map");
    var popoverEl = document.getElementById("lot-popover");
    var tileById = {};
    var openSlotId = null;

    function escapeHtml(s) {
        if (s == null) return "";
        return String(s)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function formatCurrency(value) {
        var n = Number(value);
        if (isNaN(n)) return "";
        return "$" + n.toFixed(2);
    }

    function computeBounds(items) {
        if (!items.length) return null;
        var minLat = Infinity, maxLat = -Infinity, minLng = Infinity, maxLng = -Infinity;
        items.forEach(function (s) {
            if (s.latitude < minLat) minLat = s.latitude;
            if (s.latitude > maxLat) maxLat = s.latitude;
            if (s.longitude < minLng) minLng = s.longitude;
            if (s.longitude > maxLng) maxLng = s.longitude;
        });
        return {
            minLat: minLat, maxLat: maxLat,
            minLng: minLng, maxLng: maxLng,
            latRange: (maxLat - minLat),
            lngRange: (maxLng - minLng)
        };
    }

    // Converts a slot's lat/lng to an (x%, y%) position inside the lot view.
    // Higher latitude renders nearer the top (north-up).
    function positionFor(slot, bounds) {
        var pad = 9;
        var span = 100 - 2 * pad;
        var x = bounds.lngRange < 1e-9
            ? 50
            : pad + ((slot.longitude - bounds.minLng) / bounds.lngRange) * span;
        var y = bounds.latRange < 1e-9
            ? 50
            : pad + (1 - (slot.latitude - bounds.minLat) / bounds.latRange) * span;
        return { x: x, y: y };
    }

    function buildPopoverContent(slot) {
        var status = slot.status || "";
        var cls = status.toLowerCase();
        var lat = Number(slot.latitude).toFixed(5);
        var lng = Number(slot.longitude).toFixed(5);
        return (
            '<button type="button" class="lot-popover-close" aria-label="Close">&times;</button>' +
            '<div class="lot-popover-number">' + escapeHtml(slot.slotNumber) + '</div>' +
            '<span class="lot-popover-status status-' + cls + '">' + escapeHtml(status) + '</span>' +
            '<dl class="lot-popover-meta">' +
                '<dt>Type</dt><dd>' + escapeHtml(slot.slotType) + '</dd>' +
                '<dt>Rate</dt><dd>' + formatCurrency(slot.hourlyRate) + ' / hour</dd>' +
                (slot.description
                    ? '<dt>Notes</dt><dd>' + escapeHtml(slot.description) + '</dd>'
                    : '') +
                '<dt>Coordinates</dt><dd class="muted">' + lat + ', ' + lng + '</dd>' +
            '</dl>'
        );
    }

    function openPopover(slot, tile) {
        if (!popoverEl || !mapEl) return;
        openSlotId = slot.id;
        popoverEl.innerHTML = buildPopoverContent(slot);
        popoverEl.classList.add("open");

        // Position popover below the tile, clamped to the map's horizontal bounds.
        var mapRect = mapEl.getBoundingClientRect();
        var tileRect = tile.getBoundingClientRect();
        var leftPct = ((tileRect.left + tileRect.width / 2) - mapRect.left) / mapRect.width * 100;
        var topPct = ((tileRect.bottom - mapRect.top) / mapRect.height) * 100;

        // Clamp horizontally so the popover stays inside the map
        leftPct = Math.max(18, Math.min(82, leftPct));
        // If the tile is near the bottom, render popover above it instead
        var openAbove = topPct > 70;
        popoverEl.classList.toggle("above", openAbove);
        popoverEl.style.left = leftPct + "%";
        popoverEl.style.top = openAbove
            ? ((tileRect.top - mapRect.top) / mapRect.height * 100) + "%"
            : topPct + "%";

        var closeBtn = popoverEl.querySelector(".lot-popover-close");
        if (closeBtn) closeBtn.addEventListener("click", function (e) {
            e.stopPropagation();
            closePopover();
        });
    }

    function closePopover() {
        if (popoverEl) popoverEl.classList.remove("open");
        openSlotId = null;
    }

    function renderMap() {
        if (!mapEl) return;

        // Remove old tiles but keep the compass + popover
        Array.prototype.slice.call(mapEl.querySelectorAll(".lot-tile, .lot-empty")).forEach(function (n) {
            n.parentNode.removeChild(n);
        });
        tileById = {};

        if (!slots.length) {
            var empty = document.createElement("div");
            empty.className = "lot-empty";
            empty.textContent = "No slots to display.";
            mapEl.appendChild(empty);
            return;
        }

        var bounds = computeBounds(slots);
        slots.forEach(function (slot) {
            var pos = positionFor(slot, bounds);
            var tile = document.createElement("button");
            tile.type = "button";
            tile.className = "lot-tile status-" + (slot.status || "").toLowerCase();
            tile.style.left = pos.x + "%";
            tile.style.top = pos.y + "%";
            tile.setAttribute("data-slot-id", slot.id);
            tile.setAttribute("title", slot.slotNumber + " — " + slot.status);
            tile.setAttribute("aria-label", slot.slotNumber + ", " + slot.status);
            tile.innerHTML = '<span class="lot-tile-number">' + escapeHtml(slot.slotNumber) + '</span>';

            tile.addEventListener("click", function (e) {
                e.stopPropagation();
                openPopover(slot, tile);
            });

            mapEl.appendChild(tile);
            tileById[slot.id] = tile;
        });
    }

    function refreshCardStatus(slot) {
        var card = document.querySelector('.slot-card[data-slot-id="' + slot.id + '"]');
        if (!card) return;

        var prev = card.className.match(/status-(available|occupied|maintenance)/i);
        var prevStatus = prev ? prev[1].toLowerCase() : null;
        var nextStatus = (slot.status || "").toLowerCase();

        if (prevStatus !== nextStatus) {
            card.classList.remove("status-available", "status-occupied", "status-maintenance");
            card.classList.add("status-" + nextStatus);

            var label = card.querySelector(".slot-status-label");
            if (label) label.textContent = slot.status;

            card.classList.remove("slot-changed");
            void card.offsetWidth;
            card.classList.add("slot-changed");
            setTimeout(function () { card.classList.remove("slot-changed"); }, 900);
        }
    }

    function refreshTileStatus(slot) {
        var tile = tileById[slot.id];
        if (!tile) return;
        tile.className = "lot-tile status-" + (slot.status || "").toLowerCase();
        tile.setAttribute("title", slot.slotNumber + " — " + slot.status);
        tile.setAttribute("aria-label", slot.slotNumber + ", " + slot.status);
    }

    function updateCounts(allSlots) {
        var counts = { Available: 0, Occupied: 0, Maintenance: 0 };
        allSlots.forEach(function (s) {
            if (counts[s.status] != null) counts[s.status]++;
        });
        var pillA = document.querySelector(".count-available");
        var pillO = document.querySelector(".count-occupied");
        var pillM = document.querySelector(".count-maintenance");
        if (pillA) pillA.textContent = counts.Available + " available";
        if (pillO) pillO.textContent = counts.Occupied + " occupied";
        if (pillM) pillM.textContent = counts.Maintenance + " maintenance";
    }

    function pollStatus() {
        if (!statusUrl) return;
        fetch(statusUrl, { credentials: "same-origin", headers: { "Accept": "application/json" } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (data) {
                if (!data || !data.slots) return;
                slots = data.slots;
                data.slots.forEach(function (s) {
                    refreshCardStatus(s);
                    refreshTileStatus(s);
                    if (openSlotId === s.id) {
                        popoverEl.innerHTML = buildPopoverContent(s);
                        var closeBtn = popoverEl.querySelector(".lot-popover-close");
                        if (closeBtn) closeBtn.addEventListener("click", function (e) {
                            e.stopPropagation();
                            closePopover();
                        });
                    }
                });
                updateCounts(data.slots);
            })
            .catch(function () { /* silently ignore transient network errors */ });
    }

    // Clicking outside the popover (but still on the map background) closes it.
    document.addEventListener("click", function (e) {
        if (!popoverEl || !popoverEl.classList.contains("open")) return;
        if (popoverEl.contains(e.target)) return;
        if (e.target.classList && e.target.classList.contains("lot-tile")) return;
        if (e.target.closest && e.target.closest(".lot-tile")) return;
        closePopover();
    });

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") closePopover();
    });

    renderMap();
    setInterval(pollStatus, 5000);
})();
