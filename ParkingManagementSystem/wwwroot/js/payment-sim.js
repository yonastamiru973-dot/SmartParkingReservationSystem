(function () {
    "use strict";

    var form = document.getElementById("payment-form");
    var payBtn = document.getElementById("pay-btn");
    var processing = document.getElementById("payment-processing");
    var cardNumber = document.getElementById("card-number");
    var cardHolder = document.getElementById("card-holder");
    var cardExpiry = document.getElementById("card-expiry");
    var previewNumber = document.getElementById("preview-card-number");
    var previewHolder = document.getElementById("preview-card-holder");
    var previewExpiry = document.getElementById("preview-card-expiry");

    function formatCardDisplay(val) {
        var digits = (val || "").replace(/\D/g, "").slice(0, 16);
        var parts = digits.match(/.{1,4}/g) || [];
        var display = parts.join(" ");
        while (parts.length < 4) display += (display ? " " : "") + "••••";
        return display.trim();
    }

    if (cardNumber && previewNumber) {
        cardNumber.addEventListener("input", function () {
            cardNumber.value = cardNumber.value.replace(/\D/g, "").slice(0, 16);
            previewNumber.textContent = formatCardDisplay(cardNumber.value);
        });
    }
    if (cardHolder && previewHolder) {
        cardHolder.addEventListener("input", function () {
            previewHolder.textContent = cardHolder.value.toUpperCase() || "CARDHOLDER NAME";
        });
    }
    if (cardExpiry && previewExpiry) {
        cardExpiry.addEventListener("input", function () {
            var v = cardExpiry.value.replace(/\D/g, "").slice(0, 4);
            if (v.length >= 3) v = v.slice(0, 2) + "/" + v.slice(2);
            cardExpiry.value = v;
            previewExpiry.textContent = v || "MM/YY";
        });
    }

    if (form && payBtn) {
        form.addEventListener("submit", function (e) {
            if (form.dataset.submitting === "1") return;
            if (!form.checkValidity()) return;

            e.preventDefault();
            form.dataset.submitting = "1";
            payBtn.disabled = true;
            if (processing) processing.hidden = false;

            setTimeout(function () {
                form.dataset.submitting = "";
                form.submit();
            }, 1800);
        });
    }
})();
