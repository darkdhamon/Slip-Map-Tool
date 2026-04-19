const recordObservers = new Map();
const recordLoading = new Map();

export function observeLoadMore(key, element, dotNetReference, methodName) {
    disconnectLoadMore(key);

    if (!key || !element || !dotNetReference || !methodName) {
        return;
    }

    recordLoading.set(key, false);
    const observer = new IntersectionObserver((entries) => {
        const entry = entries[0];
        if (!entry?.isIntersecting || recordLoading.get(key)) {
            return;
        }

        recordLoading.set(key, true);
        disconnectLoadMore(key);
        dotNetReference.invokeMethodAsync(methodName)
            .catch(() => {
                recordLoading.set(key, false);
            });
    }, {
        root: null,
        rootMargin: "640px 0px",
        threshold: 0.01
    });

    recordObservers.set(key, observer);
    observer.observe(element);
}

export function disconnectLoadMore(key) {
    const observer = recordObservers.get(key);
    if (observer) {
        observer.disconnect();
        recordObservers.delete(key);
    }

    recordLoading.delete(key);
}

export function disconnectAllLoadMore() {
    for (const key of recordObservers.keys()) {
        disconnectLoadMore(key);
    }
}

export function observeTimelineLoadMore(element, dotNetReference) {
    observeLoadMore("timeline", element, dotNetReference, "LoadMoreTimelineEvents");
}

export function disconnectTimelineLoadMore() {
    disconnectLoadMore("timeline");
}

export function observeColonyLoadMore(element, dotNetReference) {
    observeLoadMore("colony", element, dotNetReference, "LoadMoreColonies");
}

export function disconnectColonyLoadMore() {
    disconnectLoadMore("colony");
}
