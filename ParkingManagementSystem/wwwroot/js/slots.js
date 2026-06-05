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

    var statusColors = {
        Available:   "#10b981",
        Occupied:    "#dc2626",
        Maintenance: "#d97706"
    };

    var mapState = {
        map: null,
        markers: {},
        infoWindow: null
    };

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

    function buildInfoWindowContent(slot) {
        var status = slot.status || "";
        var cls = status.toLowerCase();
        return (
            '<div class="gmap-iw">' +
                '<div class="gmap-iw-number">' + escapeHtml(slot.slotNumber) + '</div>' +
                '<div class="gmap-iw-row">' +
                    '<span class="gmap-iw-status ' + cls + '">' + escapeHtml(status) + '</span>' +
                '</div>' +
                '<div class="gmap-iw-row"><strong>Type:</strong> ' + escapeHtml(slot.slotType) + '</div>' +
                '<div class="gmap-iw-row"><strong>Rate:</strong> ' + formatCurrency(slot.hourlyRate) + ' / hour</div>' +
                (slot.description
                    ? '<div class="gmap-iw-row">' + escapeHtml(slot.description) + '</div>'
                    : '') +
            '</div>'
        );
    }

    function buildMarkerIcon(status) {
        if (!window.google || !google.maps) return undefined;
        return {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 11,
            fillColor: statusColors[status] || "#64748b",
            fillOpacity: 1,
            strokeColor: "#ffffff",
            strokeWeight: 3
        };
    }

    function createMarker(slot) {
        if (!mapState.map || !window.google || slot.latitude == null || slot.longitude == null) return null;
        var marker = new google.maps.Marker({
            position: { lat: Number(slot.latitude), lng: Number(slot.longitude) },
            map: mapState.map,
            title: slot.slotNumber + " — " + slot.status,
            icon: buildMarkerIcon(slot.status)
        });
        marker.addListener("click", function () {
            if (!mapState.infoWindow) mapState.infoWindow = new google.maps.InfoWindow();
            mapState.infoWindow.setContent(buildInfoWindowContent(slot));
            mapState.infoWindow.open(mapState.map, marker);
        });
        return marker;
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
            // re-trigger the pulse animation
            void card.offsetWidth;
            card.classList.add("slot-changed");
            setTimeout(function () { card.classList.remove("slot-changed"); }, 900);
        }
    }

    function updateMarkerStatus(slot) {
        var marker = mapState.markers[slot.id];
        if (!marker) return;
        marker.setIcon(buildMarkerIcon(slot.status));
        marker.setTitle(slot.slotNumber + " — " + slot.status);
        // Refresh open info window content if it belongs to this marker
        if (mapState.infoWindow && mapState.infoWindow.getMap()) {
            mapState.infoWindow.setContent(buildInfoWindowContent(slot));
        }
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
                    updateMarkerStatus(s);
                });
                updateCounts(data.slots);
            })
            .catch(function () { /* network errors silently ignored */ });
    }

    function initMap() {
        var mapEl = document.getElementById("slot-map");
        if (!mapEl || !window.google || !google.maps) return;

        var centerLat = parseFloat(mapEl.getAttribute("data-center-lat")) || 0;
        var centerLng = parseFloat(mapEl.getAttribute("data-center-lng")) || 0;
        var zoom = parseInt(mapEl.getAttribute("data-zoom"), 10) || 17;

        mapState.map = new google.maps.Map(mapEl, {
            center: { lat: centerLat, lng: centerLng },
            zoom: zoom,
            mapTypeControl: false,
            streetViewControl: false,
            fullscreenControl: true,
            clickableIcons: false
        });

        var bounds = new google.maps.LatLngBounds();
        slots.forEach(function (slot) {
            var marker = createMarker(slot);
            if (marker) {
                mapState.markers[slot.id] = marker;
                bounds.extend(marker.getPosition());
            }
        });

        if (slots.length > 0 && !bounds.isEmpty()) {
            mapState.map.fitBounds(bounds, 60);
            // Don't zoom in too far on a single point
            var listener = google.maps.event.addListenerOnce(mapState.map, "idle", function () {
                if (mapState.map.getZoom() > zoom + 1) mapState.map.setZoom(zoom);
            });
        }
    }

    window.initSlotsMap = initMap;

    // Poll for live availability updates every 5 seconds.
    setInterval(pollStatus, 5000);
})();
