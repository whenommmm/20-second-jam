// What the game asks the page: the same checks web/later/src/main.js makes.
mergeInto(LibraryManager.library, {
  LaterCanFullscreen: function () {
    var root = document.documentElement;
    return root.requestFullscreen || root.webkitRequestFullscreen ? 1 : 0;
  },
  LaterCoarsePointer: function () { return matchMedia("(pointer: coarse)").matches ? 1 : 0; },
  LaterUpright: function () { return matchMedia("(orientation: portrait) and (max-width: 900px)").matches ? 1 : 0; },
  LaterPixelRatio: function () { return window.devicePixelRatio || 1; },
  LaterSetTitle: function (title) { document.title = UTF8ToString(title); }
});
