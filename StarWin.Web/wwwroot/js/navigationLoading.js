(function () {
    const navigationOverlayId = "explorer-navigation-loading";
    const sectionOverlayId = "explorer-section-loading";

    function getOverlay(overlayId) {
        return document.getElementById(overlayId);
    }

    function updateBodyLoadingState() {
        const hasVisibleOverlay = [navigationOverlayId, sectionOverlayId]
            .map(getOverlay)
            .some(overlay => overlay && !overlay.hidden);

        document.body.classList.toggle("navigation-loading-active", hasVisibleOverlay);
    }

    function resetOverlayTimer(overlay) {
        if (!overlay) {
            return;
        }

        const timer = overlay.querySelector("[data-loading-timer='true']");
        if (!timer) {
            return;
        }

        timer.dataset.startedAtUnixMs = String(Date.now());
    }

    function showOverlay(overlayId) {
        const overlay = getOverlay(overlayId);
        if (!overlay) {
            return;
        }

        resetOverlayTimer(overlay);
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

            showOverlay(sectionOverlayId);
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
