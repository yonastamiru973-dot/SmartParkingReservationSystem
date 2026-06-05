(function () {
    "use strict";

    var readerEl = document.getElementById("qr-reader");
    var tokenInput = document.getElementById("token-input");
    var statusEl = document.getElementById("camera-status");
    var form = document.getElementById("scanner-form");
    if (!readerEl) return;

    var lastScanned = "";
    var scanCooldown = false;

    function onScanSuccess(decodedText) {
        if (scanCooldown || decodedText === lastScanned) return;
        lastScanned = decodedText;
        scanCooldown = true;

        if (tokenInput) tokenInput.value = decodedText;
        if (statusEl) statusEl.textContent = "QR detected — validating…";

        setTimeout(function () {
            scanCooldown = false;
            if (form) form.submit();
        }, 400);
    }

    function onScanError() { /* ignore per-frame misses */ }

    if (typeof Html5Qrcode === "undefined") {
        if (statusEl) statusEl.textContent = "Camera library unavailable. Use manual token entry.";
        return;
    }

    var scanner = new Html5Qrcode("qr-reader");
    var config = { fps: 10, qrbox: { width: 250, height: 250 } };

    Html5Qrcode.getCameras().then(function (cameras) {
        if (!cameras || cameras.length === 0) {
            if (statusEl) statusEl.textContent = "No camera found. Paste the token manually below.";
            return;
        }
        // Prefer the rear camera on mobile when available.
        var cameraId = cameras.length > 1 ? cameras[cameras.length - 1].id : cameras[0].id;
        return scanner.start(cameraId, config, onScanSuccess, onScanError);
    }).then(function () {
        if (statusEl) statusEl.textContent = "Camera active — point at a QR code.";
    }).catch(function (err) {
        if (statusEl) statusEl.textContent = "Camera unavailable: " + (err.message || err) + ". Use manual entry.";
    });
})();
