angular.module('app').directive('d3Cloud', function($window, $timeout) {
	return {
		restrict: 'E',
		scope: {
			data: '=*',
			onClick: '&'
		},
		link: function(scope, ele, attrs) {
            var renderTimeout;
			var svg = d3.select(ele[0])
				.append('svg')
				.style('width', '100%');

			$window.onresize = function() {
				scope.$apply();
			};

			scope.$watch(function() {
				return angular.element($window)[0].innerWidth;
			}, function() {
				scope.render(scope.data);
			});

		    scope.$watch('data', function(newValue, oldValue) {
				if (newValue === oldValue || angular.isUndefined(newValue))
                    return;
                
                scope.render(newValue);
			});

            scope.render = function(data) {
				svg.selectAll('*').remove();

				if (!data) 
                    return;

                if (renderTimeout)
                    $timeout.cancel(renderTimeout);

                renderTimeout = $timeout(function() {
                    var width = svg.property('clientWidth'), 
                        height = svg.property('clientHeight');

                    wordScale = d3.scale.linear().domain([0,100]).range([10,60]);

                    d3.layout.cloud()
                        .size([width, height])
                        .timeInterval(10)
                        .words(data)
                        .text(function(d) { return d.key; })
                        .font("Raleway")
                        .fontSize(function(d) { 
                            return wordScale(d.frequency); 
                        })
                        .rotate(function(d) {
                            return 0;
                        })
                        .padding(1)
                        .on("end", draw)
                        .start();

                    function draw(words, bounds) {
                        var center = [width >> 1, height >> 1];
                        var wordG = svg.append("g")
                            .attr("id", "wordCloudG")
                            .attr("transform", "translate(" + center + ")");

                        wordG.append("rect")
                            .attr("x", -center[0])
                            .attr("y", -center[1])
                            .attr("width", "100%")
                            .attr("height", "100%")
                            .attr("fill", "#4A90E2");
                        wordG.selectAll("text")
                            .data(words)
                            .enter()
                            .append("text")
                            .style("fill", "white")
                            .style("font-size", function(d) { return d.size + "px"; })
                            .style("font-family", function(d) { return d.font; })
                            .style("opacity", .75)
                            .attr("text-anchor", "middle")
                            .attr("transform", function(d) {
                                return "translate(" + [d.x, d.y] + ")rotate(" + d.rotate + ")";
                            })
                            .text(function(d) { return d.text; });
                    }
                }, 200);
			};
		}}
    })
