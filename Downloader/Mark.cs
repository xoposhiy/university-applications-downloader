using System;
using System.Linq;

namespace Downloader
{
    public class Mark
    {
        public Mark(string type, int score)
        {
            Type = type;
            Score = score;
        }

        public string Type;
        public int Score;
        public static Mark NA = new Mark("", 0);

        public static Mark Parse(string value)
        {
            try
            {
                var parts = value.Split(new []{' ', '(', ')', ':'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= 1) return NA;
                var score = parts.Select(p => int.TryParse(p, out var v) ? v : -1).FirstOrDefault(v => v >= 0);
                var type = parts.FirstOrDefault(p => !int.TryParse(p, out _));
                return new Mark(type, score);
            }
            catch (Exception e)
            {
                throw new FormatException(value, e);
            }
        }
    }
}