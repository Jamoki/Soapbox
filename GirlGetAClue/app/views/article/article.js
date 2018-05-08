angular.module('app').controller('MeetingController', function($scope, $location, $analytics, $routeParams, $q, $log, dataService) {
	$analytics.pageTrack($location.path());
});