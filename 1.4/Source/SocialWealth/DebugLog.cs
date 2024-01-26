using System.Diagnostics;

namespace SocialWealth;

static class Log
{
    [Conditional("DEBUG")]
    public static void Message(string x)
    {
        Verse.Log.Message(x);
    }
}
