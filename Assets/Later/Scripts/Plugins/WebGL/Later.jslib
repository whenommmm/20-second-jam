// What the game asks the page: the same checks web/later/src/main.js makes, and what the template's later.js collects.
mergeInto(LibraryManager.library, {
  LaterCanFullscreen: function () {
    var root = document.documentElement;
    return root.requestFullscreen || root.webkitRequestFullscreen ? 1 : 0;
  },
  LaterCoarsePointer: function () { return matchMedia("(pointer: coarse)").matches ? 1 : 0; },
  LaterUpright: function () { return matchMedia("(orientation: portrait) and (max-width: 900px)").matches ? 1 : 0; },
  LaterPixelRatio: function () { return window.devicePixelRatio || 1; },
  LaterSetTitle: function (title) { document.title = UTF8ToString(title); },

  // the key events later.js queued since the last call, one per line ("+key", "-key", "!" for lost focus)
  LaterTakeKeys: function () {
    var page = window.LaterPage;
    if (!page || !page.keys.length) return 0;
    var s = page.keys.join("\n");
    page.keys.length = 0;
    var n = lengthBytesUTF8(s) + 1, buffer = _malloc(n);
    stringToUTF8(s, buffer, n);
    return buffer;
  },
  LaterSetPlaying: function (playing) { if (window.LaterPage) window.LaterPage.playing = !!playing; },
  // the time of the latest press, on the page's clock (ms)
  LaterPointerTime: function () { return window.LaterPage ? window.LaterPage.lastDown : 0; },
  // the cursor over the game: "pointer" over buttons, "none" over the stage in play, "" otherwise
  LaterSetCursor: function (cursor) { Module.canvas.style.cursor = UTF8ToString(cursor); },
  // where FULLSCREEN and Show less are (CSS px; zero size when hidden): later.js clicks and focuses them itself
  LaterSetButtons: function (fx, fy, fw, fh, cx, cy, cw, ch) {
    if (!window.LaterPage) return;
    window.LaterPage.buttons = { fs: fw > 0 ? { x: fx, y: fy, w: fw, h: fh } : null, close: cw > 0 ? { x: cx, y: cy, w: cw, h: ch } : null };
  },
  LaterFocused: function () { var f = window.LaterPage && window.LaterPage.focus; return f === "close" ? 1 : f === "fs" ? 2 : 0; },
  // the viewport and its safe-area insets (CSS px), measured as main.js's fit() measures #frame; read once a frame
  LaterReadFrame: function () { if (window.LaterPage) window.LaterPage.readFrame(); },
  LaterFrame: function (i) { return window.LaterPage ? window.LaterPage.frame[i] : 0; },
  // the paper colour, for the page's own ROTATE YOUR DEVICE (var(--paper), as on the web)
  LaterSetPaper: function (rgb) { document.documentElement.style.setProperty("--paper", UTF8ToString(rgb)); },
  // a name's pill width in the page's own font, or 0 until the font has loaded
  LaterPillWidth: function (who) { return window.LaterPage ? window.LaterPage.pillWidth(UTF8ToString(who)) : 0; }
});
