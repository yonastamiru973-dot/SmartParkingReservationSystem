(function () {
    "use strict";

    var form = document.getElementById("payment-form");
    var payBtn = document.getElementById("pay-btn");
    var processing = document.getElementById("payment-processing");
    var cardNumber = document.getElementById("card-number");
    var cardHolder = document.getElementById("card-holder");
    var cardExpiry = document.getElementById("card-expiry");
    var cardCvv = document.getElementById("card-cvv");
    var previewNumber = document.getElementById("preview-card-number");
    var previewHolder = document.getElementById("preview-card-holder");
    var previewExpiry = document.getElementById("preview-card-expiry");

    function digitsOnly(val, maxLen) {
        return (val || "").replace(/\D/g, "").slice(0, maxLen);
    }

    function formatCardDisplay(digits) {
        var parts = [];
        for (var i = 0; i < 16; i += 4) {
            var chunk = digits.slice(i, i + 4);
            parts.push(chunk.length ? chunk : "••••");
        }
        return parts.join(" ");
    }

    function setInputValue(input, value) {
        if (!input || input.value === value) return;
        input.value = value;
    }

    function updateCardPreview() {
        if (!cardNumber || !previewNumber) return;
        var digits = digitsOnly(cardNumber.value, 16);
        previewNumber.textContent = formatCardDisplay(digits);
    }

    if (cardNumber) {
        cardNumber.addEventListener("input", updateCardPreview);
        cardNumber.addEventListener("blur", function () {
            setInputValue(cardNumber, digitsOnly(cardNumber.value, 16));
            updateCardPreview();
        });
        updateCardPreview();
    }

    if (cardHolder && previewHolder) {
        cardHolder.addEventListener("input", function () {
            previewHolder.textContent = (cardHolder.value || "CARDHOLDER NAME").toUpperCase();
        });
    }

    if (cardExpiry && previewExpiry) {
        cardExpiry.addEventListener("input", function () {
            previewExpiry.textContent = cardExpiry.value || "MM/YY";
        });
        cardExpiry.addEventListener("blur", function () {
            var v = digitsOnly(cardExpiry.value, 4);
            if (v.length >= 3) v = v.slice(0, 2) + "/" + v.slice(2);
            setInputValue(cardExpiry, v);
            previewExpiry.textContent = v || "MM/YY";
        });
    }

    if (cardCvv) {
        cardCvv.addEventListener("blur", function () {
            setInputValue(cardCvv, digitsOnly(cardCvv.value, 3));
        });
    }

    if (form && payBtn) {
        form.addEventListener("submit", function (e) {
            if (form.dataset.processing === "1") {
                e.preventDefault();
                return;
            }

            // Sanitize fields before validation/submit.
            if (cardNumber) setInputValue(cardNumber, digitsOnly(cardNumber.value, 16));
            if (cardCvv) setInputValue(cardCvv, digitsOnly(cardCvv.value, 3));
            if (cardExpiry) {
                var exp = digitsOnly(cardExpiry.value, 4);
                if (exp.length >= 3) exp = exp.slice(0, 2) + "/" + exp.slice(2);
                setInputValue(cardExpiry, exp);
            }

            // Honour jQuery unobtrusive validation when present.
            if (window.jQuery) {
                var $form = window.jQuery(form);
                var validator = $form.data("validator");
                if (validator && !$form.valid()) {
                    e.preventDefault();
                    return;
                }
            } else if (!form.checkValidity()) {
                e.preventDefault();
                return;
            }

            e.preventDefault();
            form.dataset.processing = "1";
            payBtn.disabled = true;
            if (processing) processing.hidden = false;

            setTimeout(function () {
                // Native submit — does not re-fire this listener.
                HTMLFormElement.prototype.submit.call(form);
            }, 1500);
        });
    }
})();
