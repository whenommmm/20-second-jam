// The page around the Unity build: what web/later/src/main.js does with the browser directly, done here so the game
// behaves the same. Unity reads it through Assets/Later/Scripts/Plugins/WebGL/Later.jslib (window.LaterPage).
(function () {
  "use strict";
  var page = window.LaterPage = { keys: [], playing: false, lastDown: 0 };

  // ---- keys, as main.js reads them: by e.key, so the letters (not their positions) on any layout, and the number
  // pad's arrows. Unity never sees a key: its own handling would stop every key's default, where the web stops only
  // the arrows, and only during play.
  var STEER = { ArrowLeft: 1, a: 1, ArrowRight: 1, d: 1, ArrowUp: 1, w: 1, ArrowDown: 1, s: 1 };
  var name = function (e) { return e.key.length === 1 ? e.key.toLowerCase() : e.key; };
  addEventListener("keydown", function (e) {
    e.stopImmediatePropagation();
    if (!e.key) return;
    page.keys.push("+" + name(e));
    if (STEER[name(e)] && page.playing && e.key.indexOf("Arrow") === 0) e.preventDefault();
  }, true);
  addEventListener("keyup", function (e) {
    e.stopImmediatePropagation();
    if (e.key) page.keys.push("-" + name(e));
  }, true);
  addEventListener("keypress", function (e) { e.stopImmediatePropagation(); }, true);
  // unlike the web version, a key released while another window has focus doesn't stay held
  addEventListener("blur", function () { page.keys.push("!"); });

  // ---- presses: when the browser saw each one, for the double-tap's 350 ms
  addEventListener("pointerdown", function (e) { page.lastDown = e.timeStamp; }, true);
})();
