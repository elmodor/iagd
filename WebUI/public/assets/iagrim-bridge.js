(function () {
    "use strict";

    if (typeof invokeCSharpAction !== "function") {
        console.debug("IAGD native bridge unavailable.");
        return;
    }

    if (window.iagrim !== undefined) {
        console.debug('IAGD host already exists.');
        return;
    }

    const pending = new Map();

    window.__coreResponse = function (id, result, error) {
        const request = pending.get(id);

        if (!request) {
            console.warn('Received response for unknown request:', id);
            return;
        }
        pending.delete(id);
        if (error) {
            request.reject(new Error(error));
        } else {
            request.resolve(result);
        }
    };

    function notifyCSharp(method, ...args) {
        window.invokeCSharpAction(JSON.stringify({method, args}));
    }

    function callCSharp(method, ...args) {
        const id = crypto.randomUUID();
        return new Promise((resolve, reject) => {
            pending.set(id, { resolve, reject});
            window.invokeCSharpAction(JSON.stringify({ id, method, args }));
        });
    }

    window.iagrim = {
        SignalReady() {
            notifyCSharp('SignalReady');
        },

        RequestMoreItems() {
            notifyCSharp('RequestMoreItems');
        },

        RequestCollectionData() {
            notifyCSharp('RequestCollectionData');
        },

        SetClipboard(text) {
            notifyCSharp('SetClipboard', text);
        },

        OpenURL(url) {
            notifyCSharp('OpenURL', url);
        },

        DismissNumericFilterBanner() {
            notifyCSharp('DismissNumericFilterBanner');
        },

        TransferItem(url, transferAll) {
            return callCSharp('TransferItem', url, transferAll);
        },

        GetItemSetAssociations() {
            return callCSharp('GetItemSetAssociations');
        },

        GetBackedUpCharacters() {
            return callCSharp('GetBackedUpCharacters');
        },

        GetCharacterDownloadUrl(character) {
            return callCSharp('GetCharacterDownloadUrl', character);
        },

        GetTranslationStrings() {
            return callCSharp('GetTranslationStrings');
        }
    };
    console.log('IAGD Avalonia bridge installed');
})();
