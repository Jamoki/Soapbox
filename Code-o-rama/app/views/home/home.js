angular.module('app').controller('HomeController', function($scope, $location, $analytics, dataService) {
    $analytics.pageTrack($location.path());

    dataService.getArticleSummaries()
        .success(function(data) {
            $scope.articles = data.items;
        });

    $scope.data = [
      {key: "Xamarin", frequency: 90},
      {key: "ServiceStack", frequency: 99},
      {key: "Ubuntu", frequency: 20},
      {key: "Bootstrap", frequency: 48}
    ];

});
