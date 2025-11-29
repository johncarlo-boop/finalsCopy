// Firebase web SDK loaded via ES modules because the project does not use a JS bundler.
// This module initializes Firebase so other scripts can reuse the configured app instance.
import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.2/firebase-app.js";
import {
    getAnalytics,
    isSupported as isAnalyticsSupported
} from "https://www.gstatic.com/firebasejs/10.12.2/firebase-analytics.js";

const firebaseConfig = {
    apiKey: "AIzaSyC02EcGbxUYSb5ndit3J-y8Ahtcb53UJE8",
    authDomain: "propertyinventory-d6e4c.firebaseapp.com",
    projectId: "propertyinventory-d6e4c",
    storageBucket: "propertyinventory-d6e4c.firebasestorage.app",
    messagingSenderId: "357299031724",
    appId: "1:357299031724:web:b5e7d4da4d9409526e9909",
    measurementId: "G-P7W3G313QB"
};

const firebaseApp = initializeApp(firebaseConfig);

// Analytics is optional; guard it so unsupported browsers do not throw.
const analyticsPromise = isAnalyticsSupported()
    .then(supported => (supported ? getAnalytics(firebaseApp) : null))
    .catch(err => {
        console.warn("Firebase analytics setup skipped:", err);
        return null;
    });

// Expose handles so legacy scripts can access initialized Firebase services if needed.
window.firebaseApp = firebaseApp;
window.firebaseAnalytics = analyticsPromise;



