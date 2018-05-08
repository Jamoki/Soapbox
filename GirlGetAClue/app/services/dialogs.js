angular.module('app.services').factory('dialogs', function ($modal) {
    return {
        confirmDelete: function (itemName) {
            return $modal.open({
                templateUrl: 'confirmDeleteDialog.html',
                controller: 'confirmDeleteController',
                resolve: {
                    itemName: function () {
                        return angular.copy(itemName);
                    }
                }
            });
        }
    };
});
