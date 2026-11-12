// The page around the Unity build: what web/later/src/main.js does with the browser directly, done here so the game
// behaves the same. Unity reads it through Assets/Later/Scripts/Plugins/WebGL/Later.jslib (window.LaterPage).
(function () {
  "use strict";
  var page = window.LaterPage = { keys: [], playing: false, lastDown: 0, buttons: { fs: null, close: null }, focus: "" };

  // ---- keys, as main.js reads them: by e.key, so the letters (not their positions) on any layout, and the number
  // pad's arrows. Unity never sees a key: its own handling would stop every key's default, where the web stops only
  // the arrows, and only during play.
  var STEER = { ArrowLeft: 1, a: 1, ArrowRight: 1, d: 1, ArrowUp: 1, w: 1, ArrowDown: 1, s: 1 };
  var name = function (e) { return e.key.length === 1 ? e.key.toLowerCase() : e.key; };
  addEventListener("keydown", function (e) {
    e.stopImmediatePropagation();
    if (!e.key) return;
    if (focusKeys(e)) return;
    page.keys.push("+" + name(e));
    if (STEER[name(e)] && page.playing && e.key.indexOf("Arrow") === 0) e.preventDefault();
  }, true);
  addEventListener("keyup", function (e) {
    e.stopImmediatePropagation();
    if (!e.key) return;
    if (e.key === " " && page.focus && page.buttons[page.focus]) activate(); // a focused button clicks on Space's release
    page.keys.push("-" + name(e));
  }, true);
  addEventListener("keypress", function (e) { e.stopImmediatePropagation(); }, true);
  // unlike the web version, a key released while another window has focus doesn't stay held
  addEventListener("blur", function () { page.keys.push("!"); });

  // ---- presses: when the browser saw each one, for the double-tap's 350 ms
  addEventListener("pointerdown", function (e) { page.lastDown = e.timeStamp; }, true);

  // ---- FULLSCREEN, a real button as on the web: it toggles on a click (press and lift on it, main button) or on
  // Enter/Space while it has focus, and makes the whole page fullscreen. The game says where it is (page.buttons).
  var root = document.documentElement;
  function toggleFullscreen() {
    if (document.fullscreenElement || document.webkitFullscreenElement) (document.exitFullscreen || document.webkitExitFullscreen).call(document);
    else if (root.requestFullscreen || root.webkitRequestFullscreen) (root.requestFullscreen || root.webkitRequestFullscreen).call(root);
  }
  function over(b, e) { return !!b && e.clientX >= b.x && e.clientX < b.x + b.w && e.clientY >= b.y && e.clientY < b.y + b.h; }
  var pressed = null;
  addEventListener("pointerdown", function (e) {
    pressed = e.button === 0 && over(page.buttons.fs, e) ? e.pointerId : null;
    page.focus = ""; // a click elsewhere takes focus off the buttons; a click on one doesn't give it focus either
  }, true);
  addEventListener("pointerup", function (e) {
    if (pressed !== null && pressed === e.pointerId && over(page.buttons.fs, e)) toggleFullscreen();
    pressed = null;
  }, true);

  // ---- keyboard focus: Tab moves between the buttons that show (Show less comes first in the web page's order),
  // then off them; Enter (on press) and Space (on release) activate the focused one
  function focusKeys(e) {
    var order = ["close", "fs"].filter(function (b) { return page.buttons[b]; });
    if (page.focus && !page.buttons[page.focus]) page.focus = ""; // a button that hides loses focus
    if (e.key === "Tab") {
      if (!order.length) return false;
      var stops = order.concat([""]), i = stops.indexOf(page.focus);
      page.focus = stops[(i + (e.shiftKey ? stops.length - 1 : 1)) % stops.length];
      if (page.focus) e.preventDefault();
      return true;
    }
    if (!page.focus) return false;
    if (e.key === "Enter") { activate(); return true; }
    if (e.key === " ") { e.preventDefault(); return true; }
    return false;
  }
  function activate() {
    if (page.focus === "fs") toggleFullscreen();
    else if (page.focus === "close") page.keys.push("@close");
  }
})();
