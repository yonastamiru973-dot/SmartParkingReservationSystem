(function () {
    "use strict";

    var startEl = document.getElementById("start-time");
    var durationEl = document.getElementById("duration");
    var rateEl = document.getElementById("hourly-rate");
    var endEl = document.getElementById("preview-end");
    var feeEl = document.getElementById("preview-fee");
    if (!startEl || !durationEl || !rateEl) return;

    function update() {
        var rate = parseFloat(rateEl.value) || 0;
        var mins = parseInt(durationEl.value, 10) || 0;
        var startVal = startEl.value;
        if (!startVal) return;

        var start = new Date(startVal);
        var end = new Date(start.getTime() + mins * 60000);
        var fee = Math.round(rate * (mins / 60) * 100) / 100;

        if (endEl) {
            endEl.textContent = end.toLocaleString(undefined, {
                month: "short", day: "numeric", hour: "numeric", minute: "2-digit"
            });
        }
        if (feeEl) {
            feeEl.textContent = "$" + fee.toFixed(2);
        }
    }

    startEl.addEventListener("change", update);
    durationEl.addEventListener("change", update);
    update();
})();
