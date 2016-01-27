'use strict';

var scribe = scribe || angular.module('Scribe', ['ngSanitize']);

scribe.directive('ngValidatedClick', function () {

	'use strict';

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