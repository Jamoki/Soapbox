"use strict";

angular.module('app.filters', []);
angular.module('app.services', ['app.constants']);
angular.module('app.directives', ['app.constants', 'app.services']);

angular.module('app', [
		'ngRoute', 
        'ngTouch',
        'ngSanitize',
		'app.services', 
        'app.constants',
        'app.directives',
		'app.version', 
        'app.filters',
		'angulartics', 
		'angulartics.google.analytics',
        'ui.bootstrap',
        'akoenig.deckgrid'
		])
	.config(function($routeProvider, $analyticsProvider, $locationProvider) {
		$analyticsProvider.virtualPageviews(false);
        $locationProvider.html5Mode(true);

		$routeProvider
            .when('/article/:id', {
                templateUrl: 'views/article/article.html',
                controller: 'ArticleController'
            })
            .when('/home', {
                templateUrl: 'views/home/home.html',
                controller: 'HomeController'
            })
			.otherwise({
				redirectTo: '/home'
			});
	})
    .run(function($rootScope) {
        // Try to keep this empty
    })
    .controller('AppController', function($scope, $location, appVersion) {
        $scope.version = appVersion;
    });
