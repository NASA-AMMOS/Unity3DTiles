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

  //memory functions based on Unity-WebGL-Utilities (MIT license)
  //https://github.com/kongregate/Unity-WebGL-Utilities/blob/master/Assets/Plugins/WebGLMemoryStats/WebGLMemoryStats.jslib
  getTotalMemorySize: function() {
    return TOTAL_MEMORY;
  },

  getTotalStackSize: function() {
    return TOTAL_STACK;
  },
  
  getStaticMemorySize: function() {
    return STATICTOP - STATIC_BASE;
  },

  getDynamicMemorySize: function() {
    if (typeof DYNAMICTOP !== "undefined") {
      return DYNAMICTOP - DYNAMIC_BASE;
    } else {
      return HEAP32[DYNAMICTOP_PTR >> 2] - DYNAMIC_BASE;
    }
  },
});
