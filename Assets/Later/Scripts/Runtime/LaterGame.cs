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

        // a browser only clicks for a finger that moved less than this (CSS px) between press and lift
        const double TapSlop = 10;

        readonly DragInput drag = new DragInput();
        readonly KeyState keys = new KeyState();
        readonly System.Collections.Generic.Dictionary<int, Vector2> pressedAt = new System.Collections.Generic.Dictionary<int, Vector2>();
        Sound sound;
        PlayView view;
        VisualElement frame, stage, world, title, fs, rotate;
        VisualElement[] centred;
        Label rotateText;
        double scale = 1;
        Vector2 screen;
        double lastTap = -1;
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
            title = Root.Q("title"); fs = Root.Q("fs"); rotate = Root.Q("rotate"); rotateText = rotate.Q<Label>();
            centred = new[] { Root.Q("centre"), Root.Q("centre-top") };
            view = new PlayView(world, sound);

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
            Round = RoundSim.Create(DEFAULT_PILL_W, WorldW);
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

        // One world 540 tall, scaled to fit. On wide screens (phones on their side) the world is wider (up to W_MAX)
        // so play fills the screen instead of leaving dead bars at the sides. Its width only changes between rounds.
        void Fit(bool widen)
        {
            screen = ScreenOverride is Vector2Int o ? new Vector2(o.x, o.y) : new Vector2(Screen.width, Screen.height);
            if (widen) WorldW = Rules.WorldWidth(screen.x, screen.y);
            scale = Math.Min(screen.x / WorldW, screen.y / H);
            frame.style.width = screen.x; frame.style.height = screen.y;
            float sw = (float)(WorldW * scale), sh = (float)(H * scale);
            stage.style.left = (screen.x - sw) / 2; stage.style.top = (screen.y - sh) / 2;
            stage.style.width = sw; stage.style.height = sh;
            world.style.width = (float)WorldW;
            world.style.scale = new Scale(new Vector2((float)scale, (float)scale));
            foreach (var c in centred) c.style.left = (float)((WorldW - W) / 2);
            // min(34px, 8vw), in CSS pixels
            // landscape phones: the phone's text a touch larger (the web version's @media (max-height: 460px))
            frame.EnableInClassList("small", screen.y / Browser.PixelRatio <= 460);
            double dpr = Browser.PixelRatio, size = Math.Min(34, 0.08 * screen.x / dpr) * dpr;
            rotateText.style.fontSize = (float)size;
            rotateText.text = $"<cspace={2 * dpr:0.##}px>ROTATE YOUR DEVICE</cspace>";
        }

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

        void Update()
        {
            var size = ScreenOverride is Vector2Int o ? new Vector2(o.x, o.y) : new Vector2(Screen.width, Screen.height);
            if (size != screen) Fit(State == "title");
            bool upright = Browser.Upright;
            rotate.EnableInClassList("on", upright);
            ReadKeys();

            double dt = Math.Min(0.1, Math.Max(0, Time.deltaTime));
            // an upright phone pauses play (a hidden tab already pauses: the game doesn't run in the background)
            if (State == "play" && !upright)
            {
                var events = RoundSim.Step(Round, dt, new RoundInput { Target = ForcedTarget ?? drag.Target, KeysX = keys.X, KeysY = keys.Y });
                view.Handle(events, Round);
                // once every call has come and gone, the phone stops ringing: the last seconds are quiet
                if (sound.Ringing && Round.NextCall == CALLS.Length && Round.Pills.Count == 0) sound.RingStop();
                if (events.Any(e => e.Type == RoundEventType.Time)) EndRound();
            }
            view.Tick(dt);
            Ending?.Update(dt);
            if (State != "over") view.Draw(Round);
            UpdateCursor();
            ReportButtons();
        }
    }
}
