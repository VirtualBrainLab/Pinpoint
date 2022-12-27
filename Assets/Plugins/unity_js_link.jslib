mergeInto(LibraryManager.library, {

  Copy2Clipboard: function (str) {
    console.log('copy2clipboard called: ' + str);
	copy2clipboardStr = str;
	canvas.addEventListener('click', copy2clipboardCallback, false);
  }

});