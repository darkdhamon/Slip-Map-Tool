const timers = new WeakMap();

function formatElapsed(startedAtUnixMs) {
    const elapsedSeconds = Math.max(0, Math.floor((Date.now() - startedAtUnixMs) / 1000));
    const hours = Math.floor(elapsedSeconds / 3600);
    const minutes = Math.floor((elapsedSeconds % 3600) / 60);
    const seconds = elapsedSeconds % 60;

    if (hours > 0) {
        return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
    }

    return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

function render(element, startedAtUnixMs) {
    if (!element) {
        return;
    }

    element.textContent = `Elapsed time: ${formatElapsed(startedAtUnixMs)}`;
}

export function start(element, startedAtUnixMs) {
    stop(element);
    render(element, startedAtUnixMs);

    const intervalId = window.setInterval(() => {
        if (!element || !element.isConnected) {
            stop(element);
            return;
        }

        render(element, startedAtUnixMs);
    }, 1000);

    timers.set(element, intervalId);
}

export function stop(element) {
    if (!element) {
        return;
    }

    const intervalId = timers.get(element);
    if (intervalId !== undefined) {
        window.clearInterval(intervalId);
        timers.delete(element);
    }
}
