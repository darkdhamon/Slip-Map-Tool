let threePromise;
const maps = new WeakMap();
const performanceSampleSize = 150;
const performanceWarmupFrames = 45;
const slowFrameMilliseconds = 34;
const slowAverageMilliseconds = 28;
const slowFrameRatio = 0.32;
const minimumAdaptiveSystemLimit = 150;

async function loadThree() {
    if (!threePromise) {
        threePromise = import("https://cdn.jsdelivr.net/npm/three@0.164.1/build/three.module.js");
    }

    return threePromise;
}

export async function renderSectorMap(host, sector, selectedSystemId, dotNetReference) {
    if (!host) {
        return;
    }

    const THREE = await loadThree();
    let state = maps.get(host);
    if (!state) {
        state = createMap(host, THREE);
        maps.set(host, state);
    }

    state.dotNetReference = dotNetReference;
    state.systems = sector.systems ?? [];
    resetPerformanceMonitor(state);
    rebuildScene(state, sector, selectedSystemId);
    focusSystem(state, selectedSystemId);
    resize(state);
    state.renderer.render(state.scene, state.camera);
}

export function toggleMapFullscreen(host) {
    if (!host) {
        return;
    }

    if (document.fullscreenElement === host) {
        document.exitFullscreen();
        return;
    }

    host.requestFullscreen();
}

export function recenterSectorMap(host, systemId) {
    const state = maps.get(host);
    if (!state) {
        return;
    }

    focusSystem(state, systemId);
}

export function disposeSectorMap(host) {
    const state = maps.get(host);
    if (!state) {
        return;
    }

    state.resizeObserver.disconnect();
    cancelAnimationFrame(state.animationFrame);
    state.renderer.dispose();
    state.host.replaceChildren();
    maps.delete(host);
}

function createMap(host, THREE) {
    const canvas = document.createElement("canvas");
    canvas.className = "sector-map-canvas";
    canvas.tabIndex = 0;
    canvas.setAttribute("aria-label", "3D sector map");
    host.appendChild(canvas);

    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: false, preserveDrawingBuffer: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.setClearColor(0x000000, 1);

    const scene = new THREE.Scene();

    const camera = new THREE.PerspectiveCamera(52, 1, 0.1, 2000);

    const ambient = new THREE.AmbientLight(0x88ccff, 0.45);
    scene.add(ambient);

    const key = new THREE.DirectionalLight(0xa5f3fc, 1.15);
    key.position.set(80, 120, 60);
    scene.add(key);

    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();
    const clickable = [];
    const group = new THREE.Group();
    const routeGroup = new THREE.Group();
    scene.add(routeGroup);
    scene.add(group);

    const resizeObserver = new ResizeObserver(() => resize(maps.get(host)));
    resizeObserver.observe(host);

    const state = {
        THREE,
        host,
        renderer,
        scene,
        camera,
        raycaster,
        pointer,
        clickable,
        group,
        routeGroup,
        resizeObserver,
        animationFrame: 0,
        dotNetReference: null,
        systems: [],
        systemPositions: new Map(),
        performance: createPerformanceMonitor(),
        target: new THREE.Vector3(36, 18, 36),
        cameraOffset: new THREE.Vector3(84, 70, 82),
        radius: 116,
        theta: 0.72,
        phi: 0.92,
        roll: 0,
        drag: null
    };

    canvas.addEventListener("pointerdown", event => {
        canvas.focus({ preventScroll: true });
        state.pointerStart = { x: event.clientX, y: event.clientY };
        state.drag = {
            button: event.button,
            x: event.clientX,
            y: event.clientY,
            moved: 0
        };
        canvas.setPointerCapture(event.pointerId);
    });

    canvas.addEventListener("pointermove", event => handlePointerMove(state, event));
    canvas.addEventListener("pointerup", event => handlePointerUp(state, event));
    canvas.addEventListener("wheel", event => handleWheel(state, event), { passive: false });
    canvas.addEventListener("keydown", event => handleKeyDown(state, event));
    canvas.addEventListener("contextmenu", event => event.preventDefault());
    updateCamera(state);
    animate(state);

    return state;
}

