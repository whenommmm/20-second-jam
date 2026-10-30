namespace Later
{
    // One call of the month: when it starts, who, how many pills, how fast, the date it stamps, and whether it's Mom's last.
    public readonly struct CallDef
    {
        public readonly double T;
        public readonly string Who;
        public readonly int N;
        public readonly double V;
        public readonly string Date;
        public readonly bool Home;

        public CallDef(double t, string who, int n, double v, string date, bool home)
        {
            T = t; Who = who; N = n; V = v; Date = date; Home = home;
        }

        public override string ToString() => $"({T}, {Who}, {N}, {V}, {Date}, {Home})";
    }

    // A text in the Messages thread.
    public readonly struct Message
    {
        public readonly string Who, Date, Text;

        public Message(string who, string date, string text)
        {
            Who = who; Date = date; Text = text;
        }

        public override string ToString() => $"{Who} {Date}: {Text}";
    }
}
