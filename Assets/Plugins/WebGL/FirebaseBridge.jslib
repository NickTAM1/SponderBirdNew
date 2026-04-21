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
                moduleInstance.SendMessage("GameManager", "OnAuthReceived", payload);
                return;
            }

            if (typeof globalScope.SendMessage === "function") {
                globalScope.SendMessage("GameManager", "OnAuthReceived", payload);
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
        var parsedPayload;
        try {
            parsedPayload = JSON.parse(rawJson);
        } catch (error) {
            console.error("[FirebaseBridge] Score payload is invalid JSON.", error);
            return;
        }

        var auth = globalScope.__fbAuth || {};
        var projectId = auth.projectId || parsedPayload.projectId || "";
        if (!auth.uid || !auth.idToken || !projectId) {
            console.warn("[FirebaseBridge] Missing auth or projectId. Score not submitted.");
            return;
        }

        var baseUrl = "https://firestore.googleapis.com/v1/projects/" + encodeURIComponent(projectId) + "/databases/(default)/documents";
        var userDocUrl = baseUrl + "/users/" + encodeURIComponent(auth.uid);
        var headers = {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + auth.idToken
        };

        var scoreDoc = {
            fields: {
                userId: { stringValue: auth.uid },
                score: { integerValue: String(parsedPayload.score || 0) },
                pipes: { integerValue: String(parsedPayload.pipes || 0) },
                duration: { integerValue: String(parsedPayload.duration || 0) },
                timestamp: { timestampValue: new Date().toISOString() }
            }
        };

        fetch(baseUrl + "/scores", {
            method: "POST",
            headers: headers,
            body: JSON.stringify(scoreDoc)
        })
            .then(function (response) {
                if (!response.ok) {
                    return response.text().then(function (text) {
                        throw new Error("Score write failed (" + response.status + "): " + text);
                    });
                }
                return response.json();
            })
            .then(function (data) {
                console.log("[FirebaseBridge] Score saved:", data.name);
            })
            .catch(function (error) {
                console.error("[FirebaseBridge] Error saving score:", error);
            });

        fetch(userDocUrl, {
            method: "GET",
            headers: headers
        })
            .then(function (response) {
                if (response.status === 404) {
                    return null;
                }
                if (!response.ok) {
                    return response.text().then(function (text) {
                        throw new Error("User read failed (" + response.status + "): " + text);
                    });
                }
                return response.json();
            })
            .then(function (doc) {
                var currentHigh = 0;
                var currentGames = 0;

                if (doc && doc.fields) {
                    if (doc.fields.highscore && doc.fields.highscore.integerValue) {
                        currentHigh = parseInt(doc.fields.highscore.integerValue, 10) || 0;
                    }
                    if (doc.fields.games && doc.fields.games.integerValue) {
                        currentGames = parseInt(doc.fields.games.integerValue, 10) || 0;
                    }
                }

                var nextHigh = Math.max(currentHigh, parsedPayload.score || 0);
                var nextGames = currentGames + 1;
                var patchBody = {
                    fields: {
                        highscore: { integerValue: String(nextHigh) },
                        games: { integerValue: String(nextGames) }
                    }
                };

                return fetch(userDocUrl + "?updateMask.fieldPaths=highscore&updateMask.fieldPaths=games", {
                    method: "PATCH",
                    headers: headers,
                    body: JSON.stringify(patchBody)
                });
            })
            .then(function (response) {
                if (!response.ok) {
                    return response.text().then(function (text) {
                        throw new Error("User patch failed (" + response.status + "): " + text);
                    });
                }
                return response.json();
            })
            .then(function () {
                console.log("[FirebaseBridge] User stats updated.");
            })
            .catch(function (error) {
                console.error("[FirebaseBridge] User profile update failed:", error);
            });
    }
});

