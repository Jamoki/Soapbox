angular.module('app.services').factory('domHelper', function ($document) {
    return {    
        setFocusToFirstInvalid: function() {
            // Assumes ng-view defined as an element and not an attribute
            var views = $document[0].getElementsByTagName('ng-view');

            if (!views)
                return;

            var elements = views[0].querySelectorAll('.ng-invalid');

            if (!elements)
                return;

            var i;
            for (i = 0; i < elements.length; i++) {
                if (elements[i].tagName !== 'FORM') {
                    elements[i].focus();
                    return;
                }
            }
        }
    };
});