function rebuildScene(state, sector, selectedSystemId) {
    const THREE = state.THREE;
    state.group.clear();
    state.routeGroup.clear();
    state.clickable.length = 0;
    state.systemPositions.clear();

    const viewDistance = Number(sector.viewDistanceParsecs ?? 20);
    const fadeStart = Number(sector.fadeStartParsecs ?? Math.max(0, viewDistance - 1));
    const routeActive = Boolean(sector.routeActive);
    const routeSystemIds = new Set(sector.routeSystemIds ?? []);

    for (const system of sector.systems ?? []) {
        const cluster = new THREE.Group();
        cluster.position.set(system.x * 8, system.z * 6, system.y * 12);
        cluster.userData = { systemId: system.id, systemName: system.name };
        state.systemPositions.set(system.id, cluster.position.clone());
        const isRouteSystem = routeSystemIds.has(system.id) || Boolean(system.isRouteSystem);
        const focusOpacity = isRouteSystem ? 1 : getFocusDistanceOpacity(system.distanceFromFocus, fadeStart, viewDistance);
        const systemOpacity = routeActive && !isRouteSystem ? Math.min(focusOpacity, 0.16) : focusOpacity;

        const bodies = (system.bodies?.length ? system.bodies : [{ kind: "Unknown" }]).slice(0, 3);
        const bodyRadii = bodies.map(body => getAstralBodyVisualRadius(body, system.id === selectedSystemId));
        const bodyGap = 0.68;
        const totalWidth = bodyRadii.reduce((sum, radius) => sum + radius * 2, 0) + Math.max(0, bodyRadii.length - 1) * bodyGap;
        let bodyX = -totalWidth / 2;
        bodies.forEach((body, index) => {
            const dot = createAstralBodyMesh(THREE, body, system.id === selectedSystemId);
            const radius = bodyRadii[index];
            bodyX += radius;
            dot.position.set(bodyX, index % 2 === 0 ? 0 : 0.68, 0);
            bodyX += radius + bodyGap;
            assignSystemUserData(dot, cluster.userData);
            cluster.add(dot);
            state.clickable.push(dot);
        });

        if (system.showLabel) {
            const label = createLabelSprite(THREE, system.name, system.id === selectedSystemId);
            label.position.set(0, 3.45, 0);
            cluster.add(label);
        }

        if (system.id === selectedSystemId) {
            const ringRadius = Math.max(2.45, totalWidth * 0.62 + 0.78);
            const ring = new THREE.Mesh(
                new THREE.TorusGeometry(ringRadius, 0.045, 8, 48),
                new THREE.MeshBasicMaterial({ color: 0x67e8f9, transparent: true, opacity: 0.76 })
            );
            ring.rotation.x = Math.PI / 2;
            cluster.add(ring);
        }

        applyFocusOpacity(cluster, systemOpacity);
        state.group.add(cluster);
    }

    addRouteLines(state, sector, selectedSystemId);
}

