mergeInto(LibraryManager.library, {

  Copy2Clipboard: function (str) {
	copy2clipboardStr = str;
	document.getElementById('unity-canvas').addEventListener('click', copy2clipboardCallback, false);
  }

});