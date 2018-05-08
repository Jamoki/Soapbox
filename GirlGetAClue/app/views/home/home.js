angular.module('app').controller('WelcomeController', function($scope, $location, $analytics) {
    $analytics.pageTrack($location.path());
});
