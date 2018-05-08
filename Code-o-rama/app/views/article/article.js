angular.module('app').controller('ArticleController', function($scope, $location, $analytics, $routeParams) {
    $analytics.pageTrack($location.path());
    $scope.articleHtml = '/articles/' + $routeParams.id + '.html';
});