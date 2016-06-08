'use strict';

var scribe = scribe || angular.module('Scribe', ['ngSanitize']);

scribe.directive('ngValidatedClick', function () {

	'use strict';

	Chart.defaults.global.elements.point.radius = 0;
	Chart.defaults.global.responsive = false;

	return {
		restrict: 'A',
		link: function (scope, element, attrs) {
			element.bind('click', function (event) {
				var message = attrs.ngValidationMessage;
				if (message && confirm(message)) {
					scope.$apply(attrs.ngValidatedClick);
				}

				if (event.stopPropagation) {
					event.stopPropagation();
				} else {
					event.cancelBubble = true;
				}
			});
		}
	};
});

String.prototype.insert = function (index, string) {
	if (index > 0)
		return this.substring(0, index) + string + this.substring(index, this.length);
	else
		return string + this;
};

scribe.getParameterByName = function(name, url) {
	if (!url) url = window.location.href;
	name = name.replace(/[\[\]]/g, '\\$&');
	var regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)');
	var results = regex.exec(url);
	if (!results) return '';
	if (!results[2]) return '';
	return decodeURIComponent(results[2].replace(/\+/g, ' '));
}