angular.module('app.constants', [])
// @if CONFIG=='release'
    .constant('apiUrl', (function() {
        var base = "https://api.jamoki.com/soapbox/v1/";

        return {
            base: base,
            data: base + "data/",
            view: base + "view/",
            action: base + "action/"
        };
    })())
// @endif
// @if CONFIG=='debug'
    .constant('apiUrl', (function() {
        var base = "http://localhost:1360/";

        return {
            base: base,
            data: base + "data/",
            view: base + "view/",
            action: base + "action/"
        };
    })())
// @endif
