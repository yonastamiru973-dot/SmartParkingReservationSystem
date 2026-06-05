(function () {
    "use strict";

    var minsEl = document.getElementById("additional-minutes");
    var rateEl = document.getElementById("hourly-rate");
    var endEl = document.getElementById("new-end");
    var feeEl = document.getElementById("additional-fee");
    var currentEndInput = document.querySelector('input[name="CurrentEndTime"]');
    if (!minsEl || !rateEl || !currentEndInput) return;

    function update() {
        var rate = parseFloat(rateEl.value) || 0;
        var mins = parseInt(minsEl.value, 10) || 0;
        var currentEnd = new Date(currentEndInput.value);
        if (isNaN(currentEnd.getTime())) return;

        var newEnd = new Date(currentEnd.getTime() + mins * 60000);
        var fee = Math.round(rate * (mins / 60) * 100) / 100;

        if (endEl) {
            endEl.textContent = newEnd.toLocaleString(undefined, {
                month: "short", day: "numeric", hour: "numeric", minute: "2-digit"
            });
        }
        if (feeEl) feeEl.textContent = "$" + fee.toFixed(2);
    }

    minsEl.addEventListener("change", update);
    update();
})();
