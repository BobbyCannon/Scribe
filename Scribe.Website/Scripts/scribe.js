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

scribe.getQueryParams = function (queryString) {
	var query = (queryString || window.location.search).substring(1);
	if (!query) {
		return false;
	}

	return _.chain(query.split('&'))
		.map(function (params) {
			var p = params.split('=');
			return [p[0], decodeURIComponent(p[1])];
		})
		.object()
		.value();
};

scribe.getGravatarLink = function (emailAddress) {
	if (emailAddress === null || emailAddress === undefined) {
		emailAddress = '';
	}

	var md5 = scribe.md5.calculate(emailAddress.trim().toLowerCase()).toLowerCase();
	return 'https://www.gravatar.com/avatar/' + md5 + '?s=180';
};

scribe.checkPassword = function (password, minLength) {
	if (password === undefined || password === null) {
		password = '';
	}

	if (!minLength) {
		minLength = 6;
	}

	var score = 0;
	if (password.length >= minLength) {
		score = 50;
	}

	var extraCount = password.length - minLength;
	if (extraCount > 0) {
		score += 3 * extraCount;
	}

	var hasUpper = false,
		hasNumber = false,
		hasSymbol = false;

	for (var i = 0; i < password.length; i++) {
		if (!hasUpper && password[i].match(/[A-Z]/g)) {
			score += 6;
			hasUpper = true;
		}
		if (!hasNumber && password[i].match(/[0-9]/g)) {
			score += 6;
			hasNumber = true;
		}
		if (!hasSymbol && password[i].match(/(.*[!,@,#,$,%,^,&,*,?,_,~])/)) {
			score += 6;
			hasSymbol = true;
		}
	}

	if (hasUpper && hasNumber && hasSymbol) {
		score += 25;
	} else if ((hasUpper && hasNumber) || (hasUpper && hasSymbol) || (hasNumber && hasSymbol)) {
		score += 15;
	}

	if (password.match(/^[\sa-z]+$/)) {
		score -= 15;
	}

	if (password.match(/^[\s0-9]+$/)) {
		score -= 35;
	}

	var verdict = 'Unknown';
	var percent = 0;

	if (score < 50) {
		verdict = 'Weak';
		percent = 34;
	} else if (score >= 50 && score < 75) {
		verdict = 'Average';
		percent = 50;
	} else if (score >= 75 && score < 100) {
		verdict = 'Good';
		percent = 75;
	} else if (score >= 100) {
		verdict = 'Strong';
		percent = 100;
	}

	return {
		score: score,
		verdict: verdict,
		width: percent + '%',
		css: scribe.getPasswordCss(percent)
	};
};

scribe.getPasswordCss = function (percent) {
	if (percent <= 34) {
		return 'bar red';
	} else if (percent <= 50) {
		return 'bar orange';
	} else if (percent <= 75) {
		return 'bar blue';
	} else {
		return 'bar green';
	}
};

String.prototype.insert = function (index, string) {
	if (index > 0)
		return this.substring(0, index) + string + this.substring(index, this.length);
	else
		return string + this;
};

scribe.getParameterByName = function (name, url) {
	if (!url) {
		url = window.location.href;
	}

	name = name.replace(/[\[\]]/g, '\\$&');
	var regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)');
	var results = regex.exec(url);

	if (!results) {
		return '';
	}

	if (!results[2]) {
		return '';
	}

	return decodeURIComponent(results[2].replace(/\+/g, ' '));
};