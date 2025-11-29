"use strict";

// -------------------------------------------------
// Notification Center (dropdown + toasts)
// -------------------------------------------------
const NotificationCenter = (() => {
    const STORAGE_KEY = "propertyInventory_notifications";
    const maxItems = 50; // Increased to store more notifications
    const elements = {
        list: null,
        badge: null,
        empty: null,
        clearButton: null
    };

    // Load notifications from localStorage
    function loadNotifications() {
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            if (stored) {
                const parsed = JSON.parse(stored);
                return parsed.map(n => ({
                    ...n,
                    timestamp: new Date(n.timestamp)
                }));
            }
        } catch (e) {
            console.warn("Failed to load notifications from localStorage:", e);
        }
        return [];
    }

    // Save notifications to localStorage
    function saveNotifications(notifications) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(notifications));
        } catch (e) {
            console.warn("Failed to save notifications to localStorage:", e);
        }
    }

    let notifications = loadNotifications();

    function init() {
        elements.list = document.getElementById("notificationList");
        elements.badge = document.getElementById("notificationBadge");
        elements.empty = document.getElementById("notificationEmpty");
        elements.clearButton = document.getElementById("clearNotifications");

        if (elements.clearButton) {
            elements.clearButton.addEventListener("click", function (event) {
                event.preventDefault();
                clearAll();
            });
        }

        render();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    }
    else {
        init();
    }

    function render() {
        if (!elements.list) {
            return;
        }

        elements.list.innerHTML = "";

        if (!notifications.length) {
            if (elements.empty) {
                elements.empty.classList.remove("d-none");
            }
        } else {
            if (elements.empty) {
                elements.empty.classList.add("d-none");
            }

            notifications.forEach(notification => {
                const item = document.createElement("div");
                item.className = "notification-item p-3 border-bottom";

                const header = document.createElement("div");
                header.className = "d-flex justify-content-between align-items-start gap-2";

                const titleWrapper = document.createElement("div");
                const title = document.createElement("div");
                title.className = "fw-semibold";
                title.textContent = notification.title;
                const message = document.createElement("div");
                message.className = "text-muted small";
                message.textContent = notification.message;
                titleWrapper.appendChild(title);
                titleWrapper.appendChild(message);

                const time = document.createElement("small");
                time.className = "text-muted text-nowrap";
                time.textContent = formatTimestamp(notification.timestamp);

                header.appendChild(titleWrapper);
                header.appendChild(time);

                const badge = document.createElement("span");
                badge.className = `badge bg-${resolveColor(notification.type)} me-2 notification-label`;
                badge.textContent = labelFor(notification.type);

                // Make notification item clickable if it has a link
                if (notification.link) {
                    item.style.cursor = "pointer";
                    item.addEventListener("click", function() {
                        window.location.href = notification.link;
                    });
                }

                item.appendChild(badge);
                item.appendChild(header);

                elements.list.appendChild(item);
            });
        }

        updateBadge();
    }

    function updateBadge() {
        if (!elements.badge) {
            return;
        }

        if (!notifications.length) {
            elements.badge.classList.add("d-none");
        } else {
            elements.badge.classList.remove("d-none");
            elements.badge.textContent = notifications.length;
        }
    }

    function clearAll() {
        notifications = [];
        saveNotifications(notifications);
        render();
    }

    function labelFor(type) {
        switch ((type || "").toLowerCase()) {
            case "success":
                return "Success";
            case "warning":
                return "Alert";
            case "danger":
                return "Error";
            case "info":
                return "Info";
            case "primary":
                return "Update";
            default:
                return "Info";
        }
    }

    function resolveColor(type) {
        switch ((type || "").toLowerCase()) {
            case "success":
                return "success";
            case "warning":
                return "warning";
            case "danger":
                return "danger";
            case "primary":
                return "primary";
            default:
                return "info";
        }
    }

    function formatTimestamp(timestamp) {
        if (!timestamp) {
            return "";
        }
        const date = timestamp instanceof Date ? timestamp : new Date(timestamp);
        return date.toLocaleString();
    }

    function add(notification, options = {}) {
        const entry = {
            title: notification.title || "Notification",
            message: notification.message || "",
            type: notification.type || "info",
            timestamp: notification.timestamp ? new Date(notification.timestamp) : new Date(),
            link: notification.link || null
        };

        notifications.unshift(entry);
        if (notifications.length > maxItems) {
            notifications = notifications.slice(0, maxItems);
        }

        // Save to localStorage
        saveNotifications(notifications);

        if (options.toast !== false) {
            showToast(entry.title, entry.message, entry.type, entry.link);
        }

        render();
    }

    function bootstrap(initialNotifications) {
        // Don't bootstrap if we already have notifications from localStorage
        if (notifications.length > 0) {
            return;
        }

        if (!Array.isArray(initialNotifications) || !initialNotifications.length) {
            return;
        }

        initialNotifications.slice(0, maxItems).reverse().forEach(item => {
            add({
                title: item.title,
                message: item.message,
                type: item.type,
                timestamp: item.timestamp
            }, { toast: false });
        });
    }

    return {
        add,
        clear: clearAll,
        bootstrap
    };
})();

