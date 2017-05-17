using System;
using System.Collections.Generic;

namespace Connect_4
{
    public class Pattern
    {
        public char Type; //Situation (s) OR Combination (c)
        public List<List<int>> Shape = new List<List<int>>();
        public int Severity;
        public int StateID;
        public int Effectiveness;
        public int PlayerID = 0;
        public List<int> SituationPlays;
        public bool Parallax = false;

        public Pattern(char c, int id, int s, int e) { Type = c; StateID = id; Severity = s; Effectiveness = e; }

        // Creates a new Pattern with '2's instead of '1's and changes the ID/Effectiveness accordingly.
        public Pattern Mirror
        {
            get
            {
                var shape = new List<List<int>>();
                for (var i = 0; i < Shape.Count; i++)
                {
                    var tmp = new List<int>();
                    for (var j = 0; j < Shape[0].Count; j++)
                    {
                        if (Shape[i][j] == 1)
                            tmp.Add(2);
                        else
                            tmp.Add(Shape[i][j]);
                    }
                    shape.Add(tmp);
                }
                return new Pattern(Type, StateID + 1, Severity, -Effectiveness) { Shape = shape, PlayerID = 1, Parallax = Parallax };
            }
        }

        // Searches the '_case' for the current pattern.
        public bool Compare(ref List<List<int>> _case)
        {
            switch (Type)
            {
                case 's': return Compare_s(ref _case);
                case 'c': return Compare_c(ref _case);
                default:
                    throw new NotImplementedException();
            }

        }

        // Situation Comparer.
        private bool Compare_s(ref List<List<int>> _case)
        {
            SituationPlays = new List<int>();
            for (var x = 1; x + Shape[0].Count <= 8; x++)
            {
                if (!Parallax)
                {
                    MirrorShape();
                    if (ComparePatern(x, 0, ref _case, ref Shape))
                    {
                        for (int i = 0; i < Shape[0].Count; i++)
                        {
                            if (Shape[0][i] == -2 && !SituationPlays.Contains(i + x))
                                SituationPlays.Add(i + x);
                        }
                        return true;
                    }
                    MirrorShape();
                }
                if (ComparePatern(x, 0, ref _case, ref Shape))
                {
                    for (int i = 0; i < Shape[0].Count; i++)
                    {
                        if (Shape[0][i] == -2 && !SituationPlays.Contains(i + x)) 
                            SituationPlays.Add(i + x);
                    }
                    return true;
                }
            }
            return false;
        }

        // Combination Comparer.
        private bool Compare_c(ref List<List<int>> _case)
        {
            var X = Shape[0].Count;
            var Y = Shape.Count;
            List<List<int>> Flip;
            if (!Parallax)
            {
                MirrorShape();
                for (var i = 0; i < 4; i++)
                {
                    Flip = FlipPatern(i);
                    for (var x = 1; x < 8; x++)
                    {
                        for (var y = 1; y < 7; y++)
                        {
                            if (ComparePatern(x, y, ref _case, ref Flip))
                                return true;
                        }
                    }
                }
                MirrorShape();
            }
            for (var i = 0; i < 4; i++)
            {
                Flip = FlipPatern(i);
                for (var x = 1; x < 8; x++)
                {
                    for (var y = 1; y < 7; y++)
                    {
                        if (ComparePatern(x, y, ref _case, ref Flip))
                            return true;
                    }
                }
            }
            return false;
        }

        // Compares a specific point with the pattern with all the rotaion and flips.
        private bool ComparePatern(int x, int y, ref List<List<int>> _case, ref List<List<int>> source)
        {
            var X = source[0].Count;
            var Y = source.Count;
            if (x + X <= 8 && y + Y <= 7)
            {
                for (var i = 0; i < X; i++)
                {
                    for (var j = 0; j < Y; j++)
                    {
                        if (source[j][i] != -1 && source[j][i] != -2 && source[j][i] != _case[y + j][x + i])
                            return false;
                    }
                }
                return true;
            }
            return false;
        }

        // Turns the current pattern x times.
        private List<List<int>> FlipPatern(int times, List<List<int>> source = null)
        {
            if (source == null) { source = Shape; }
            if (times == 0) return source;
            var output = new List<List<int>>();
            for (var x = 0; x < source[0].Count; x++)
            {
                var tmp = new List<int>();
                for (var y = source.Count - 1; y >= 0; y--)
                {
                    tmp.Add(source[y][x]);
                }
                output.Add(tmp);
            }
            if (times == 1) return output;
            return FlipPatern(times - 1, output);
        }

        // Flips the pattern vertically.
        private void MirrorShape()
        {
            for (var i = 0; i < Shape.Count; i++)
                Shape[i].Reverse();
        }
    }
}
