/* ============================================================
   DORINGKLOOF MEDICAL CENTRE
   GLOBAL APPLICATION JAVASCRIPT
   ============================================================ */

"use strict";


/* ============================================================
   DOCUMENT READY
   ============================================================ */

document.addEventListener("DOMContentLoaded", function () {

    initialiseNavigationAccessibility();
    initialiseBootstrapValidationEnhancements();
    initialiseAutoDismissAlerts();
    initialiseTableAccessibility();
    initialiseChatAccessibility();

});


/* ============================================================
   NAVIGATION ACCESSIBILITY
   ============================================================ */

function initialiseNavigationAccessibility() {

    const navbar = document.getElementById("mainNavbar");

    if (!navbar) {
        return;
    }

    const toggler = document.querySelector(".navbar-toggler");

    if (!toggler) {
        return;
    }

    navbar.addEventListener("shown.bs.collapse", function () {
        toggler.setAttribute("aria-expanded", "true");
    });

    navbar.addEventListener("hidden.bs.collapse", function () {
        toggler.setAttribute("aria-expanded", "false");
    });

}


/* ============================================================
   FORM VALIDATION ENHANCEMENTS
   ============================================================ */

function initialiseBootstrapValidationEnhancements() {

    const forms = document.querySelectorAll("form");

    forms.forEach(function (form) {

        form.addEventListener("submit", function () {

            /*
             * Give the browser a moment to process the form.
             * If validation fails, focus the first invalid field.
             */
            window.setTimeout(function () {

                const invalidField =
                    form.querySelector(
                        ".input-validation-error, :invalid"
                    );

                if (invalidField) {
                    invalidField.focus();
                }

            }, 50);

        });

    });

}


/* ============================================================
   AUTO-DISMISSIBLE ALERTS
   ============================================================ */

function initialiseAutoDismissAlerts() {

    const alerts = document.querySelectorAll(
        ".alert[data-auto-dismiss='true']"
    );

    alerts.forEach(function (alert) {

        const duration =
            parseInt(
                alert.getAttribute("data-dismiss-duration") || "5000",
                10
            );

        if (Number.isNaN(duration)) {
            return;
        }

        window.setTimeout(function () {

            if (!document.body.contains(alert)) {
                return;
            }

            alert.classList.remove("show");

            window.setTimeout(function () {

                if (alert.parentNode) {
                    alert.remove();
                }

            }, 200);

        }, duration);

    });

}


/* ============================================================
   TABLE ACCESSIBILITY
   ============================================================ */

function initialiseTableAccessibility() {

    const tables = document.querySelectorAll("table");

    tables.forEach(function (table) {

        /*
         * Responsive tables remain accessible to screen readers
         * while avoiding unnecessary duplicate announcements.
         */
        if (!table.getAttribute("role")) {
            table.setAttribute("role", "table");
        }

        const headers = table.querySelectorAll("thead th");

        headers.forEach(function (header) {

            if (!header.getAttribute("scope")) {
                header.setAttribute("scope", "col");
            }

        });

    });

}


/* ============================================================
   CHAT ACCESSIBILITY
   ============================================================ */

function initialiseChatAccessibility() {

    const chatWindow = document.getElementById("chatWindow");
    const closeChat = document.getElementById("closeChat");

    if (!chatWindow) {
        return;
    }

    /*
     * The existing chatbot implementation may control display
     * itself. These attributes improve assistive technology
     * support without changing the chatbot's underlying logic.
     */
    if (!chatWindow.hasAttribute("role")) {
        chatWindow.setAttribute("role", "dialog");
    }

    if (!chatWindow.hasAttribute("aria-label")) {
        chatWindow.setAttribute(
            "aria-label",
            "Doringkloof Medical Centre health assistant"
        );
    }

    if (closeChat) {

        closeChat.addEventListener("click", function () {

            window.setTimeout(function () {

                const launcher =
                    document.querySelector(
                        "#doctorBot, .chatbot-btn"
                    );

                if (launcher) {
                    launcher.focus();
                }

            }, 50);

        });

    }

}


/* ============================================================
   GLOBAL LOADING STATE HELPER
   ============================================================ */

/*
 * These helpers can be used by existing pages when real
 * API/database operations are added.
 *
 * They do NOT create or simulate data.
 */

function setButtonLoading(button, loading, loadingText) {

    if (!button) {
        return;
    }

    if (loading) {

        if (!button.dataset.originalHtml) {
            button.dataset.originalHtml = button.innerHTML;
        }

        button.disabled = true;
        button.setAttribute("aria-busy", "true");

        button.innerHTML =
            '<span class="spinner-border spinner-border-sm me-2" ' +
            'role="status" aria-hidden="true"></span>' +
            (loadingText || "Processing...");

    } else {

        button.disabled = false;
        button.removeAttribute("aria-busy");

        if (button.dataset.originalHtml) {
            button.innerHTML = button.dataset.originalHtml;
        }

    }

}


/* ============================================================
   GLOBAL TOAST HELPER
   ============================================================ */

/*
 * This helper creates visual feedback only when explicitly
 * called by a real application action.
 *
 * It does not create dummy records or fake backend results.
 */

function showSmartToast(message, type) {

    if (!message) {
        return;
    }

    const toastType = type || "info";

    const containerId = "smartToastContainer";

    let container =
        document.getElementById(containerId);

    if (!container) {

        container = document.createElement("div");

        container.id = containerId;

        container.className =
            "toast-container position-fixed top-0 end-0 p-3";

        container.style.zIndex = "2000";

        document.body.appendChild(container);

    }

    const toast = document.createElement("div");

    toast.className =
        "toast align-items-center border-0";

    toast.setAttribute("role", "status");
    toast.setAttribute("aria-live", "polite");
    toast.setAttribute("aria-atomic", "true");

    const typeMap = {
        success: "text-bg-success",
        danger: "text-bg-danger",
        warning: "text-bg-warning",
        info: "text-bg-primary"
    };

    toast.classList.add(
        typeMap[toastType] || typeMap.info
    );

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body"></div>
            <button type="button"
                    class="btn-close btn-close-white me-2 m-auto"
                    data-bs-dismiss="toast"
                    aria-label="Close notification">
            </button>
        </div>
    `;

    toast.querySelector(".toast-body").textContent = message;

    container.appendChild(toast);

    if (window.bootstrap && bootstrap.Toast) {

        const bootstrapToast =
            new bootstrap.Toast(toast, {
                delay: 5000
            });

        toast.addEventListener(
            "hidden.bs.toast",
            function () {
                toast.remove();
            }
        );

        bootstrapToast.show();

    } else {

        toast.classList.add("show");

        window.setTimeout(function () {
            toast.remove();
        }, 5000);

    }

}


/* ============================================================
   CONFIRMATION HELPER
   ============================================================ */

/*
 * Use this for destructive actions such as deleting a record.
 * The function returns true or false so the existing form/action
 * can decide whether to continue.
 */

function confirmSmartAction(message) {

    const confirmationMessage =
        message ||
        "Are you sure you want to continue?";

    return window.confirm(confirmationMessage);

}