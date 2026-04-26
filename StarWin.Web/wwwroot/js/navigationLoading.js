(function () {
    const navigationOverlayId = "explorer-navigation-loading";
    const sectionOverlayId = "explorer-section-loading";
    const sectionLoadingStartedAtKey = "starforgedAtlas.sectionLoadingStartedAtUnixMs";

    function getOverlay(overlayId) {
        return document.getElementById(overlayId);
    }

    function updateBodyLoadingState() {
        const hasVisibleOverlay = [navigationOverlayId, sectionOverlayId]
            .map(getOverlay)
            .some(overlay => overlay && !overlay.hidden);

        document.body.classList.toggle("navigation-loading-active", hasVisibleOverlay);
    }

    function resetOverlayTimer(overlay, startedAtUnixMs) {
        if (!overlay) {
            return;
        }

        const timer = overlay.querySelector("[data-loading-timer='true']");
        if (!timer) {
            return;
        }

        timer.dataset.startedAtUnixMs = String(startedAtUnixMs ?? Date.now());
    }

    function getSessionStorage() {
        try {
            return window.sessionStorage;
        }
        catch {
            return null;
        }
    }

    function setStoredSectionLoadingStartedAt(startedAtUnixMs) {
        const sessionStorage = getSessionStorage();
        if (!sessionStorage) {
            return;
        }

        sessionStorage.setItem(sectionLoadingStartedAtKey, String(startedAtUnixMs));
    }

    function getStoredSectionLoadingStartedAt() {
        const sessionStorage = getSessionStorage();
        if (!sessionStorage) {
            return null;
        }

        const value = Number(sessionStorage.getItem(sectionLoadingStartedAtKey));
        return Number.isFinite(value) && value > 0 ? value : null;
    }

    function clearStoredSectionLoadingStartedAt() {
        const sessionStorage = getSessionStorage();
        if (!sessionStorage) {
            return;
        }

        sessionStorage.removeItem(sectionLoadingStartedAtKey);
    }

    function showOverlay(overlayId, startedAtUnixMs) {
        const overlay = getOverlay(overlayId);
        if (!overlay) {
            return;
        }

        resetOverlayTimer(overlay, startedAtUnixMs);
        overlay.hidden = false;
        updateBodyLoadingState();
    }

    function hideOverlay(overlayId) {
        const overlay = getOverlay(overlayId);
        if (!overlay) {
            return;
        }

        overlay.hidden = true;
        updateBodyLoadingState();
    }

    window.starforgedAtlasNavigation = {
        showExplorerLoading(event, url) {
            if (event) {
                event.preventDefault();
            }

            showOverlay(navigationOverlayId);
            window.setTimeout(function () {
                window.location.assign(url);
            }, 60);
            return false;
        },
        showSectionLoading(sectionName) {
            const overlay = getOverlay(sectionOverlayId);
            if (!overlay) {
                return;
            }

            const target = overlay.querySelector("[data-section-loading-target]");
            if (target) {
                target.textContent = "Loading " + (sectionName || "section");
            }

            const status = overlay.querySelector("[data-section-loading-status]");
            if (status) {
                status.textContent = "Preparing " + (sectionName || "explorer") + " records.";
            }

            const startedAtUnixMs = Date.now();
            setStoredSectionLoadingStartedAt(startedAtUnixMs);
            showOverlay(sectionOverlayId, startedAtUnixMs);
        },
        showSectionRouteLoading(event, url, sectionName) {
            if (event) {
                event.preventDefault();
            }

            this.showSectionLoading(sectionName);
            window.setTimeout(function () {
                window.location.assign(url);
            }, 60);
            return false;
        },
        syncExplorerUrl(url, replaceHistory) {
            if (!url) {
                return;
            }

            const currentUrl = window.location.pathname + window.location.search + window.location.hash;
            if (currentUrl === url) {
                return;
            }

            const method = replaceHistory ? "replaceState" : "pushState";
            window.history[method](window.history.state, "", url);
        },
        hideExplorerLoading() {
            hideOverlay(navigationOverlayId);
        },
        hideSectionLoading() {
            hideOverlay(sectionOverlayId);
        },
        getSectionLoadingStartedAt() {
            return getStoredSectionLoadingStartedAt();
        },
        clearSectionLoadingStartedAt() {
            clearStoredSectionLoadingStartedAt();
        }
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () {
            hideOverlay(navigationOverlayId);
            hideOverlay(sectionOverlayId);
        }, { once: true });
    }
    else {
        hideOverlay(navigationOverlayId);
        hideOverlay(sectionOverlayId);
    }

    window.addEventListener("pageshow", function () {
        hideOverlay(navigationOverlayId);
        hideOverlay(sectionOverlayId);
    });
})();
