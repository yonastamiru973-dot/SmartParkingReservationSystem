(function () {
    "use strict";

    function applyStatusClass(select, status) {
        select.classList.remove("status-available", "status-occupied", "status-maintenance");
        select.classList.add("status-" + status.toLowerCase());
    }

    function flashFeedback(span, ok, message) {
        span.classList.remove("ok", "err");
        span.classList.add(ok ? "ok" : "err");
        span.textContent = message;
        setTimeout(function () {
            span.classList.remove("ok", "err");
            span.textContent = "";
        }, 2500);
    }

    document.querySelectorAll(".status-form").forEach(function (form) {
        var slotId = form.getAttribute("data-slot-id");
        var select = form.querySelector(".status-select");
        var feedback = form.querySelector(".status-feedback");
        var tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');

        if (!select) return;
        var lastValue = select.value;

        select.addEventListener("change", function () {
            var newValue = select.value;
            var body = new FormData();
            body.append("status", newValue);
            if (tokenInput) body.append("__RequestVerificationToken", tokenInput.value);

            select.disabled = true;
            feedback.textContent = "Saving…";

            fetch("/admin/slots/set-status/" + encodeURIComponent(slotId), {
                method: "POST",
                credentials: "same-origin",
                body: body
            })
            .then(function (r) {
                return r.json().catch(function () { return { ok: false, error: "Network error" }; })
                    .then(function (data) { return { ok: r.ok && data.ok, error: data.error }; });
            })
            .then(function (result) {
                select.disabled = false;
                if (result.ok) {
                    applyStatusClass(select, newValue);
                    lastValue = newValue;
                    flashFeedback(feedback, true, "Saved");
                } else {
                    select.value = lastValue;
                    flashFeedback(feedback, false, result.error || "Failed");
                }
            })
            .catch(function () {
                select.disabled = false;
                select.value = lastValue;
                flashFeedback(feedback, false, "Network error");
            });
        });
    });
})();