function addRouteLines(state, sector, selectedSystemId) {
    const THREE = state.THREE;
    const routes = sector.routes ?? [];
    const routePath = sector.routePath ?? [];
    if (!routes.length && !routePath.length) {
        return;
    }

    const standardPositions = [];
    const selectedPositions = [];
    const ownedPositions = [];
    const selectedOwnedPositions = [];
    const goldPositions = [];
    const selectedGoldPositions = [];

    for (const route of routes) {
        const source = state.systemPositions.get(route.sourceId);
        const target = state.systemPositions.get(route.targetId);
        if (!source || !target) {
            continue;
        }

        const selected = route.sourceId === selectedSystemId || route.targetId === selectedSystemId;
        const bucket = route.isGold
            ? selected ? selectedGoldPositions : goldPositions
            : route.isOwned
                ? selected ? selectedOwnedPositions : ownedPositions
                : selected ? selectedPositions : standardPositions;
        bucket.push(source.x, source.y, source.z, target.x, target.y, target.z);
    }

    if (standardPositions.length) {
        state.routeGroup.add(createRouteLineSegments(
            THREE,
            standardPositions,
            0x22d3ee,
            routePath.length ? 0.14 : 0.5,
            true));
        if (!routePath.length) {
            state.routeGroup.add(createRouteLineSegments(
                THREE,
                standardPositions,
                0xa7f3d0,
                0.26,
                false));
        }
    }

    if (selectedPositions.length) {
        state.routeGroup.add(createRouteLineSegments(
            THREE,
            selectedPositions,
            0x67e8f9,
            routePath.length ? 0.22 : 0.96,
            true));
        if (!routePath.length) {
            state.routeGroup.add(createRouteLineSegments(
                THREE,
                selectedPositions,
                0xecfeff,
                0.55,
            false));
        }
    }

    if (ownedPositions.length) {
        state.routeGroup.add(createRouteLineSegments(
            THREE,
            ownedPositions,
            0xa855f7,
            routePath.length ? 0.22 : 0.68,
            true));
        if (!routePath.length) {
            state.routeGroup.add(createRouteLineSegments(
                THREE,
                ownedPositions,
                0xd8b4fe,
                0.36,
                false));
        }
    }

    if (selectedOwnedPositions.length) {
        state.routeGroup.add(createRouteLineSegments(
            THREE,
            selectedOwnedPositions,
            0xc084fc,
            routePath.length ? 0.34 : 1,
            true));
        if (!routePath.length) {
            state.routeGroup.add(createRouteLineSegments(
                THREE,
                selectedOwnedPositions,
                0xf5d0fe,
                0.58,
            false));
        }
    }

    if (goldPositions.length) {
        state.routeGroup.add(createRouteLineSegments(
            THREE,
            goldPositions,
            0xfbbf24,
            routePath.length ? 0.28 : 0.8,
            true));
        if (!routePath.length) {
            state.routeGroup.add(createRouteLineSegments(
                THREE,
                goldPositions,
                0xfef3c7,
                0.42,
                false));
        }
    }

    if (selectedGoldPositions.length) {
        state.routeGroup.add(createRouteLineSegments(
            THREE,
            selectedGoldPositions,
            0xfacc15,
            routePath.length ? 0.42 : 1,
            true));
        if (!routePath.length) {
            state.routeGroup.add(createRouteLineSegments(
                THREE,
                selectedGoldPositions,
                0xfffbeb,
                0.62,
                false));
        }
    }

    if (routePath.length) {
        const hyperlanePositions = [];
        const offLanePositions = [];
        for (const route of routePath) {
            const source = state.systemPositions.get(route.sourceId);
            const target = state.systemPositions.get(route.targetId);
            if (!source || !target) {
                continue;
            }

            const bucket = route.isHyperlane ? hyperlanePositions : offLanePositions;
            bucket.push(source.x, source.y, source.z, target.x, target.y, target.z);
        }

        if (hyperlanePositions.length) {
            state.routeGroup.add(createRouteLineSegments(THREE, hyperlanePositions, 0x34d399, 1, false, 2.6));
            state.routeGroup.add(createRouteLineSegments(THREE, hyperlanePositions, 0xbbf7d0, 0.62, true, 1.25));
        }

        if (offLanePositions.length) {
            state.routeGroup.add(createRouteLineSegments(THREE, offLanePositions, 0x7f1d1d, 0.86, false, 3.4, false));
            state.routeGroup.add(createRouteLineSegments(THREE, offLanePositions, 0xef4444, 1, true, 1.8, false));
            state.routeGroup.add(createRouteLineSegments(THREE, offLanePositions, 0xff0000, 0.62, false, 5.2, true));
        }
    }
}

function createRouteLineSegments(THREE, positions, color, opacity, depthTest, lineWidth = 1, additive = true) {
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute("position", new THREE.Float32BufferAttribute(positions, 3));
    const material = new THREE.LineBasicMaterial({
        color,
        transparent: true,
        opacity,
        linewidth: lineWidth,
        depthWrite: false,
        depthTest,
        blending: additive ? THREE.AdditiveBlending : THREE.NormalBlending
    });

    return new THREE.LineSegments(geometry, material);
}

function getFocusDistanceOpacity(distanceFromFocus, fadeStart, viewDistance) {
    const distance = Number(distanceFromFocus ?? 0);
    if (distance <= fadeStart || viewDistance <= fadeStart) {
        return 1;
    }

    const fadeProgress = Math.min(1, Math.max(0, (distance - fadeStart) / (viewDistance - fadeStart)));
    return 1 - fadeProgress * 0.75;
}

