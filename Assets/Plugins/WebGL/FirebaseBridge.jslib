mergeInto(LibraryManager.library, {
    InitFirebaseBridge: function () {
        var globalScope = typeof window !== "undefined" ? window : null;
        if (!globalScope) {
            console.warn("[FirebaseBridge] window is unavailable.");
            return;
        }

        if (!globalScope.__fbAuth) {
            globalScope.__fbAuth = {
                uid: "",
                idToken: "",
                displayName: "Player",
                projectId: ""
            };
        }

        function sendAuthToUnity() {
            var payload = JSON.stringify(globalScope.__fbAuth);
            var moduleInstance = globalScope.Module;

            if (moduleInstance && typeof moduleInstance.SendMessage === "function") {
                moduleInstance.SendMessage("FirebaseManager", "OnAuthReceived", payload);
                return;
            }

            if (typeof globalScope.SendMessage === "function") {
                globalScope.SendMessage("FirebaseManager", "OnAuthReceived", payload);
                return;
            }

            console.warn("[FirebaseBridge] SendMessage not available yet.");
        }

        function applyAuthPayload(data) {
            if (!data || !data.uid || !data.idToken) {
                console.warn("[FirebaseBridge] Invalid auth payload received.");
                return;
            }

            globalScope.__fbAuth.uid = data.uid;
            globalScope.__fbAuth.idToken = data.idToken;
            globalScope.__fbAuth.displayName = data.displayName || "Player";
            globalScope.__fbAuth.projectId = data.projectId || globalScope.__fbAuth.projectId || "";
            sendAuthToUnity();
        }

        globalScope.__applyFirebaseAuth = applyAuthPayload;

        if (!globalScope.__firebaseBridgeInit) {
            globalScope.__firebaseBridgeInit = true;
            globalScope.addEventListener("message", function (event) {
                var data = event.data;
                if (!data) {
                    return;
                }

                if (typeof data === "string") {
                    try {
                        data = JSON.parse(data);
                    } catch (error) {
                        console.warn("[FirebaseBridge] Ignoring non-JSON message payload.");
                        return;
                    }
                }

                if (data.type !== "firebase-auth") {
                    return;
                }

                applyAuthPayload(data);

                if (globalScope.parent && globalScope.parent !== globalScope) {
                    globalScope.parent.postMessage({ type: "firebase-auth-ack" }, "*");
                }
            });

            console.log("[FirebaseBridge] Listener registered and ready.");
        }

        if (globalScope.firebaseAuth && typeof globalScope.firebaseAuth === "object") {
            applyAuthPayload(globalScope.firebaseAuth);
            return;
        }

        if (globalScope.__fbAuth.uid && globalScope.__fbAuth.idToken) {
            sendAuthToUnity();
        }
    },

    SubmitScoreToFirestore: function (jsonBodyPtr) {
        var globalScope = typeof window !== "undefined" ? window : null;
        if (!globalScope) {
            console.warn("[FirebaseBridge] window is unavailable.");
            return;
        }

        var rawJson = UTF8ToString(jsonBodyPtr);
        var payload;
        try {
            payload = JSON.parse(rawJson);
        } catch (e) {
            console.error("[FirebaseBridge] Invalid score payload JSON.", e);
            return;
        }

        var auth = globalScope.__fbAuth || {};
        if (!auth.uid) {
            console.warn("[FirebaseBridge] No authenticated user. Score not sent.");
            return;
        }

        var now = new Date().toISOString();
        var durationMs = (payload.duration || 0) * 1000;
        var endTime = payload.sessionEndIso || now;
        var startTime = payload.sessionStartIso || new Date(Date.now() - durationMs).toISOString();
        var sessionId = payload.sessionId || (auth.uid + "-" + Date.now());

        var message = {
            type: "game-over",
            userId: auth.uid,
            score: payload.score || 0,
            pipesPassed: payload.pipes || 0,
            clicks: payload.flaps || 0,
            durationSeconds: payload.duration || 0,
            sessionId: sessionId,
            startTime: startTime,
            endTime: endTime
        };

        if (globalScope.parent && globalScope.parent !== globalScope) {
            globalScope.parent.postMessage(message, "*");
            console.log("[FirebaseBridge] Game telemetry sent to portal.");
        } else {
            console.warn("[FirebaseBridge] No parent window. Game must run inside the portal iframe.");
        }
    }
});
