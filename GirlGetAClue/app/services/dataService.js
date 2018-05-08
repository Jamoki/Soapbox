angular.module('app.services').factory('dataService', function($q, $http, $log, apiUrl) {
    var dataService = {
        getArticleSummaries: function() {
            return $http.get(apiUrl.data + "articles", { 
                params: {
                    fields: "title(1),summary(1),imageUrl(1)",
                    limit: 100
                }
            });
        }
    };

    return dataService;
});

