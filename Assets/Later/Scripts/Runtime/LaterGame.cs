using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Later.LaterData;

namespace Later
{
    // Boot, scaling, input, title screen, the main loop, pausing, fullscreen. Ported from web/later/src/main.js.
    [RequireComponent(typeof(UIDocument))]
    public sealed class LaterGame : MonoBehaviour
    {
        public static LaterGame Instance;
        // tests pretend the screen is this size (pixels)
        public static Vector2Int? ScreenOverride;

        public string State { get; private set; } = "title";
        public Round Round { get; private set; } = RoundSim.Create();
        public Ending Ending { get; private set; }
        public double WorldW { get; private set; } = W;
        public VisualElement Root { get; private set; }
        // like the web version's __later.setTarget: steers the moon over the pointer and keys
        public Vec? ForcedTarget;
        // tests pass this much wall-clock time in the next frame (as if the tab had been hidden that long)
        public static double? WallSecondsForTests;

        // a browser only clicks for a finger that moved less than this (CSS px) between press and lift
        const double TapSlop = 10;

        readonly DragInput drag = new DragInput();
        readonly KeyState keys = new KeyState();
        readonly System.Collections.Generic.Dictionary<int, Vector2> pressedAt = new System.Collections.Generic.Dictionary<int, Vector2>();
        Sound sound;
        PlayView view;
        VisualElement frame, stage, world, title, fs;
        VisualElement[] centred;
        double scale = 1;
        Vector2 screen;
        Browser.Frame viewport;
        double lastTap = -1, lastWall = -1;
        Vector2? mouseAt; // the mouse, for the cursor
        string cursor;
        Rect fsReported = new Rect(-1, -1, 0, 0), closeReported = new Rect(-1, -1, 0, 0);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnityEngine.Cursor.visible = true;
        }

        void Start()
        {
            sound = GetComponent<Sound>();
            Root = GetComponent<UIDocument>().rootVisualElement;
            frame = Root.Q("frame"); stage = Root.Q("stage"); world = Root.Q("world");
            title = Root.Q("title"); fs = Root.Q("fs");
            centred = new[] { Root.Q("centre"), Root.Q("centre-top") };
            view = new PlayView(world, sound);
            // the phone's text at its exact half-pixel sizes, as the browser sets it (UI Toolkit rounds font sizes)
            foreach (var label in Root.Q("lockbody").Query<Label>().ToList()) ExactSize.Keep(label);

            if (Browser.CoarsePointer) title.Q<Label>("go").text = "TAP TO START";

            // Mouse: the moon follows the pointer over the stage. Touch: the moon moves with one finger from wherever it
            // lands, anywhere on the screen (thumbs rest in the margins beside the stage on phones), so a thumb never covers it.
            frame.RegisterCallback<PointerDownEvent>(e => { fs.RemoveFromClassList("hovered"); PointerDown(e.pointerId, e.pointerType == UnityEngine.UIElements.PointerType.touch, e.position); });
            frame.RegisterCallback<PointerMoveEvent>(e => PointerMove(e.pointerId, e.pointerType == UnityEngine.UIElements.PointerType.touch, e.position));
            frame.RegisterCallback<PointerUpEvent>(e => PointerUp(e.pointerId));
            frame.RegisterCallback<PointerCancelEvent>(e => PointerUp(e.pointerId));
            world.RegisterCallback<ClickEvent>(e => StageClick(e.pointerId, e.pointerType == UnityEngine.UIElements.PointerType.touch, e.position));

            // Fullscreen, where the browser supports it: the page toggles it itself on a real click (later.js), so here a
            // press on the button is just not a press on the game. On a touch screen it keeps its hover look, as a
            // browser's :hover sticks after a tap.
            fs.RegisterCallback<PointerDownEvent>(e => { e.StopPropagation(); fs.RemoveFromClassList("hovered"); });
            fs.RegisterCallback<PointerUpEvent>(e => { if (e.pointerType == UnityEngine.UIElements.PointerType.touch) fs.AddToClassList("hovered"); });
            fs.RegisterCallback<ClickEvent>(e => e.StopPropagation());

            view.Reset();
            Fit(true);
            ShowChrome();
            if (SmokeMode.Wanted) gameObject.AddComponent<SmokeMode>();
        }

