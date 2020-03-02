mergeInto(LibraryManager.library, {

  //based on https://stackoverflow.com/a/901144
  getURLParameter: function(name) {
    name = Pointer_stringify(name);
    name = name.replace(/[\[\]]/g, '\\$&');
    const regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)');
    const results = regex.exec(window.location.href);
    const val = results && results[2] ? decodeURIComponent(results[2].replace(/\+/g, ' ')) : '';
    const sz = lengthBytesUTF8(val) + 1;
    const buf = _malloc(sz);
    stringToUTF8(val, buf, sz);
    return buf;
  },

  getWindowLocationURL: function() {
    const val = window.location.href;
    const sz = lengthBytesUTF8(val) + 1;
    const buf = _malloc(sz);
    stringToUTF8(val, buf, sz);
    return buf;
  },

});