function applyFocusOpacity(root, opacity) {
    if (opacity >= 0.999) {
        return;
    }

    root.traverse(object => {
        const material = object.material;
        if (!material) {
            return;
        }

        const materials = Array.isArray(material) ? material : [material];
        for (const item of materials) {
            if (item.userData.baseOpacity === undefined) {
                item.userData.baseOpacity = item.opacity === undefined ? 1 : item.opacity;
            }

            item.transparent = true;
            item.opacity = Math.max(0.08, item.userData.baseOpacity * opacity);
        }
    });
}

function createAstralBodyMesh(THREE, body, selected) {
    if (body.imageUrl) {
        return createImageBodyMesh(THREE, body, selected);
    }

    const kind = (body.kind ?? "").toLowerCase();
    if (kind.includes("black")) {
        return createBlackHoleMesh(THREE, selected);
    }

    if (kind.includes("nebula")) {
        return createNebulaMesh(THREE, selected);
    }

    if (kind.includes("pulsar")) {
        return createPulsarMesh(THREE, selected);
    }

    if (kind.includes("quasar")) {
        return createQuasarMesh(THREE, selected);
    }

    if (kind.includes("ion")) {
        return createIonStormMesh(THREE, selected);
    }

    if (kind.includes("rift")) {
        return createSpaceRiftMesh(THREE, selected);
    }

    const spectralColor = getSpectralColor(body);
    const color = spectralColor.color;
    const emissive = spectralColor.emissive;
    const radius = getAstralBodyVisualRadius(body, selected);
    const luminosity = getPositiveNumber(body.luminosity);
    const haloScale = 1.28 + Math.min(0.34, Math.log10((luminosity ?? 0) + 1) * 0.12);

    const material = new THREE.MeshStandardMaterial({
        color,
        emissive,
        emissiveIntensity: selected ? 1.1 : 0.55 + Math.min(0.34, (luminosity ?? 0) * 0.06),
        roughness: 0.38,
        metalness: 0.08
    });

    const mesh = new THREE.Mesh(new THREE.SphereGeometry(radius, 24, 16), material);
    const halo = new THREE.Mesh(
        new THREE.SphereGeometry(radius * haloScale, 24, 16),
        new THREE.MeshBasicMaterial({
            color: emissive,
            transparent: true,
            opacity: selected ? 0.16 : 0.045,
            depthWrite: false
        })
    );
    mesh.add(halo);
    return mesh;
}

function createImageBodyMesh(THREE, body, selected) {
    const radius = getAstralBodyVisualRadius(body, selected);
    const group = new THREE.Group();
    const texture = new THREE.TextureLoader().load(body.imageUrl);
    texture.colorSpace = THREE.SRGBColorSpace;

    const image = new THREE.Mesh(
        new THREE.PlaneGeometry(radius * 2.6, radius * 2.6),
        new THREE.MeshBasicMaterial({
            map: texture,
            transparent: true,
            side: THREE.DoubleSide
        })
    );
    image.userData.faceCamera = true;
    group.add(image);

    const frame = new THREE.Mesh(
        new THREE.TorusGeometry(radius * 1.45, radius * 0.045, 8, 72),
        new THREE.MeshBasicMaterial({
            color: selected ? 0x67e8f9 : 0x38bdf8,
            transparent: true,
            opacity: selected ? 0.86 : 0.48,
            depthWrite: false
        })
    );
    frame.rotation.z = Math.PI / 2;
    group.add(frame);
    return group;
}

function faceImageMarkers(state) {
    state.group.traverse(object => {
        if (object.userData?.faceCamera) {
            object.quaternion.copy(state.camera.quaternion);
        }
    });
}

function getAstralBodyVisualRadius(body, selected) {
    const kind = (body.kind ?? "").toLowerCase();
    if (!kind.includes("star")) {
        return selected ? 0.72 : 0.54;
    }

    const solarMasses = getPositiveNumber(body.solarMasses);
    const luminosity = getPositiveNumber(body.luminosity);
    const massScale = solarMasses ? Math.pow(solarMasses, 0.62) : null;
    const luminosityScale = luminosity ? Math.pow(luminosity, 0.12) : null;
    const scale = massScale ?? luminosityScale ?? 1;
    const radius = 0.34 + Math.min(0.76, Math.max(0.18, scale) * 0.34);
    return selected ? radius * 1.18 : radius;
}

function getPositiveNumber(value) {
    const number = Number(value);
    return Number.isFinite(number) && number > 0 ? number : null;
}

