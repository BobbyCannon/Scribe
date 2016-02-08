(function () {
	var fieldSelection = {
		getSelection: function () {
			var e = this.jquery ? this[0] : this;

			if ('selectionStart' in e) {
				var l = e.selectionEnd - e.selectionStart;
				return {
					start: e.selectionStart,
					end: e.selectionEnd,
					length: l,
					text: e.value.substr(e.selectionStart, l)
				};
			}

			if (document.selection) {
				e.focus();

				var r = document.selection.createRange();
				if (r == null) {
					return {
						start: 0,
						end: e.value.length,
						length: 0
					};
				}

				var re = e.createTextRange();
				var rc = re.duplicate();
				re.moveToBookmark(r.getBookmark());
				rc.setEndPoint('EndToStart', re);

				var rcLen = rc.text.length, i, rcLenOut = rcLen;
				for (i = 0; i < rcLen; i++) {
					if (rc.text.charCodeAt(i) === 13)
						rcLenOut--;
				}
				var rLen = r.text.length,
					rLenOut = rLen;
				for (i = 0; i < rLen; i++) {
					if (r.text.charCodeAt(i) === 13)
						rLenOut--;
				}

				return {
					start: rcLenOut,
					end: rcLenOut + rLenOut,
					length: rLenOut,
					text: r.text
				};
			}

			return {
				start: 0,
				end: e.value.length,
				length: 0
			};
		},

		setSelection: function (start, end) {
			var e = this[0];
			e.focus();

			if (e.setSelectionRange) {
				e.setSelectionRange(start, end);
			} else if (this.createTextRange) {
				var range = e.createTextRange();
				range.collapse(true);
				range.moveEnd('character', end);
				range.moveStart('character', start);
				range.select();
			} else if ('selectionStart' in e) {
				e.selectionStart = start;
				e.selectionEnd = end;
			}

			return this;
		},

		replaceSelection: function () {
			var e = this.jquery ? this[0] : this;
			var text = arguments[0] || '';

			if ('selectionStart' in e) {
				var start = e.value.substr(0, e.selectionStart);
				var end = e.value.substr(e.selectionEnd, e.value.length);
				e.value = start + text + end;
				return this;
			}

			if (document.selection) {
				e.focus();
				document.selection.createRange().text = text;
				return this;
			}

			e.value += text;
			return this;
		}
	};

	jQuery.each(fieldSelection, function (i) {
		jQuery.fn[i] = this;
	});

})();