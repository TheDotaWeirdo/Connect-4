using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Connect_4
{
    public class MainGame
    {
        public string Moves { get; private set; }
        public bool LeftIsRed = true;
        public bool Finished = true;
        public bool Busy = true;
        public bool Loading = false;
        public bool vsAI = true;
        public bool PredictiveAI = true;
        public bool StrategicAI = false;
        public bool HumanizedAI = false;
        public bool FastGame = false;
        private bool Calculating = false;
        private bool Lmode = false;
        public int Starter;
        public int[] P = { -1, -1 };
        public int _Turn, Undos;
        private int diff;
        public List<List<int>> Case = new List<List<int>>();
        private List<List<int>> _Case = new List<List<int>>();
        public int Delay { get; private set; }
        private int winner = 0;
        public int MoveCount { get { return Moves.Length - 4; } }
        private double WinChance, BlockChance, ThreatBlockChance, ThreatChance, PredictWinChance, PredictLossChance, StrategicCheckChance, StrategicBlockChance, FutureStrategicBlockChance, SacrificeChance;
        private int Positiveness = 0;
        private List<int> PlayerPositiveness = new List<int>();
        private Dictionary<int, int> Effectiveness = new Dictionary<int, int> { { -10, 1 }, { -9, -1 }, { -8, 3 }, { -7, -2 }, { -6, 2 }, { -5, 3 }, { -4, 1 }, { -3, -1 }, { -2, -2 }, { -1, -2 }, { 0, 0 }, { 1, -2 }, { 2, 0 }, { 3, 0 }, { 4, 2 }, { 5, 2 }, { 6, -1 }, { 7, -1 }, { 8, 1 }, { 9, 2 } };
        private Dictionary<int, List<int>> St = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
        private Dictionary<int, int> Se = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        public Dictionary<int, List<int>> PlayerState = new Dictionary<int, List<int>>();
        public Dictionary<int, int> PlayerSeverity = new Dictionary<int, int>();
        private Dictionary<int, List<int>> HumanState = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
        private Dictionary<int, int> HumanSeverity = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        public Dictionary<int, List<int>> AIState = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
        public Dictionary<int, int> AISeverity = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        // States of each column are cases where specified conditions are met, a column can have more than one state at a time
        // State Severity of a column is calculated based on the multiple states applied in a column, the higher the severity the more likely the column will be played

        //-10: FuturePredictiveCheck | checks if a play by the bot will create a Predicted Threat (8) state in another column
        // -9: FuturePredictiveBlock | checks if a play by the player will create a Predicted ThreatBlocked (7) state in another column
        // -8: Threat Sacrifice | sacrifice a Threat in order to force another one (win in 3 moves)
        // -7: Possible Loss Prediction | checks if a play by the Player will create a possible Loss (6) (Loss in 4 moves)
        // -6: Possible Win Prediction | checks if a play by the Bot will create a possible Win (5) (win in 2 moves)
        // -5: Block/Mistake Force Prediction | checks if a play by the Bot will force the player into surrendering a Threat Block or Mistake
        // -4: StrategicCheck | checks if a play by the bot will create a Threat state in another column
        // -3: StrategicBlock | checks if a play by the player will create a Threat Block state in another column
        // -2: FutureStrategicBlock | checks if a ThreatBlock will be created after 2 moves in the column
        // -1: Block/Mistake Cancel Prediction | checks if a play by the player will force the bot into surrendering a Threat Block or Mistake
        //  0: Free | no certain states for the column
        //  1: ThreatBlocked | case where playing in the column will result in a possible loss (loss in 2 moves)
        //  2: NearlyFull | case where the column is 1 token away from being full
        //  3: Full | case where the whole column is full
        //  4: Threat | case where playing in the column will result in a loss of a possible win
        //  5: Possible Win | (Win in 1 move)
        //  6: Possible Loss | (Loss in 1 move)
        //  7: Predicted ThreatBlocked | checks if a ThreatBlock will occur after one play in the column
        //  8: Predicted Threat | checks if a Threat will occur after one play in the column
        //  9: Relevant Future Threat Block | checks if there is any 3+ layer threat possibility that is relevant by odd/even standards
        //10: Relevant Future Threat | checks if there is any 3+ layer threat possibility that is relevant by odd/even standards

        public int Diff
        {
            get { return diff; }
            set {
                diff = value;
                Delay = (100 - value) * ((FastGame) ? 4 : 12);
                WinChance = (value * .1) + 90;
                BlockChance = (value * .2) + 80;
                ThreatBlockChance = (value * .6) + 40;
                ThreatChance = (value * .7) + 30;
                PredictWinChance = (value * .5) + 50;
                PredictLossChance = (value * .6) + 40;
                StrategicCheckChance = (value * .6) + 40;
                StrategicBlockChance = (value * .7) + 30;
                FutureStrategicBlockChance = (value * .8) + 20;
                SacrificeChance = (value * .7) + 30;
                Undos = 2 * (int)Math.Floor((50 - value) / 25d + 1);
            }
        }
        public int Winner { get
            {
                if (winner != 0) return winner;
                int output = 0;
                List<Point> LP = new List<Point>();
                for (int y = 6; y > 0; y--)
                {
                    for (int x = 1; x < 8; x++)
                    {
                        CheckData CD;
                        if (y < 4 && x < 5)
                        {
                            CD = CheckDiagDown(ref Case, x, y, true);
                            if (CD.Check)
                            {
                                output = CD.Color;
                                for (int i = 0; i < CD.Points.Count; i++)
                                    LP.Add(CD.Points[i]);
                            }
                        }
                        if (y > 3 && x < 5)
                        {
                            CD = CheckDiagUp(ref Case, x, y, true);
                            if (CD.Check)
                            {
                                output = CD.Color;
                                for (int i = 0; i < CD.Points.Count; i++)
                                    LP.Add(CD.Points[i]);
                            }
                        }
                        if (x < 5)
                        {
                            CD = CheckHorizontal(ref Case, x, y, true);
                            if (CD.Check)
                            {
                                output = CD.Color;
                                for (int i = 0; i < CD.Points.Count; i++)
                                    LP.Add(CD.Points[i]);
                            }
                        }
                        if (y < 4)
                        {
                            CD = CheckVertical(ref Case, x, y, true);
                            if (CD.Check)
                            {
                                output = CD.Color;
                                for (int i = 0; i < CD.Points.Count; i++)
                                    LP.Add(CD.Points[i]);
                            }
                        }
                    }
                }
                if (output > 0)
                {
                    Finished = true;
                    for (int y = 1; y < 7; y++)
                        for (int x = 1; x < 8; x++)
                            Case[y][x] = -Case[y][x];
                    foreach (Point P in LP)
                        Case[P.Y][P.X] = output + 2;
                }                
                return winner = output;
            } }
        public bool Tied { get
            {
                for (int x = 1; x < 8; x++)
                    if (Case[1][x] == 0)
                        return false;
                for (int y = 1; y < 7; y++)
                    for (int x = 1; x < 8; x++)
                        Case[y][x] = -Case[y][x];
                return true;
            } }
        public bool LearnMode
        {
            get { return Lmode && vsAI; }
            set { Lmode = value; }
        }

        private Dictionary<string, Pattern> Patterns = new Dictionary<string, Pattern>();

        class Pattern
        {
            public char Type; //Situation (s) OR Combination (c)
            public List<List<int>> Shape = new List<List<int>>();
            public int Severity;
            public int StateID;
            public int Effectiveness;
            public int PlayerID = 0;

            public Pattern(char c, int id, int s, int e) { Type = c; StateID = id; Severity = s; Effectiveness = e; }

            public Pattern Mirror
            {
                get
                {
                    List<List<int>> shape = new List<List<int>>();
                    for (int i = 0; i < Shape.Count; i++)
                    {
                        List<int> tmp = new List<int>();
                        for (int j = 0; j < Shape[0].Count; j++)
                        {
                            if (Shape[i][j] == 1)
                                tmp.Add(2);
                            else
                                tmp.Add(Shape[i][j]);
                        }
                        shape.Add(tmp);
                    }
                    return new Pattern(Type, StateID + 1, Severity, -Effectiveness) { Shape = shape, PlayerID = 1 };
                }
            }

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

            private bool Compare_s(ref List<List<int>> _case)
            {
                for (int x = 1; x < 8; x++)
                {
                    for (int y = 1; y < 7; y++)
                    {
                        if (Shape[y][x] != -1 && Shape[y][x] != _case[y][x])
                            return false;
                    }
                }
                return true;
            }

            private bool Compare_c(ref List<List<int>> _case)
            {
                int X = Shape[0].Count, Y = Shape.Count;
                List<List<int>> Flip;
                MirrorShape();
                for (int i = 0; i < 4; i++)
                {
                    Flip = FlipPatern(i);
                    for (int x = 1; x < 8; x++)
                    {
                        for (int y = 1; y < 7; y++)
                        {
                            if (ComparePatern(x, y, ref _case, ref Flip))
                                return true;
                        }
                    }
                }
                MirrorShape();
                for (int i = 0; i < 4; i++)
                {
                    Flip = FlipPatern(i);
                    for (int x = 1; x < 8; x++)
                    {
                        for (int y = 1; y < 7; y++)
                        {
                            if (ComparePatern(x, y, ref _case, ref Flip))
                                return true;
                        }
                    }
                }
                return false;
            }

            private bool ComparePatern(int x, int y, ref List<List<int>> _case, ref List<List<int>> source)
            {
                int X = source[0].Count, Y = source.Count;
                if (x + X <= 8 && y + Y <= 7)
                {
                    for (int i = 0; i < X; i++)
                    {
                        for (int j = 0; j < Y; j++)
                        {
                            if (source[j][i] != -1 && source[j][i] != _case[y + j][x + i])
                                return false;
                        }
                    }
                    return true;
                }
                return false;
            }

            private List<List<int>> FlipPatern(int times, List<List<int>> source = null)
            {
                if (source == null) { source = Shape; }
                if (times == 0) return source;
                var output = new List<List<int>>();
                for (int x = 0; x < source[0].Count; x++)
                {
                    List<int> tmp = new List<int>();
                    for (int y = source.Count - 1; y >= 0; y--)
                    {
                        tmp.Add(source[y][x]);
                    }
                    output.Add(tmp);
                }
                if (times == 1) return output;
                return FlipPatern(times - 1, output);
            }

            private void MirrorShape()
            {
                for (int i = 0; i < Shape.Count; i++)
                    Shape[i].Reverse();
            }

            private int GetLow(int col, ref List<List<int>> _case)
            {
                for (int i = 0; i < 6; i++)
                    if (_case[i][col] > 0)
                        return i;
                return 0;
            }
        }

        class CheckData
        {
            public bool Check;
            public int Color;
            public List<Point> Points = new List<Point>();

            public CheckData(bool b, int col = 0, List<Point> P = null)
            {
                Check = b; Color = col; Points = P;
            }
        }

        public MainGame()
        {
            _Turn = (new Random().NextDouble() < .5) ? 0 : 1;
            Moves = "";
            Diff = 50;
            ResetPlayerStSe();
            // Case initialization
            Case.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
            Case.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
            Case.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
            Case.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
            Case.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
            Case.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
            Case.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
            // Pattern initialization
            // 2 - Token Diagonal
            Patterns.Add("2TDiag1", new Pattern('c', 12, 1, -1));
            Patterns["2TDiag1"].Shape.Add(new List<int> { -1, -1, -1, 0 });
            Patterns["2TDiag1"].Shape.Add(new List<int> { -1, 1, 0, -1 });
            Patterns["2TDiag1"].Shape.Add(new List<int> { -1, 1, -1, -1 });
            Patterns["2TDiag1"].Shape.Add(new List<int> { 1, -1, -1, -1 });
            Patterns.Add("2TDiag2", Patterns["2TDiag1"].Mirror);
            // 2 - Token Diagonal
            Patterns.Add("2TDiag1_2", new Pattern('c', 12, 1, -1));
            Patterns["2TDiag1_2"].Shape.Add(new List<int> { -1, -1, -1, 0 });
            Patterns["2TDiag1_2"].Shape.Add(new List<int> { -1, 1, 1, -1 });
            Patterns["2TDiag1_2"].Shape.Add(new List<int> { -1, 0, -1, -1 });
            Patterns["2TDiag1_2"].Shape.Add(new List<int> { 1, -1, -1, -1 });
            Patterns.Add("2TDiag2_2", Patterns["2TDiag1_2"].Mirror);
            // 2 - Token Diagonal
            Patterns.Add("2TDiag1_3", new Pattern('c', 12, 1, -1));
            Patterns["2TDiag1_3"].Shape.Add(new List<int> { -1, -1, -1, 0 });
            Patterns["2TDiag1_3"].Shape.Add(new List<int> { -1, 1, 1, -1 });
            Patterns["2TDiag1_3"].Shape.Add(new List<int> { -1, 1, -1, -1 });
            Patterns["2TDiag1_3"].Shape.Add(new List<int> { 0, -1, -1, -1 });
            Patterns.Add("2TDiag2_3", Patterns["2TDiag1_3"].Mirror);
            // 2 - Token Straight
            Patterns.Add("2TStr1", new Pattern('c', 12, 1, -1));
            Patterns["2TStr1"].Shape.Add(new List<int> { 1, 1, 0, 0 });
            Patterns.Add("2TStr2", Patterns["2TStr1"].Mirror);
            // Seven
            Patterns.Add("Seven1", new Pattern('c', 20, 4, -3));
            Patterns["Seven1"].Shape.Add(new List<int> { 1, -1, 1, -1 });
            Patterns["Seven1"].Shape.Add(new List<int> { -1, 1, 1, -1 });
            Patterns["Seven1"].Shape.Add(new List<int> { -1, -1, 1, -1 });
            Patterns["Seven1"].Shape.Add(new List<int> { -1, -1, 0, 0 });
            Patterns.Add("Seven2", Patterns["Seven1"].Mirror);
            // Arrow
            Patterns.Add("Arrow1", new Pattern('c', 22, 2, -3));
            Patterns["Arrow1"].Shape.Add(new List<int> { 0, 0, -1, -1 });
            Patterns["Arrow1"].Shape.Add(new List<int> { 0, 1, 1, 1 });
            Patterns["Arrow1"].Shape.Add(new List<int> { -1, 1, 1, -1 });
            Patterns["Arrow1"].Shape.Add(new List<int> { -1, 1, -1, 1 });
            Patterns.Add("Arrow2", Patterns["Arrow1"].Mirror);
            // L
            Patterns.Add("L1", new Pattern('c', 24, 3, -2));
            Patterns["L1"].Shape.Add(new List<int> { -1, 0, -1, -1 });
            Patterns["L1"].Shape.Add(new List<int> { 0, 1, 1, 1 });
            Patterns["L1"].Shape.Add(new List<int> { -1, 1, -1, -1 });
            Patterns["L1"].Shape.Add(new List<int> { -1, 1, -1, -1 });
            Patterns.Add("L2", Patterns["L1"].Mirror);
            // Diamond
            Patterns.Add("Diamond1", new Pattern('c', 26, 5, -3));
            Patterns["Diamond1"].Shape.Add(new List<int> { -1, 0, -1, 0, -1 });
            Patterns["Diamond1"].Shape.Add(new List<int> { 0, -1, 1, -1, 0 });
            Patterns["Diamond1"].Shape.Add(new List<int> { -1, 1, -1, 1, -1 });
            Patterns["Diamond1"].Shape.Add(new List<int> { -1, -1, 1, -1, -1 });
            Patterns.Add("Diamond2", Patterns["Diamond1"].Mirror);
            // Square
            Patterns.Add("Square1", new Pattern('c', 28, 2, -1));
            Patterns["Square1"].Shape.Add(new List<int> { -1, -1, -1, -1 });
            Patterns["Square1"].Shape.Add(new List<int> { -1, 1, 1, -1 });
            Patterns["Square1"].Shape.Add(new List<int> { -1, 1, 1, -1 });
            Patterns.Add("Square2", Patterns["Square1"].Mirror);
            // T
            Patterns.Add("T1", new Pattern('c', 30, 2, -1));
            Patterns["T1"].Shape.Add(new List<int> { -1, -1, 0, -1, -1 });
            Patterns["T1"].Shape.Add(new List<int> { 0, 1, 1, 1, 0 });
            Patterns["T1"].Shape.Add(new List<int> { -1, -1, 1, -1, -1 });
            Patterns["T1"].Shape.Add(new List<int> { -1, -1, 1, -1, -1 });
            Patterns.Add("T2", Patterns["T1"].Mirror);
            // Z
            Patterns.Add("Z1", new Pattern('c', 32, 2, -1));
            Patterns["Z1"].Shape.Add(new List<int> { 0, -1, -1, -1 });
            Patterns["Z1"].Shape.Add(new List<int> { 0, 1, 1, 0 });
            Patterns["Z1"].Shape.Add(new List<int> { -1, -1, 1, -1 });
            Patterns["Z1"].Shape.Add(new List<int> { -1, -1, 1, 1 });
            Patterns.Add("Z2", Patterns["Z1"].Mirror);
        }

        public void StartMoveCounting()
        {
            if(diff < 10)
                Moves = "00" + diff.ToString() + _Turn.ToString();
            else if(diff < 100)
                Moves = "0" + diff.ToString() + _Turn.ToString();
            else
                Moves = "100" + _Turn.ToString();
        }

        public bool Btwn(int Value, int Min, int Max, bool StrictCheck = false)
        {
            if (StrictCheck)
                return (Value > Min && Value < Max);
            return (Value >= Min && Value <= Max);
        }

        public Point Undo()
        {
            if (MoveCount > 0 && Undos > 0)
            {
                int t = int.Parse(Moves.Last().ToString());
                Point p = new Point(t, GetLow(t, ref Case) + 1);
                Case[p.Y][p.X] = 0;
                Moves = Moves.Substring(0, Moves.Length - 1);
                Undos--;
                return p;
            }
            throw new InvalidOperationException();
        }

        public void Play(int index)
        {
            Moves += index.ToString();
            Case[GetLow(index, ref Case)][index] = _Turn + 1;
        }

        public void Turn()
        {
            _Turn = (_Turn == 0) ? 1 : 0;
        }

        public int PlayAI()
        {
            // Calculate the State & Severity of each column
            St = AIState; Se = AISeverity; Positiveness = 0;
            CaseCopy(ref _Case, ref Case);
            CalculateStates();
            AISeverity[4] = (int)((AISeverity[4] + 1) * 1.5);
            // Run the Humanization function
            if (HumanizedAI)
            {
                for (int i = 1; i < 8; i++)
                    AISeverity[i] *= 2;
                Humanize();
            }
            // Waits for any ongoing animations to be done
            while (Busy) { Thread.Sleep(1); }
            if (Finished)
                return 0; // In case the game was declared finished while calculations were ongoing
            // Chooses a column based on the severity of all of them
            // It will choose a random column from the highest severity 
            Se = AISeverity;
            return ChooseColumn();
        }

        public void CalculateStates()
        {
            while (Calculating) { Thread.Sleep(1); }
            Calculating = true;
            Random RND = new Random(Guid.NewGuid().GetHashCode());
            // Loops through all columns to calculate the states and severity
            for (int x = 1; x < 8; x++)
            {
                // Resets the column's old values
                Se[x] = 0;
                St[x] = new List<int> { 0 };
                int y = GetLow(x, ref _Case);
                // If the Column is full, state = 3
                if (y == 0)
                { St[x].Add(3); Se[x] = -100; Positiveness += Effectiveness[3]; }
                else
                {
                    // Checks for possible wins
                    _Case[y][x] = P[1] + 1;
                    if (CheckWin(ref _Case) == P[1] + 1 && RND.NextDouble() * 100 <= WinChance)
                    {
                        St[x].Add(5);
                        Se[x] += 150;
                        Positiveness += Effectiveness[5];
                    }
                    else
                    {
                        // Checks for possible losses
                        _Case[y][x] = P[0] + 1;
                        if (CheckWin(ref _Case) == P[0] + 1 && RND.NextDouble() * 100 <= BlockChance)
                        {
                            St[x].Add(6);
                            Se[x] += 30;
                            Positiveness += Effectiveness[6];
                        }
                    }
                    _Case[y][x] = 0;
                    // In case the column is 1-token short from being full
                    if (y == 1)
                    { St[x].Add(2); Se[x] -= 1; Positiveness += Effectiveness[2]; }
                    else
                    {
                        // In case playing in the column will result in an open possible loss
                        if (RND.NextDouble() * 100 <= ThreatBlockChance && CheckThreat(x, P[0], 1))
                        { St[x].Add(1); Se[x] -= 13; Positiveness += Effectiveness[1]; }
                        // In case playing in the column will result in a loss of a possible win
                        if (RND.NextDouble() * 100 <= ThreatChance && CheckThreat(x, P[1], 1))
                        { St[x].Add(4); Se[x] -= 4 - ThreatUsefulness(P[1], x, 1); Positiveness += Effectiveness[4]; }
                    }
                }
            }

            for (int x = 1; x < 8; x++)
            {
                int y = GetLow(x, ref _Case);
                // Predictive AI calculations
                if (PredictiveAI)
                {
                    // In case playing the column will result in a possible win state
                    int temp;
                    _Case[y][x] = P[1] + 1;
                    if (RND.NextDouble() * 100 <= PredictWinChance && (temp = CountPossWin(x)) > 0)
                    {  St[x].Add(-6); Se[x] += (int) Math.Floor(1.4 * (temp + 1)); Positiveness += Effectiveness[-6]; }
                    // In case the Player playing the column will result in a possible loss state
                    _Case[y][x] = P[0] + 1;
                    if (RND.NextDouble() * 100 <= PredictLossChance && (temp = CountPossLoss(x)) > 0)
                    {  St[x].Add(-7); Se[x] += (int) Math.Floor(1.4 * (temp + 1)); Positiveness += Effectiveness[-7]; }
                    _Case[y][x] = 0;

                    if (y - 2 > 0)
                    {
                        // In case playing in the column will result in an open possible loss
                        if (RND.NextDouble() * 100 <= ThreatBlockChance && CheckThreat(x, P[0], 2))
                        { St[x].Add(7); Se[x] -= ThreatUsefulness(P[0], x, 2); Positiveness += Effectiveness[7]; }
                        // In case playing in the column will result in a loss of a possible win
                        if (RND.NextDouble() * 100 <= ThreatChance && CheckThreat(x, P[1], 2))
                        { St[x].Add(8); Se[x] += 2 + ThreatUsefulness(P[1], x, 2); Positiveness += Effectiveness[8]; }
                    }

                    // Checks if after 2 plays, a ThreatBlock will be created(1)
                    if (RND.NextDouble() * 100 <= FutureStrategicBlockChance && CheckThreat(x, P[0], 3))
                    { St[x].Add(-2); Se[x] -= 1; Positiveness += Effectiveness[2]; }
                }
                // Strategic AI calculations
                if (StrategicAI)
                {
                    // Checks if playing in the column will create a Threat state (4)
                    if (RND.NextDouble() * 100 <= StrategicCheckChance && StrategicCheck(x, 1, 1))
                    { St[x].Add(-4); Se[x] += 2; Positiveness += Effectiveness[-4]; }
                    // Checks if the Player playing in the column will create a Threat Block state (1)
                    if (RND.NextDouble() * 100 <= StrategicBlockChance && StrategicCheck(x, 0, 1))
                    { St[x].Add(-3); Se[x] += 2; Positiveness += Effectiveness[-3]; }
                    // Checks if a column with Threat will still have a Threat after one move in it
                    if (RND.NextDouble() * 100 <= SacrificeChance && CheckThreat(x, P[1], 1) && !CheckThreat(x, P[0], 1) && CheckThreat(x, P[1], 2))// && St[x].Contains(4) && !St[x].Contains(1) && SacrificeCheck(x))
                    { St[x].Add(-8); Se[x] += 7; Positiveness += Effectiveness[-8]; }
                    // Checks if playing in the column will create a Predicted Threat state (8)
                    if (RND.NextDouble() * 100 <= StrategicCheckChance && StrategicCheck(x, 1, 2))
                    { St[x].Add(-10); Se[x] += 1; Positiveness += Effectiveness[-10]; }
                    // Checks if the Player playing in the column will create a Predicted Threat Block state (7)
                    if (RND.NextDouble() * 100 <= StrategicBlockChance && StrategicCheck(x, 0, 2))
                    { St[x].Add(-9); Se[x] += 1; Positiveness += Effectiveness[-9]; }
                }

                if(HumanizedAI)
                {
                    for (int i = 3; i < 7; i++)
                    {
                        if (StrategicCheck(x, 0, i))
                        { St[x].Add(9); Se[x] += 2; Positiveness += Effectiveness[9]; }
                        if (StrategicCheck(x, 1, i))
                        { St[x].Add(10); Se[x] += 2; Positiveness += Effectiveness[9]; }
                    }
                }
            }

            if(HumanizedAI)
                Humanize3();

            Calculating = false;
        }

        // Checks if every move possible is beneficial to the AI or not
        public void Humanize()
        {
            ResetPlayerStSe();
            CaseCopy(ref _Case, ref Case);
            int p0 = P[0], p1 = P[1];
            P[1] = p0; P[0] = p1;
            St = PlayerState; Se = PlayerSeverity;
            PlayerPositiveness.Add(0);
            for (int x = 1; x < 8; x++)
            {
                if (!IsFull(x, ref _Case))
                {
                    int y = GetLow(x, ref _Case);
                    _Case[y][x] = P[0] + 1;
                    Positiveness = 0;
                    CalculateStates();
                    _Case[y][x] = 0;
                    PlayerPositiveness.Add(-Positiveness);
                }
                else
                    PlayerPositiveness.Add(0);
            }
            for (int j = 1; j < 8; j++)
                if (!IsFull(j, ref _Case))
                    AISeverity[j] += (int)Math.Round(PlayerPositiveness[j] / 5d);
            P[1] = p1; P[0] = p0;
            Humanize2();
        }

        // Checks for win/loss outcomes within the next 6 moves
        public void Humanize2()
        {
            int p0 = P[0], p1 = P[1];
            int TotMoves;
            for (int i = 1; i < 8; i++)
            {
                if (IsFull(i, ref Case)) continue;
                List<List<int>> SaveCase = new List<List<int>>();
                CaseCopy(ref SaveCase, ref Case);
                SaveCase[GetLow(i, ref SaveCase)][i] = P[1] + 1;
                int Winner = 0;
                TotMoves = 0;
                var turn = 0;
                while ((Winner = CheckWin(ref SaveCase)) == 0 && TotMoves <= 5) 
                {
                    TotMoves++;
                    St = HumanState; Se = HumanSeverity;
                    if (turn == 0)
                    { P[0] = p1; P[1] = p0; }
                    else
                    { P[0] = p0; P[1] = p1; }
                    CalculateStates();
                    if (St.All(x => x.Value.Contains(3))) break;
                    int temp = ChooseColumn();
                    SaveCase[GetLow(temp, ref SaveCase)][temp] = P[1] + 1;
                    turn = (turn == 0) ? 1 : 0;
                }
                P[0] = p0; P[1] = p1;
                if (TotMoves > 0)
                {
                    if (Winner == P[0] + 1)
                        AISeverity[i] -= 100 / (int)Math.Pow(TotMoves + 1, .55) - 30;
                    else if (Winner == P[1] + 1)
                        AISeverity[i] += 100 / (int)Math.Pow(TotMoves + 1, .55) - 30;
                }
            }
            P[0] = p0; P[1] = p1;
        }

        // Checks for patterns and situations
        private void Humanize3()
        {
            CaseCopy(ref _Case, ref Case);
            for (int x = 1; x < 8; x++)
            {
                int y = GetLow(x, ref _Case);
                _Case[y][x] = P[0] + 1;
                foreach (Pattern P in Patterns.Values.Where(p => p.PlayerID == P[0] && p.Type == 'c'))
                {
                    if (P.Compare(ref _Case))
                    {
                        _Case[y][x] = 0;
                        if (!P.Compare(ref _Case))
                        {
                            St[x].Add(P.StateID);
                            Se[x] += P.Severity;
                            if(LeftIsRed)
                                Positiveness += P.Effectiveness;
                            else
                                Positiveness -= P.Effectiveness;
                        }
                        _Case[y][x] = this.P[0] + 1;
                    }
                }
                _Case[y][x] = P[1] + 1;
                foreach (Pattern P in Patterns.Values.Where(p => p.PlayerID == P[1] && p.Type == 'c'))
                {
                    if (P.Compare(ref _Case))
                    {
                        _Case[y][x] = 0;
                        if (!P.Compare(ref _Case))
                        {
                            St[x].Add(P.StateID);
                            Se[x] += P.Severity;
                            if (LeftIsRed)
                                Positiveness += P.Effectiveness;
                            else
                                Positiveness -= P.Effectiveness;
                        }
                         _Case[y][x] = this.P[1] + 1;
                    }
                }
                _Case[y][x] = 0;
                foreach (Pattern P in Patterns.Values.Where(p => p.Type == 's'))
                { }
            }
        }
        
        private void CaseCopy(ref List<List<int>> To, ref List<List<int>> From)
        {
            To = new List<List<int>>();
            for (int i = 0; i < 7; i++)
            {
                To.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
                for (int j = 0; j < 8; j++)
                {
                    To[i][j] = From[i][j];
                }
            }
        }

        public int CheckWin(ref List<List<int>> checkCase)
        {
            for (int y = 6; y > 0; y--)
            {
                for (int x = 1; x < 8; x++)
                {
                    CheckData CD;
                    if (y < 4 && x < 5)
                    {
                        CD = CheckDiagDown(ref checkCase, x, y, true);
                        if (CD.Check)
                            return CD.Color;
                    }
                    if (y > 3 && x < 5)
                    {
                        CD = CheckDiagUp(ref checkCase, x, y, true);
                        if (CD.Check)
                            return CD.Color;
                    }
                    if (x < 5)
                    {
                        CD = CheckHorizontal(ref checkCase, x, y, true);
                        if (CD.Check)
                            return CD.Color;
                    }
                    if (y < 4)
                    {
                        CD = CheckVertical(ref checkCase, x, y, true);
                        if (CD.Check)
                            return CD.Color; 
                    }
                }
            }
            return 0;
        }

        public void AsignHelps()
        {
            int p0 = P[0], p1 = P[1], d = diff;
            bool b1 = PredictiveAI, b2 = StrategicAI;
            P[0] = p1; P[1] = p0;
            ResetPlayerStSe();
            St = PlayerState; Se = PlayerSeverity;
            Diff = 100;
            StrategicAI = PredictiveAI = true;
            CaseCopy(ref _Case, ref Case);
            CalculateStates();
            PredictiveAI = b1; StrategicAI = b2;
            Diff = d;
            P[0] = p0; P[1] = p1;
        }

        public int GetLow(int col, ref List<List<int>> _case)
        {
            for (int i = 6; i > 0; i--)
                if (_case[i][col] == 0)
                    return i;
            return 0;
        }

        public bool IsFull(int col, ref List<List<int>> _case)
        {
            return _case[1][col] != 0;
        }

        private int CountPossLoss(int n)
        {
            int output = 0;
            for (int x = 1; x < 8; x++)
            {
                int y;
                //In _Case of Possible Win or Loss
                if ((y = GetLow(x, ref _Case)) > 0 && x != n)
                {
                    _Case[y][x] = P[0] + 1;
                    if (CheckWin(ref _Case) == P[0] + 1)
                    {
                        output++;
                        if (StrategicAI && (St[x].Contains(1) || St[x].Contains(4)))
                        { St[n].Add(-1); Se[n] += 2; }
                    }
                    _Case[y][x] = 0;
                }
            }
            if(output > 0 && _Case[1][n] == 0)
            {
                _Case[GetLow(n, ref _Case)][n] = P[0] + 1;
                if (CheckWin(ref _Case) == P[0] + 1)
                {
                    output++;
                    if (StrategicAI && (St[n].Contains(1) || St[n].Contains(4)))
                    { St[n].Add(-1); Se[n] += 2; }
                }
                _Case[GetLow(n, ref _Case)][n] = 0;
            }
            return (int)Math.Pow(output, 2) ;
        }

        private int CountPossWin(int n)
        {
            int output = 0;
            for (int x = 1; x < 8; x++)
            {
                int y;
                //In _Case of Possible Win or Loss
                if ((y = GetLow(x, ref _Case)) > 0 && x != n)
                {
                    _Case[y][x] = P[1] + 1;
                    if (CheckWin(ref _Case) == P[1] + 1)
                    {
                        output++;
                        if (StrategicAI && (St[x].Contains(1) || St[x].Contains(4)))
                        { St[n].Add(-5); Se[n] += 3; Positiveness += 2; }
                        for (int i = 1; i < 8; i++)
                        {
                            if (!IsFull(i, ref _Case))
                            {
                                int y2 = GetLow(i, ref _Case);
                                _Case[y2][i] = P[0] + 1;
                                if (CheckWin(ref _Case) == P[0] + 1)
                                {
                                    if (St[n].Contains(-5))
                                        St[n].Remove(-5);
                                    Se[x] -= 3;
                                    Positiveness -= 2;
                                }
                                _Case[y2][i] = 0;
                            }
                        }
                    }
                    _Case[y][x] = 0;
                }
            }
            if (output > 0 && _Case[1][n] == 0)
            {
                _Case[GetLow(n, ref _Case)][n] = P[1] + 1;
                if (CheckWin(ref _Case) == P[0] + 1)
                {
                    output++;
                    if (StrategicAI && (St[n].Contains(1) || St[n].Contains(4)))
                    { St[n].Add(-5); Se[n] += 3; }
                }
                _Case[GetLow(n, ref _Case)][n] = 0;
            }
            return (int)Math.Pow(output, 2);
        }

        private int ChooseColumn()
        {
            int Max = Se.Values.Max();
            var d = Se.Where(x => x.Value == Max);
            return d.ElementAt(new Random().Next(d.AsEnumerable().Count())).Key;
        }

        private bool StrategicCheck(int x, int ID, int Severity)
        {
            int y = GetLow(x, ref _Case);
            _Case[y][x] = P[ID] + 1;
            for (int i = 1; i < 8; i++)
            {
                if(x != i)
                {
                    if (Severity == 1)
                    {
                        if (!St[i].Contains((ID == 0) ? 1 : 4) && CheckThreat(i, P[ID], Severity))
                        { _Case[y][x] = 0; return true; }
                    }
                    else if (Severity == 2)
                    {
                        if (!St[i].Contains((ID == 0) ? 7 : 8) && CheckThreat(i, P[ID], Severity))
                        { _Case[y][x] = 0; return true; }
                    }
                    else if (CheckThreat(i, P[ID], Severity) && ThreatUsefulness(P[ID], i, Severity) == 2)
                    {
                        _Case[y][x] = 0;
                        if (CheckThreat(i, P[ID], Severity))
                            _Case[y][x] = P[ID] + 1;
                        else
                            return true;
                    }
                }
            }
            _Case[y][x] = 0;
            return false;
        }
        
        private void ResetPlayerStSe()
        {
            PlayerSeverity = new Dictionary<int, int>();
            PlayerState = new Dictionary<int, List<int>>();
            PlayerPositiveness = new List<int>();
        }

        private int ThreatUsefulness(int Pl, int Column, int Layers)
        {
            if (!StrategicAI) return 0;
            int y = GetLow(Column, ref _Case) - Layers;
            if ((y % 2 == 0) == (Starter == Pl))
                return 2;
            return -2;
        }

        public bool CheckThreat(int Column, int Winner, int Layers)
        {
            int y = GetLow(Column, ref _Case), t = Winner;
            bool b = true;
            if (y - Layers <= 0) return false;
            if (Layers % 2 == 1) t = (t == 1) ? 0 : 1;
            for (int i = y; i >= y - Layers; i--)
            {
                _Case[i][Column] = t + 1;
                t = (t == 1) ? 0 : 1;
                if (i != y - Layers && CheckWin(ref _Case) != 0)
                    b = false;
            }
            b = b && CheckWin(ref _Case) == Winner + 1;
            for (int i = y - Layers; i <= y; i++)
                _Case[i][Column] = 0;
            return b;
        }

        CheckData CheckHorizontal(ref List<List<int>> checkCase, int x, int y, bool win = false)
        {
            if (win)
            {
                if (Btwn(x + 3, 1, 7)) 
                {
                    if (checkCase[y][x] != 0 && checkCase[y][x] == checkCase[y][x + 1] && checkCase[y][x] == checkCase[y][x + 2] && checkCase[y][x] == checkCase[y][x + 3])
                        return new CheckData(true, checkCase[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y), new Point(x + 2, y), new Point(x + 3, y) });
                }
                else { return new CheckData(false); }
            }
            else
            {
                if (Btwn(y + 1, 1, 6)) 
                {
                    if (checkCase[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                if (Btwn(x + 3, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y][x + 2] != 0 && checkCase[y][x + 2] == checkCase[y][x + 1] && checkCase[y][x + 1] == checkCase[y][x + 3])
                        return new CheckData(true, checkCase[y][x+1]);
                }
                if (Btwn(x + 2, 1, 7) && Btwn(x - 1, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y][x - 1] != 0 && checkCase[y][x - 1] == checkCase[y][x + 1] && checkCase[y][x - 1] == checkCase[y][x + 2])
                        return new CheckData(true, checkCase[y][x + 1]);
                }
                if (Btwn(x + 1, 1, 7) && Btwn(x - 2, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y][x - 2] != 0 && checkCase[y][x - 2] == checkCase[y][x - 1] && checkCase[y][x - 1] == checkCase[y][x + 1])
                        return new CheckData(true, checkCase[y][x + 1]);
                }
                if (Btwn(x - 3, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y][x - 3] != 0 && checkCase[y][x - 3] == checkCase[y][x - 2] && checkCase[y][x - 2] == checkCase[y][x - 1])
                        return new CheckData(true, checkCase[y][x - 1]);
                }
            }
            return new CheckData(false);
        }

        CheckData CheckVertical(ref List<List<int>> checkCase, int x, int y, bool win = false)
        {
            if (win)
            {
                if (Btwn(y + 3, 1, 6))
                {
                    if (checkCase[y][x] != 0 && checkCase[y][x] == checkCase[y + 1][x] && checkCase[y][x] == checkCase[y + 2][x] && checkCase[y][x] == checkCase[y + 3][x])
                        return new CheckData(true, checkCase[y][x], new List<Point> { new Point(x, y), new Point(x, y + 1), new Point(x, y + 2), new Point(x, y + 3) });
                }
                else { return new CheckData(false); }
            }
            else
            {
                if (Btwn(y + 1, 1, 6))
                {
                    if (checkCase[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                if (Btwn(y + 3, 1, 6))
                {
                    if (checkCase[y][x] == 0 && checkCase[y + 1][x] != 0 && checkCase[y + 1][x] == checkCase[y+2][x] && checkCase[y+1][x] == checkCase[y+3][x])
                        return new CheckData(true, checkCase[y+1][x]);
                }
            }
            return new CheckData(false);
        }

        CheckData CheckDiagDown(ref List<List<int>> checkCase, int x, int y, bool win = false)
        {
            if (win)
            {
                if (Btwn(y + 3, 1, 6) && Btwn(x + 3, 1, 7))
                {
                    if (checkCase[y][x] != 0 && checkCase[y][x] == checkCase[y+1][x + 1] && checkCase[y][x] == checkCase[y+2][x + 2] && checkCase[y][x] == checkCase[y+3][x + 3])
                        return new CheckData(true, checkCase[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y + 1), new Point(x + 2, y + 2), new Point(x + 3, y + 3) });
                }
                else { return new CheckData(false); }
            }
            else
            {
                if (Btwn(y + 1, 1, 6))
                {
                    if (checkCase[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                if (Btwn(y + 3, 1, 6) && Btwn(x + 3, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y + 2][x + 2] != 0 && checkCase[y + 2][x + 2] == checkCase[y + 1][x + 1] && checkCase[y + 2][x + 2] == checkCase[y + 3][x + 3])
                        return new CheckData(true, checkCase[y + 1][x + 1]);
                }
                if (Btwn(y + 2, 1, 6) && Btwn(x + 2, 1, 7) && Btwn(y - 1, 1, 6) && Btwn(x - 1, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y - 1][x - 1] != 0 && checkCase[y - 1][x - 1] == checkCase[y + 1][x + 1] && checkCase[y - 1][x - 1] == checkCase[y + 2][x + 2])
                        return new CheckData(true, checkCase[y + 1][x + 1]);
                }
                if (Btwn(y + 1, 1, 6) && Btwn(x + 1, 1, 7) && Btwn(y - 1, 1, 6) && Btwn(x - 1, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y - 2][x - 2] != 0 && checkCase[y - 2][x - 2] == checkCase[y - 1][x - 1] && checkCase[y - 1][x - 1] == checkCase[y + 1][x + 1])
                        return new CheckData(true, checkCase[y + 1][x + 1]);
                }
                if (Btwn(y - 3, 1, 6) && Btwn(x - 3, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y - 3][x - 3] != 0 && checkCase[y - 3][x - 3] == checkCase[y - 2][x - 2] && checkCase[y - 2][x - 2] == checkCase[y - 1][x - 1])
                        return new CheckData(true, checkCase[y - 1][x - 1]);
                }
            }
            return new CheckData(false);
        }

        CheckData CheckDiagUp(ref List<List<int>> checkCase, int x, int y, bool win = false)
        {
            if (win)
            {
                if (Btwn(y - 3, 1, 6) && Btwn(x + 3, 1, 7))
                {
                    if (checkCase[y][x] != 0 && checkCase[y][x] == checkCase[y - 1][x + 1] && checkCase[y][x] == checkCase[y - 2][x + 2] && checkCase[y][x] == checkCase[y - 3][x + 3]) 
                        return new CheckData(true, checkCase[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y - 1), new Point(x + 2, y - 2), new Point(x + 3, y - 3) });
                }
                else { return new CheckData(false); }
            }
            else
            {
                if (Btwn(y + 1, 1, 6))
                {
                    if (checkCase[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                if (Btwn(y - 3, 1, 6) && Btwn(x + 3, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y - 2][x + 2] != 0 && checkCase[y - 2][x + 2] == checkCase[y - 1][x + 1] && checkCase[y - 1][x + 1] == checkCase[y - 3][x + 3])
                        return new CheckData(true, checkCase[y - 1][x + 1]);
                }
                if (Btwn(y - 2, 1, 6) && Btwn(x + 2, 1, 7) && Btwn(y + 1, 1, 6) && Btwn(x - 1, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y + 1][x - 1] != 0 && checkCase[y + 1][x - 1] == checkCase[y - 1][x + 1] && checkCase[y + 1][x - 1] == checkCase[y - 2][x + 2])
                        return new CheckData(true, checkCase[y - 1][x + 1]);
                }
                if (Btwn(y - 1, 1, 6) && Btwn(x + 1, 1, 7) && Btwn(y + 2, 1, 6) && Btwn(x - 2, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y + 2][x - 2] != 0 && checkCase[y + 2][x - 2] == checkCase[y + 1][x - 1] && checkCase[y + 1][x - 1] == checkCase[y - 1][x + 1])
                        return new CheckData(true, checkCase[y + 1][x - 1]);
                }
                if (Btwn(y + 3, 1, 6) && Btwn(x - 3, 1, 7))
                {
                    if (checkCase[y][x] == 0 && checkCase[y + 3][x - 3] != 0 && checkCase[y + 3][x - 3] == checkCase[y + 2][x - 2] && checkCase[y + 2][x - 2] == checkCase[y + 1][x - 1])
                        return new CheckData(true, checkCase[y + 1][x - 1]);
                }
            }
            return new CheckData(false);
        }
    }
}