function assignSystemUserData(object, userData) {
    object.userData = { ...object.userData, ...userData };
    object.traverse?.(child => {
        child.userData = { ...child.userData, ...userData };
    });
}

function createBlackHoleMesh(THREE, selected) {
    const group = new THREE.Group();
    const radius = selected ? 1.3 : 1;

    const core = new THREE.Mesh(
        new THREE.SphereGeometry(radius, 32, 24),
        new THREE.MeshBasicMaterial({ color: 0x000000 })
    );
    group.add(core);

    const photonRing = new THREE.Mesh(
        new THREE.TorusGeometry(radius * 1.35, radius * 0.08, 12, 72),
        new THREE.MeshBasicMaterial({ color: 0xc4b5fd, transparent: true, opacity: selected ? 0.9 : 0.68 })
    );
    photonRing.rotation.x = Math.PI / 2.25;
    group.add(photonRing);

    const accretionDisk = new THREE.Mesh(
        new THREE.TorusGeometry(radius * 2.15, radius * 0.13, 12, 96),
        new THREE.MeshBasicMaterial({ color: 0xfb923c, transparent: true, opacity: selected ? 0.72 : 0.5 })
    );
    accretionDisk.scale.y = 0.42;
    accretionDisk.rotation.x = Math.PI / 2.7;
    accretionDisk.rotation.z = Math.PI / 7;
    group.add(accretionDisk);

    const lensGlow = new THREE.Mesh(
        new THREE.SphereGeometry(radius * 2.8, 32, 20),
        new THREE.MeshBasicMaterial({ color: 0x818cf8, transparent: true, opacity: selected ? 0.12 : 0.07, depthWrite: false })
    );
    group.add(lensGlow);

    return group;
}

function createNebulaMesh(THREE, selected) {
    const group = new THREE.Group();
    const scale = selected ? 1.18 : 1;
    const cloudColors = [0x38bdf8, 0xdb2777, 0xa78bfa, 0x22d3ee];

    cloudColors.forEach((color, index) => {
        const cloud = new THREE.Mesh(
            new THREE.SphereGeometry((1.25 + index * 0.2) * scale, 24, 16),
            new THREE.MeshBasicMaterial({
                color,
                transparent: true,
                opacity: selected ? 0.24 : 0.16,
                depthWrite: false,
                blending: THREE.AdditiveBlending
            })
        );
        cloud.position.set(
            Math.cos(index * 1.7) * 0.75,
            Math.sin(index * 2.1) * 0.42,
            Math.sin(index * 1.4) * 0.65
        );
        cloud.scale.set(1.45, 0.78 + index * 0.1, 1.05);
        cloud.rotation.set(index * 0.7, index * 0.4, index * 0.9);
        group.add(cloud);
    });

    const core = new THREE.Points(
        new THREE.BufferGeometry().setAttribute(
            "position",
            new THREE.Float32BufferAttribute([
                -0.8, 0.2, 0.3,
                -0.2, -0.4, 0.1,
                0.5, 0.35, -0.2,
                0.9, -0.1, 0.4,
                0.1, 0.1, -0.7
            ], 3)
        ),
        new THREE.PointsMaterial({
            color: 0xe0f2fe,
            size: selected ? 0.38 : 0.28,
            transparent: true,
            opacity: 0.9,
            depthWrite: false
        })
    );
    group.add(core);

    return group;
}

function createPulsarMesh(THREE, selected) {
    const group = new THREE.Group();
    const radius = selected ? 1.05 : 0.82;
    const core = new THREE.Mesh(
        new THREE.SphereGeometry(radius, 24, 18),
        new THREE.MeshStandardMaterial({
            color: 0xdbeafe,
            emissive: 0x60a5fa,
            emissiveIntensity: selected ? 1.4 : 0.95,
            roughness: 0.24,
            metalness: 0.08
        })
    );
    group.add(core);

    const beamMaterial = new THREE.MeshBasicMaterial({
        color: 0x67e8f9,
        transparent: true,
        opacity: selected ? 0.72 : 0.5,
        depthWrite: false,
        blending: THREE.AdditiveBlending
    });
    const beamGeometry = new THREE.CylinderGeometry(0.12, 0.42, selected ? 9 : 7, 18, 1, true);
    const beamA = new THREE.Mesh(beamGeometry, beamMaterial);
    beamA.rotation.z = Math.PI / 2;
    const beamB = beamA.clone();
    beamB.rotation.z = -Math.PI / 2;
    group.add(beamA, beamB);

    const pulseRing = new THREE.Mesh(
        new THREE.TorusGeometry(radius * 1.9, 0.035, 8, 64),
        new THREE.MeshBasicMaterial({ color: 0xbae6fd, transparent: true, opacity: selected ? 0.7 : 0.42 })
    );
    pulseRing.rotation.x = Math.PI / 2;
    group.add(pulseRing);
    return group;
}