        public void StartRound()
        {
            Ending?.Dispose();
            Ending = null;
            sound.Unlock();
            sound.RingStart(); // the phone rings softly under play
            Fit(true);
            // each name's pill as wide as the page draws it (the defaults until its font has loaded), as main.js does
            Round = RoundSim.Create(Browser.PillWidths(DEFAULT_PILL_W.Keys) ?? DEFAULT_PILL_W, WorldW);
            view.Reset();
            drag.Reset(); ForcedTarget = null;
            State = "play";
            ShowChrome();
        }

        void EndRound()
        {
            State = "over";
            sound.Silence();
            ShowChrome();
            Ending = new Ending(Round, world, sound, StartRound);
        }

        // the title, the fullscreen button and the cursor follow the state
        void ShowChrome()
        {
            Browser.SetPlaying(State == "play");
            title.EnableInClassList("on", State == "title");
            fs.EnableInClassList("ontitle", State == "title");
            fs.EnableInClassList("hide", State == "play" || !Browser.CanFullscreen);
        }

        // the cursor, as the web's CSS sets it: a hand over what can be clicked, hidden only over the stage during play
        void UpdateCursor()
        {
            string want = "";
            if (mouseAt is Vector2 p)
            {
                if (State == "play" && stage.worldBound.Contains(p)) want = "none";
                else
                    for (var e = Root.panel.Pick(p); e != null; e = e.parent)
                        if (e.ClassListContains("clicky")) { if (!e.ClassListContains("tried")) want = "pointer"; break; }
            }
            if (want != cursor) { cursor = want; Browser.SetCursor(want); }
        }

        // the page clicks and focuses FULLSCREEN and Show less itself, so it needs to know where they are
        void ReportButtons()
        {
            Rect fsAt = Css(fs), closeAt = Css(Root.Q("sheetClose"));
            if (fsAt != fsReported || closeAt != closeReported) { fsReported = fsAt; closeReported = closeAt; Browser.SetButtons(fsAt, closeAt); }
            string focus = Browser.Focused;
            fs.EnableInClassList("focused", focus == "fs");
            Root.Q("sheetClose").EnableInClassList("focused", focus == "close");
        }

