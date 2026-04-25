const timers = new WeakMap();
const observer = new MutationObserver(handleMutations);

function formatElapsed(startedAtUnixMs) {
    const elapsedMilliseconds = Math.max(0, Date.now() - startedAtUnixMs);
    const elapsedSeconds = Math.floor(elapsedMilliseconds / 1000);
    const hours = Math.floor(elapsedSeconds / 3600);
    const minutes = Math.floor((elapsedSeconds % 3600) / 60);
    const seconds = elapsedSeconds % 60;
    const tenths = Math.floor((elapsedMilliseconds % 1000) / 100);

    if (hours > 0) {
        return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
    }

    return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}.${tenths}`;
}

function render(element, startedAtUnixMs) {
    element.textContent = `Elapsed time: ${formatElapsed(startedAtUnixMs)}`;
}

function getStartedAtUnixMs(element) {
    const startedAt = Number(element.dataset.startedAtUnixMs);
    return Number.isFinite(startedAt) ? startedAt : Date.now();
}

function startTimer(element) {
    const startedAtUnixMs = getStartedAtUnixMs(element);
    const currentTimer = timers.get(element);
    if (currentTimer?.startedAtUnixMs === startedAtUnixMs) {
        return;
    }

    stopTimer(element);
    render(element, startedAtUnixMs);

    const intervalId = window.setInterval(() => {
        if (!element.isConnected) {
            stopTimer(element);
            return;
        }

        render(element, startedAtUnixMs);
    }, 100);

    timers.set(element, { intervalId, startedAtUnixMs });
}

function stopTimer(element) {
    const timer = timers.get(element);
    if (!timer) {
        return;
    }

    window.clearInterval(timer.intervalId);
    timers.delete(element);
}

function refreshTimers(root = document) {
    const timerElements = root.querySelectorAll?.('[data-loading-timer="true"]') ?? [];
    for (const element of timerElements) {
        startTimer(element);
    }
}

function handleMutations(mutations) {
    for (const mutation of mutations) {
        if (mutation.type === "childList") {
            for (const node of mutation.addedNodes) {
                if (node.nodeType !== Node.ELEMENT_NODE) {
                    continue;
                }

                const element = /** @type {Element} */ (node);
                if (element.matches?.('[data-loading-timer="true"]')) {
                    startTimer(element);
                }

                refreshTimers(element);
            }

            for (const node of mutation.removedNodes) {
                if (node.nodeType !== Node.ELEMENT_NODE) {
                    continue;
                }

                const element = /** @type {Element} */ (node);
                if (element.matches?.('[data-loading-timer="true"]')) {
                    stopTimer(element);
                }

                const nestedTimers = element.querySelectorAll?.('[data-loading-timer="true"]') ?? [];
                for (const timerElement of nestedTimers) {
                    stopTimer(timerElement);
                }
            }
        }

        if (mutation.type === "attributes"
            && mutation.target instanceof Element
            && mutation.target.matches('[data-loading-timer="true"]')) {
            startTimer(mutation.target);
        }
    }
}

refreshTimers();
observer.observe(document.body, {
    childList: true,
    subtree: true,
    attributes: true,
    attributeFilter: ["data-started-at-unix-ms"]
});