window.NotificationCenter = NotificationCenter;

function showToast(title, message, type, link = null) {
    let toastContainer = document.querySelector(".toast-container");
    if (!toastContainer) {
        toastContainer = document.createElement("div");
        toastContainer.className = "toast-container";
        document.body.appendChild(toastContainer);
    }

    const toastId = `toast-${Date.now()}`;
    const clickableClass = link ? "cursor-pointer" : "";
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white bg-${type === "success" ? "success" : type === "warning" ? "warning" : type === "danger" ? "danger" : "info"} border-0 ${clickableClass}" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body ${link ? "cursor-pointer" : ""}" ${link ? `onclick="window.location.href='${link}'"` : ""}>
                    <strong>${title}</strong><br />
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close" onclick="event.stopPropagation();"></button>
            </div>
        </div>`;

    toastContainer.insertAdjacentHTML("beforeend", toastHtml);

    const toastElement = document.getElementById(toastId);
    
    // Make entire toast clickable if it has a link
    if (link) {
        toastElement.style.cursor = "pointer";
        toastElement.addEventListener("click", function(e) {
            // Don't redirect if clicking the close button
            if (e.target.classList.contains("btn-close") || e.target.closest(".btn-close")) {
                return;
            }
            window.location.href = link;
        });
    }
    
    const toast = new bootstrap.Toast(toastElement);
    toast.show();

    toastElement.addEventListener("hidden.bs.toast", function () {
        toastElement.remove();
    });
}

// -------------------------------------------------
// SignalR connection for live updates
// -------------------------------------------------
const connection = window.signalR
    ? new signalR.HubConnectionBuilder()
        .withUrl("/propertyHub", {
            skipNegotiation: false,
            transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect()
        .build()
    : null;

if (connection) {
    connection.start().then(function () {
        console.log("SignalR connected");
    }).catch(function (err) {
        console.warn("SignalR connection error:", err.toString());
    });

    const shouldRefreshIndex = () => {
        const path = (window.location.pathname || "").toLowerCase();
        return path === "/" || path === "/index";
    };

    const refreshIndex = () => {
        setTimeout(function () {
            if (shouldRefreshIndex()) {
                window.location.reload();
            }
        }, 2000);
    };

    connection.on("PropertyCreated", function (propertyCode) {
        NotificationCenter.add({
            title: "New Property Added",
            message: `Property ${propertyCode} has been added.`,
            type: "success",
            link: "/"
        });
        refreshIndex();
    });

    connection.on("PropertyUpdated", function (propertyCode, action, borrowerName) {
        let title = "Property Updated";
        let message = `Property ${propertyCode} has been ${action}.`;
        let type = "info";
        let showToast = true; // Default to showing toast
        
        // Handle different action types with specific messages
        if (action === "borrowed") {
            title = "Property Borrowed";
            message = borrowerName 
                ? `Property ${propertyCode} has been borrowed by ${borrowerName}.`
                : `Property ${propertyCode} has been borrowed.`;
            type = "info";
            showToast = true; // Show toast for borrowed
        } else if (action === "returned") {
            title = "Property Returned";
            message = `Property ${propertyCode} has been returned and is now available.`;
            type = "success";
        } else if (action === "updated") {
            title = "Property Updated";
            message = `Property ${propertyCode} has been updated.`;
            type = "primary";
        }
        
        NotificationCenter.add({
            title: title,
            message: message,
            type: type,
            link: "/"
        }, { toast: showToast }); // Only show toast if not borrowed
        refreshIndex();
    });

    connection.on("PropertyDeleted", function (propertyCode) {
        NotificationCenter.add({
            title: "Property Deleted",
            message: `Property ${propertyCode} has been deleted.`,
            type: "danger",
            link: "/"
        }, { toast: true }); // Show alert/toast for deleted items
        refreshIndex();
    });

    connection.on("AccountRequestCreated", function (payload) {
        if (!payload) {
            return;
        }
        NotificationCenter.add({
            title: "New Account Request",
            message: `${payload.fullName} (${payload.email}) requested access.`,
            type: "warning",
            timestamp: payload.requestedAt ? new Date(payload.requestedAt) : new Date(),
            link: "/Admin/AccountRequests"
        }, { toast: true }); // Show toast notification for new account requests
        refreshPendingCount();
    });
    
    // Function to refresh pending count badge
    function refreshPendingCount() {
        // Reload the page to update the ViewComponent, or use AJAX to fetch count
        // For now, we'll trigger a page reload for the navbar badge
        if (window.location.pathname.includes("/Admin/AccountRequests")) {
            // If on Account Requests page, just refresh that section
            return;
        }
        // The ViewComponent will automatically update on next page load
    }

    connection.on("OverduePropertyNotification", function (payload) {
        if (!payload) {
            return;
        }
        const daysOverdue = payload.daysOverdue || 0;
        const message = payload.message || `Property ${payload.propertyCode} (Tag: ${payload.tagNumber}) borrowed by ${payload.borrowerName} is overdue.`;
        const link = payload.propertyId ? `/Details/${payload.propertyId}` : "/";
        
        NotificationCenter.add({
            title: "⚠️ Overdue Property Alert",
            message: message,
            type: "warning",
            timestamp: new Date(),
            link: link
        }, { toast: true });
        refreshIndex();
    });

    connection.on("AccountRequestStatusChanged", function (payload) {
        if (!payload) {
            return;
        }
        const status = (payload.status || "").toLowerCase();
        const type = status === "approved" ? "success" : "info"; // Changed rejected from "danger" to "info"
        const action = status === "approved" ? "approved" : status === "rejected" ? "rejected" : "updated";
        NotificationCenter.add({
            title: `Account Request ${action.charAt(0).toUpperCase() + action.slice(1)}`,
            message: `${payload.fullName} (${payload.email}) - ${payload.status}`,
            type: type,
            timestamp: payload.reviewedAt ? new Date(payload.reviewedAt) : new Date(),
            link: "/Admin/AccountRequests"
        }, { toast: true }); // Show toast notification for status changes (approved/rejected)
    });

    connection.on("UserDeleted", function (payload) {
        if (!payload) {
            return;
        }
        NotificationCenter.add({
            title: "User Removed",
            message: `User ${payload.userName || payload.userEmail} has been removed from the system.`,
            type: "info", // Changed from "danger" to "info"
            timestamp: payload.deletedAt ? new Date(payload.deletedAt) : new Date(),
            link: "/Admin/AccountRequests"
        }, { toast: true }); // Show toast notification for user deletion
        refreshIndex();
    });
}
else {
    console.warn("SignalR library not found. Live updates are disabled.");
}