function createQuasarMesh(THREE, selected) {
    const group = new THREE.Group();
    const radius = selected ? 1.1 : 0.86;
    const core = new THREE.Mesh(
        new THREE.SphereGeometry(radius, 28, 20),
        new THREE.MeshBasicMaterial({ color: 0xf8fbff })
    );
    group.add(core);

    const jetMaterial = new THREE.MeshBasicMaterial({
        color: 0x93c5fd,
        transparent: true,
        opacity: selected ? 0.86 : 0.62,
        depthWrite: false,
        blending: THREE.AdditiveBlending
    });
    const jetGeometry = new THREE.ConeGeometry(0.55, selected ? 9 : 7, 24, 1, true);
    const jetA = new THREE.Mesh(jetGeometry, jetMaterial);
    jetA.position.y = selected ? 4.8 : 3.8;
    const jetB = jetA.clone();
    jetB.rotation.x = Math.PI;
    jetB.position.y = selected ? -4.8 : -3.8;
    group.add(jetA, jetB);

    const disk = new THREE.Mesh(
        new THREE.TorusGeometry(radius * 2.2, 0.08, 8, 96),
        new THREE.MeshBasicMaterial({ color: 0xfbbf24, transparent: true, opacity: selected ? 0.66 : 0.46 })
    );
    disk.rotation.x = Math.PI / 2;
    group.add(disk);
    return group;
}

function createIonStormMesh(THREE, selected) {
    const group = new THREE.Group();
    const colors = [0x22d3ee, 0x34d399, 0xa78bfa];
    colors.forEach((color, index) => {
        const ring = new THREE.Mesh(
            new THREE.TorusGeometry((1.1 + index * 0.45) * (selected ? 1.2 : 1), 0.045, 8, 72),
            new THREE.MeshBasicMaterial({
                color,
                transparent: true,
                opacity: selected ? 0.58 : 0.38,
                depthWrite: false,
                blending: THREE.AdditiveBlending
            })
        );
        ring.rotation.set(index * 0.9, Math.PI / 2.5 + index * 0.45, index * 0.65);
        group.add(ring);
    });

    const sparks = new THREE.BufferGeometry();
    const positions = [];
    for (let i = 0; i < 18; i++) {
        positions.push(Math.cos(i) * (0.6 + (i % 4) * 0.35), Math.sin(i * 1.7) * 0.9, Math.sin(i) * (0.6 + (i % 3) * 0.4));
    }
    sparks.setAttribute("position", new THREE.Float32BufferAttribute(positions, 3));
    group.add(new THREE.Points(sparks, new THREE.PointsMaterial({ color: 0xecfeff, size: selected ? 0.2 : 0.15, transparent: true, opacity: 0.92 })));
    return group;
}

function createSpaceRiftMesh(THREE, selected) {
    const group = new THREE.Group();
    const material = new THREE.MeshBasicMaterial({
        color: 0xc084fc,
        transparent: true,
        opacity: selected ? 0.72 : 0.5,
        depthWrite: false,
        side: THREE.DoubleSide,
        blending: THREE.AdditiveBlending
    });
    const geometry = new THREE.PlaneGeometry(selected ? 4.8 : 3.8, selected ? 1.5 : 1.15, 1, 1);
    const riftA = new THREE.Mesh(geometry, material);
    riftA.rotation.set(0.4, 0.8, -0.2);
    const riftB = riftA.clone();
    riftB.rotation.set(-0.5, -0.7, 0.35);
    group.add(riftA, riftB);

    const edge = new THREE.Mesh(
        new THREE.TorusGeometry(selected ? 2.3 : 1.8, 0.035, 8, 72),
        new THREE.MeshBasicMaterial({ color: 0x67e8f9, transparent: true, opacity: selected ? 0.7 : 0.45 })
    );
    edge.scale.y = 0.28;
    edge.rotation.x = Math.PI / 2.4;
    group.add(edge);
    return group;
}