        // where an element is drawn, in CSS px, or nothing when it doesn't show
        static Rect Css(VisualElement el)
        {
            for (var e = el; e != null; e = e.parent)
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden) return Rect.zero;
            var r = el.worldBound;
            float k = (float)Browser.PixelRatio;
            return new Rect(r.x / k, r.y / k, r.width / k, r.height / k);
        }

        public void PointerDown(int id, bool touch, Vector2 panelPos)
        {
            // double-tap (or double-click) during the ending shows the rest of her words at once; timed by the browser
            double now = Browser.PointerTime ?? Time.unscaledTimeAsDouble;
            if (State == "over" && Ending != null && now - lastTap < 0.35) Ending.TapTwice();
            lastTap = now;
            pressedAt[id] = panelPos;
            if (touch) drag.TouchDown(id, ToWorld(panelPos), Round);
        }

        public void PointerMove(int id, bool touch, Vector2 panelPos)
        {
            if (touch) drag.TouchMove(id, ToWorld(panelPos), Round);
            else
            {
                mouseAt = panelPos;
                if (stage.worldBound.Contains(panelPos)) drag.MouseMove(ToWorld(panelPos));
            }
        }

        public void PointerUp(int id) => drag.TouchUp(id);

        // a click on the stage starts the round from the title; for a finger, only a tap counts, as in a browser
        public void StageClick(int id, bool touch, Vector2 panelPos)
        {
            if (State != "title") return;
            if (touch && pressedAt.TryGetValue(id, out var at) && (panelPos - at).magnitude / Browser.PixelRatio > TapSlop) return;
            StartRound();
        }

        // a finger down when the game loses focus (another app, the notification shade) never sends its lift
        void OnApplicationFocus(bool focused)
        {
            if (!focused) drag.CancelTouch();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) drag.CancelTouch();
        }

        Vec ToWorld(Vector2 panelPos)
        {
            var p = world.WorldToLocal(panelPos);
            return new Vec(p.x, p.y);
        }

        // One world 540 tall, scaled to fit inside the safe area (notches, home bars), worked out in CSS px exactly as
        // main.js's fit() does. On wide screens (phones on their side) the world is wider (up to W_MAX) so play fills the
        // screen instead of leaving dead bars at the sides. Its width only changes between rounds.
        void Fit(bool widen)
        {
            screen = ScreenSize();
            viewport = Browser.ViewFrame(screen);
            double k = screen.x / viewport.W; // canvas px per CSS px
            double w = viewport.W - viewport.Left - viewport.Right, h = viewport.H - viewport.Top - viewport.Bottom;
            if (widen) WorldW = Rules.WorldWidth(w, h);
            double s = Math.Min(w / WorldW, h / H);
            scale = s * k;
            Layer.K = Layer.KFor(scale); // filtered things are drawn this much larger, to be sharp at this scale
            frame.style.width = screen.x; frame.style.height = screen.y;
            stage.style.left = (float)((viewport.Left + (w - WorldW * s) / 2) * k);
            stage.style.top = (float)((viewport.Top + (h - H * s) / 2) * k);
            stage.style.width = (float)(WorldW * scale); stage.style.height = (float)(H * scale);
            world.style.width = (float)WorldW;
            world.style.scale = new Scale(new Vector2((float)scale, (float)scale));
            foreach (var c in centred) c.style.left = (float)((WorldW - W) / 2);
            // landscape phones: the phone's text a touch larger (the web version's @media (max-height: 460px))
            frame.EnableInClassList("small", viewport.H <= 460);
        }

        static Vector2 ScreenSize() => ScreenOverride is Vector2Int o ? new Vector2(o.x, o.y) : new Vector2(Screen.width, Screen.height);

        // keys as main.js reads them; every keydown during play (repeats too) leaves the moon to the keys
        void ReadKeys()
        {
            foreach (var e in Browser.TakeKeyEvents())
            {
                if (e == "!") keys.Clear();
                else if (e == "@close") Ending?.CloseSheet(); // Enter or Space on a focused Show less
                else if (e[0] == '+') { if (keys.Down(e.Substring(1), State == "play")) drag.ClearTarget(); }
                else if (e[0] == '-') keys.Up(e.Substring(1));
            }
        }

        // the wall-clock time since the last frame (in tests, which fix each frame's time, that fixed time)
        double WallClockStep()
        {
            double now = Time.realtimeSinceStartupAsDouble, step = lastWall < 0 ? 0 : now - lastWall;
            lastWall = now;
            if (Time.captureDeltaTime > 0) step = Time.deltaTime;
            if (WallSecondsForTests is double forced) { step = forced; WallSecondsForTests = null; }
            return Math.Max(0, step);
        }

        void Update()
        {
            var size = ScreenSize();
            if (size != screen || !Browser.ViewFrame(size).Equals(viewport)) Fit(State == "title");
            // an upright phone pauses play; the page shows ROTATE YOUR DEVICE itself, as the web's CSS does
            bool upright = Browser.Upright;
            ReadKeys();

            // Two clocks, as on the web. The round steps by the frame's time, capped at 0.1 s, and not at all on the first
            // frame after the tab comes back (main.js restarts its clock then). Everything else (the ending's timers, the
            // view's animations, as setTimeout and CSS run) goes by the wall clock, time away included.
            double wall = WallClockStep();
            double dt = Browser.TakeResumed() ? 0 : Math.Min(0.1, Math.Max(0, Time.deltaTime));
            bool ended = false;
            // an upright phone pauses play (a hidden tab pauses everything: the browser stops sending frames)
            if (State == "play" && !upright)
            {
                var events = RoundSim.Step(Round, dt, new RoundInput { Target = ForcedTarget ?? drag.Target, KeysX = keys.X, KeysY = keys.Y });
                view.Handle(events, Round);
                // once every call has come and gone, the phone stops ringing: the last seconds are quiet
                if (sound.Ringing && Round.NextCall == CALLS.Length && Round.Pills.Count == 0) sound.RingStop();
                if (events.Any(e => e.Type == RoundEventType.Time)) { EndRound(); ended = true; }
            }
            view.Tick(wall);
            // the ending's clock starts from the frame the round ended in, as the web's timers do
            if (!ended) Ending?.Update(wall);
            if (State != "over") view.Draw(Round);
            UpdateCursor();
            ReportButtons();
        }
    }
}
