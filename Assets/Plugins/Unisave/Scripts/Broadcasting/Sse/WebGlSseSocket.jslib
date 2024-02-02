mergeInto(LibraryManager.library, {
    JS_WebGlSseSocket_StartPoll: function(gameObjectName, url, jsonBody) {
        // abort if another SSE Poll is running
        if (window.unisaveSseSocketAbort) {
            window.unisaveSseSocketAbort();
            window.unisaveSseSocketAbort = undefined;
        }

        // convert C# strings to JS strings
        gameObjectName = UTF8ToString(gameObjectName);
        url = UTF8ToString(url);
        jsonBody = UTF8ToString(jsonBody);

        // convert C# callbacks to JS callbacks
        function chunkCallback(chunk) {
            SendMessage(gameObjectName, "JsCallback_OnChunk", chunk);
        }
        function doneCallback(error) {
            SendMessage(gameObjectName, "JsCallback_OnDone", error);
        }

        // poll-global variables
        var ourPollWasAborted = false;
        var reader = null;
        var decoder = new window.TextDecoder();

        // aborting logic
        var ourAborter = function() {
            ourPollWasAborted = true;
            if (reader !== null)
                reader.cancel();
        };
        window.unisaveSseSocketAbort = ourAborter;

        // send the request
        window.fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Accept": "text/event-stream"
            },
            body: jsonBody

        // === CONSUME RESPONSE ===
        }).then(function(response) {

            if (ourPollWasAborted) {
                return Promise.resolve(true);
            }

            if (!response.ok) {
                return Promise.reject(
                    new Error("HTTP status code: " + response.status)
                );
            }
    
            reader = response.body.getReader();
            
            function pump() {
                return reader.read().then(function(chunk) {
                    if (chunk.done) {
                        return Promise.resolve(true);
                    }
    
                    var textValue = decoder.decode(chunk.value);
                    chunkCallback(textValue);
                    
                    return pump();
                })
            }
    
            return pump();

        // === CLEAN UP ===
        }).then(function() {
            
            if (window.unisaveSseSocketAbort === ourAborter)
                window.unisaveSseSocketAbort = undefined;
            
            doneCallback("");

        // === ERROR ===
        }).catch(function(e) {
            doneCallback("" + e);
        });
    },

    JS_WebGlSseSocket_AbortPoll: function() {
        if (window.unisaveSseSocketAbort) {
            window.unisaveSseSocketAbort();
            window.unisaveSseSocketAbort = undefined;
        }
    },
});