function getSpectralColor(body) {
    const classification = (body.classification ?? "").trim().toUpperCase();
    const spectralClass = classification.match(/[OBAFGKM]/)?.[0];

    switch (spectralClass) {
        case "O":
            return { color: 0x9bbcff, emissive: 0x3b82f6 };
        case "B":
            return { color: 0xbdd7ff, emissive: 0x60a5fa };
        case "A":
            return { color: 0xf8fbff, emissive: 0xc7d2fe };
        case "F":
            return { color: 0xfff3c4, emissive: 0xfacc15 };
        case "G":
            return { color: 0xffdd74, emissive: 0xf59e0b };
        case "K":
            return { color: 0xffa24a, emissive: 0xea580c };
        case "M":
            return { color: 0xff5c45, emissive: 0xdc2626 };
        default:
            return { color: 0xfacc15, emissive: 0x7c2d12 };
    }
}

function createLabelSprite(THREE, text, selected) {
    const canvas = document.createElement("canvas");
    canvas.width = 256;
    canvas.height = 64;
    const context = canvas.getContext("2d");
    context.clearRect(0, 0, canvas.width, canvas.height);
    context.font = selected ? "700 24px Segoe UI" : "600 21px Segoe UI";
    context.fillStyle = selected ? "#ecfeff" : "#c8d8ea";
    context.shadowColor = "#020617";
    context.shadowBlur = 8;
    context.fillText(text, 8, 38);

    const texture = new THREE.CanvasTexture(canvas);
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, transparent: true }));
    sprite.scale.set(18, 4.5, 1);
    return sprite;
}

function handlePointerUp(state, event) {
    if (!state.pointerStart) {
        return;
    }

    const moved = Math.abs(event.clientX - state.pointerStart.x) + Math.abs(event.clientY - state.pointerStart.y);
    state.pointerStart = null;
    const wasDragging = state.drag?.moved > 6;
    state.drag = null;
    if (moved > 6 || wasDragging || event.button !== 0) {
        return;
    }

    const rect = state.renderer.domElement.getBoundingClientRect();
    state.pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    state.pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
    state.raycaster.setFromCamera(state.pointer, state.camera);

    const hit = state.raycaster.intersectObjects(state.clickable, true)
        .find(item => item.object.userData?.systemId);
    if (hit && state.dotNetReference) {
        focusSystem(state, hit.object.userData.systemId);
        state.dotNetReference.invokeMethodAsync("SelectSystemFromMap", hit.object.userData.systemId);
    }
}

function focusSystem(state, systemId) {
    const position = state.systemPositions.get(systemId);
    if (!position) {
        return;
    }

    state.target.copy(position);
    updateCamera(state);
}

function handlePointerMove(state, event) {
    if (!state.drag) {
        return;
    }

    const dx = event.clientX - state.drag.x;
    const dy = event.clientY - state.drag.y;
    state.drag.x = event.clientX;
    state.drag.y = event.clientY;
    state.drag.moved += Math.abs(dx) + Math.abs(dy);

    if (state.drag.button === 0) {
        rotateCameraOffset(state, -dx * 0.006, -dy * 0.006);
    } else {
        const panScale = state.radius * 0.0016;
        const forward = new state.THREE.Vector3().subVectors(state.target, state.camera.position).normalize();
        const right = new state.THREE.Vector3().crossVectors(forward, state.camera.up).normalize();
        const up = new state.THREE.Vector3().crossVectors(right, forward).normalize();
        state.target.addScaledVector(right, -dx * panScale);
        state.target.addScaledVector(up, dy * panScale);
    }

    updateCamera(state);
}

function handleWheel(state, event) {
    event.preventDefault();
    const direction = event.deltaY > 0 ? 1 : -1;
    state.radius = Math.max(26, Math.min(220, state.radius * (1 + direction * 0.12)));
    updateCamera(state);
}

function handleKeyDown(state, event) {
    if (event.altKey || event.ctrlKey || event.metaKey) {
        return;
    }

    const key = event.key.toLowerCase();
    const rotationStep = event.shiftKey ? 0.16 : 0.08;
    let handled = true;

    switch (key) {
        case "a":
            rotateCameraOffset(state, -rotationStep, 0);
            break;
        case "d":
            rotateCameraOffset(state, rotationStep, 0);
            break;
        case "w":
            rotateCameraOffset(state, 0, -rotationStep);
            break;
        case "s":
            rotateCameraOffset(state, 0, rotationStep);
            break;
        case "q":
            state.roll += rotationStep;
            break;
        case "e":
            state.roll -= rotationStep;
            break;
        default:
            handled = false;
            break;
    }

    if (!handled) {
        return;
    }

    event.preventDefault();
    updateCamera(state);
}

function updateCamera(state) {
    state.cameraOffset.setLength(state.radius);
    state.camera.position.copy(state.target).add(state.cameraOffset);
    state.camera.lookAt(state.target);
    if (state.roll) {
        state.camera.rotateZ(state.roll);
    }
}

function rotateCameraOffset(state, yawDelta, pitchDelta) {
    const THREE = state.THREE;
    const offset = state.cameraOffset;
    const forward = offset.clone().multiplyScalar(-1).normalize();
    const cameraUp = state.camera.up.clone().applyQuaternion(state.camera.quaternion).normalize();
    const right = new THREE.Vector3().crossVectors(forward, cameraUp).normalize();

    if (Math.abs(yawDelta) > 0) {
        offset.applyAxisAngle(cameraUp, yawDelta);
    }

    if (Math.abs(pitchDelta) > 0) {
        offset.applyAxisAngle(right, pitchDelta);
        const normalized = offset.clone().normalize();
        if (Math.abs(normalized.dot(cameraUp)) > 0.985) {
            offset.applyAxisAngle(right, -pitchDelta);
        }
    }

    offset.setLength(state.radius);
}

function resize(state) {
    if (!state) {
        return;
    }

    const width = Math.max(state.host.clientWidth, 320);
    const height = Math.max(state.host.clientHeight, 360);
    state.camera.aspect = width / height;
    state.camera.updateProjectionMatrix();
    state.renderer.setSize(width, height, false);
}

function animate(state, timestamp = performance.now()) {
    state.animationFrame = requestAnimationFrame(nextTimestamp => animate(state, nextTimestamp));
    trackMapPerformance(state, timestamp);
    faceImageMarkers(state);
    state.renderer.render(state.scene, state.camera);
}

function createPerformanceMonitor() {
    return {
        lastTimestamp: 0,
        frameCount: 0,
        samples: [],
        reported: false
    };
}

function resetPerformanceMonitor(state) {
    state.performance = createPerformanceMonitor();
}

function trackMapPerformance(state, timestamp) {
    const monitor = state.performance;
    if (!monitor || monitor.reported) {
        return;
    }

    if (monitor.lastTimestamp === 0) {
        monitor.lastTimestamp = timestamp;
        return;
    }

    const frameMilliseconds = timestamp - monitor.lastTimestamp;
    monitor.lastTimestamp = timestamp;
    monitor.frameCount++;
    if (monitor.frameCount <= performanceWarmupFrames) {
        return;
    }

    monitor.samples.push(frameMilliseconds);
    if (monitor.samples.length < performanceSampleSize) {
        return;
    }

    const averageFrameMilliseconds = monitor.samples.reduce((sum, sample) => sum + sample, 0) / monitor.samples.length;
    const slowFrames = monitor.samples.filter(sample => sample >= slowFrameMilliseconds).length;
    const currentLimit = Number(state.systems.length || 0);
    const suggestedLimit = Math.max(minimumAdaptiveSystemLimit, Math.floor(currentLimit * 0.72));
    const hasSustainedLag = averageFrameMilliseconds >= slowAverageMilliseconds
        || slowFrames / monitor.samples.length >= slowFrameRatio;

    monitor.samples.length = 0;
    if (!hasSustainedLag || suggestedLimit >= currentLimit || currentLimit <= minimumAdaptiveSystemLimit) {
        return;
    }

    monitor.reported = true;
    state.dotNetReference?.invokeMethodAsync("ReduceSectorMapRenderLimit", suggestedLimit, averageFrameMilliseconds, currentLimit);
}
