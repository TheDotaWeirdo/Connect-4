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
        public bool Finished = false;
        public bool Busy = true;
        public bool Loading = false;
        public bool vsAI = true;
        public bool PredictiveAI = true;
        public bool StrategicAI = false;
        public bool FastGame = false;
        public int[] P = { -1, -1 };
        public int Turn = 0;
        public int Winner = 0;
        private int diff;
        public List<List<int>> Case = new List<List<int>>();
        public int Delay;
        double WinChance;
        double BlockChance;
        double FutureBlockChance;
        double FutureMistakeChance;
        double PredictWinChance;
        double PredictLossChance;
        double StrategicCheckChance;
        double StrategicBlockChance;
        double FutureStrategicBlockChance;
        public Dictionary<int, List<int>> State = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
        public Dictionary<int, int> Severity = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        // States of each column are cases where specified conditions are met, a column can have more than one state at a time
        // State Severity of a column is calculated based on the multiple states applied in a column, the higher the severity the more likely the column will be played

        // -7: Possible Loss Prediction | checks if a play by the Player will create a possible Loss (6) (Loss in 4 moves)
        // -6: Possible Win Prediction | checks if a play by the Bot will create a possible Win (5) (win in 2 moves)
        // -5: Block/Mistake Cancel Prediction | checks if a play by the Bot will force the player into surrendering a Future Block or Mistake
        //                                                                | or if a play by the player will force the bot into surrendering a Future Block or Mistake
        // -4: StrategicCheck | checks if a play by the bot will create a future mistake state 
        // -3: StrategicBlock | checks if a play by the player will create a future block state
        // -2: FutureStrategicBlock | checks if (a play by the bot then player) OR (any play in a column) will create a block state
        // -1: Unknown | either a starting state or a recent play in the column
        //  0: Free | no certain states for the column
        //  1: FutureBlocked | case where playing in the column will result in a possible loss (loss in 2 moves)
        //  2: NearlyFull | case where the column is 1 token away from being full
        //  3: Full | case where the whole column is full
        //  4: FutureMistake | case where playing in the column will result in a loss of a possible win
        //  5: Possible Win | (Win in 1 move)
        //  6: Possible Loss | (Loss in 1 move)

        public int Diff {
            get { return diff; }
            set {
                diff = value;
                Delay = (100 - value) * ((FastGame) ? 4 : 12);
                WinChance = (value * .1) + 90;
                BlockChance = (value * .2) + 80;
                FutureBlockChance = (value * .6) + 40;
                FutureMistakeChance = (value * .7) + 30;
                PredictWinChance = (value * .5) + 50;
                PredictLossChance = (value * .6) + 40;
                StrategicCheckChance = (value * .6) + 40;
                StrategicBlockChance = (value * .7) + 30;
                FutureStrategicBlockChance = (value * .8) + 20;
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
            for (int i = 0; i < 7; i++)
            {
                Case.Add(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 });
            }
            Turn = (new Random().NextDouble() < .5) ? 0 : 1;
            Diff = 50;
        }

        public bool Btwn(int Value, int Min, int Max, bool StrictCheck = false)
        {
            if (StrictCheck)
                return (Value > Min && Value < Max);
            return (Value >= Min && Value <= Max);
        }

        public int PlayAI()
        {
            // Calculate the State & Severity of each column
            CalculateStates();
            // Waits for any ongoing animations to be done
            while (Busy) { Thread.Sleep(1); }
            if (Finished)
                return 0; // In case the game was declared finished while calculations were ongoing
            // Chooses a column based on the severity of all of them
            // It will choose a random column from the highest severity 
            return ChooseColumn();
        }

        public void CalculateStates()
        {
            Random RND = new Random(Guid.NewGuid().GetHashCode());
            // Loops through all columns to calculate the states and severity
            for (int x = 1; x < 8; x++)
            {
                // Resets the column's old values
                Severity[x] = 0;
                State[x] = new List<int> { 0 };
                int y = GetLow(x);
                // If the Column is full, state = 3
                if (y == 0)
                { State[x].Add(3); Severity[x] = -100; }
                else
                {
                    // Checks for possible wins
                    Case[y][x] = P[1] + 1;
                    if (CheckWin() == P[1] + 1 && RND.NextDouble() * 100 <= WinChance)
                    {
                        State[x].Add(5);
                        Severity[x] += 50;
                    }
                    else
                    {
                        // Checks for possible losses
                        Case[y][x] = P[0] + 1;
                        if (CheckWin() == P[0] + 1 && RND.NextDouble() * 100 <= BlockChance)
                        {
                            State[x].Add(6);
                            Severity[x] += 15;
                        }
                    }
                    Case[y][x] = 0;
                    // In case the column is 1-token short from being full
                    if (y == 1)
                    { State[x].Add(2); Severity[x] -= 1; }
                    else
                    {
                        // In case playing in the column will result in an open possible loss
                        Case[y][x] = P[1] + 1; Case[y - 1][x] = P[0] + 1;
                        if (RND.NextDouble() * 100 <= FutureBlockChance && CheckWin() - 1 == P[0])
                        { State[x].Add(1); Severity[x] -= 5; }
                        // In case playing in the column will result in a loss of a possible win
                        Case[y][x] = P[0] + 1; Case[y - 1][x] = P[1] + 1;
                        if (RND.NextDouble() * 100 <= FutureMistakeChance && CheckWin() - 1 == P[1])
                        { State[x].Add(4); Severity[x] -= 4; }
                        Case[y][x] = Case[y - 1][x] = 0;
                    }
                }
            }

            for (int x = 1; x < 8; x++)
            {
                int y = GetLow(x);
                // Predictive AI calculations
                if (PredictiveAI)
                {
                    // In case playing the column will result in a possible win state
                    int temp;
                    Case[y][x] = P[1] + 1;
                    if (RND.NextDouble() * 100 <= PredictWinChance && (temp = CountPossWin(x)) > 0)
                    {  State[x].Add(-6); Severity[x] += (int) Math.Floor(1.4 * (temp + 1)); }
                    // In case the Player playing the column will result in a possible loss state
                    Case[y][x] = P[0] + 1;
                    if (RND.NextDouble() * 100 <= PredictLossChance && (temp = CountPossLoss(x)) > 0)
                    {  State[x].Add(-7); Severity[x] += (int) Math.Floor(1.4 * (temp + 1)); }
                    Case[y][x] = 0;                    
                }
                // Strategic AI calculations
                if (StrategicAI)
                {
                    // Checks if playing in the column will create a future mistake state (4)
                    if (RND.NextDouble() * 100 <= StrategicCheckChance && StrategicCheck(x, 1))
                    {  State[x].Add(-4); Severity[x] += 2; }
                    // Checks if the Player playing in the column will create a future block state (1)
                    if (RND.NextDouble() * 100 <= StrategicBlockChance && StrategicCheck(x, 0))
                    {  State[x].Add(-3); Severity[x] += 2; }
                    // Checks if a play by either the AI or player will create Future block state(1)
                    // Or if a play by the AI then the Player in the column will create a future block state (1)
                    if (!State[x].Contains(1) && !State[x].Contains(4) && RND.NextDouble() * 100 <= FutureStrategicBlockChance)
                    {
                        if(FutureStrategicCheck(x))
                        { State[x].Add(-2); Severity[x] -= 2; }
                        else if(StrategicCheck(x, 2))
                        { State[x].Add(-2); Severity[x] -= 1; }
                    }                    
                }
            }
        }

        public int AssessWin()
        {
            int output = 0;
            List<Point> LP = new List<Point>();
            for (int y = 6; y > 0; y--)
            {
                for (int x = 1; x < 8; x++)
                {
                    CheckData CD;
                    if (y < 4 && x < 5)
                    {
                        CD = CheckDiagDown(x, y, true);
                        if (CD.Check)
                        {
                            output = CD.Color;
                            for (int i = 0; i < CD.Points.Count; i++)
                                LP.Add(CD.Points[i]);
                        }
                    }
                    if (y > 3 && x < 5)
                    {
                        CD = CheckDiagUp(x, y, true);
                        if (CD.Check)
                        {
                            output = CD.Color;
                            for (int i = 0; i < CD.Points.Count; i++)
                                LP.Add(CD.Points[i]);
                        }
                    }
                    if (x < 5)
                    {
                        CD = CheckHorizontal(x, y, true);
                        if (CD.Check)
                        {
                            output = CD.Color;
                            for (int i = 0; i < CD.Points.Count; i++)
                                LP.Add(CD.Points[i]);
                        }
                    }
                    if (y < 4)
                    {
                        CD = CheckVertical(x, y, true);
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
                Winner = output;
                for (int y = 1; y < 7; y++)
                    for (int x = 1; x < 8; x++)
                        Case[y][x] = -Case[y][x];
                foreach (Point P in LP)
                    Case[P.Y][P.X] = output + 2;
            }
            return output;
        }

        public int CheckWin()
        {
            for (int y = 6; y > 0; y--)
            {
                for (int x = 1; x < 8; x++)
                {
                    CheckData CD;
                    if (y < 4 && x < 5)
                    {
                        CD = CheckDiagDown(x, y, true);
                        if (CD.Check)
                            return CD.Color;
                    }
                    if (y > 3 && x < 5)
                    {
                        CD = CheckDiagUp(x, y, true);
                        if (CD.Check)
                            return CD.Color;
                    }
                    if (x < 5)
                    {
                        CD = CheckHorizontal(x, y, true);
                        if (CD.Check)
                            return CD.Color;
                    }
                    if (y < 4)
                    {
                        CD = CheckVertical(x, y, true);
                        if (CD.Check)
                            return CD.Color;
                    }
                }
            }
            return 0;
        }

        public bool CheckTie()
        {
            for (int y = 1; y < 7; y++)
                for (int x = 1; x < 8; x++)
                    if (Case[y][x] == 0)
                            return false;
            for (int y = 1; y < 7; y++)
                for (int x = 1; x < 8; x++)
                    Case[y][x] = -Case[y][x];
            return true;
        }

        public int GetLow(int col)
        {
            for (int i = 6; i > 0; i--)
                if (Case[i][col] == 0)
                    return i;
            return 0;
        }

        private int CountPossLoss(int n = 0)
        {
            int output = 0;
            for (int x = 1; x < 8; x++)
            {
                int y;
                //In Case of Possible Win or Loss
                if ((y = GetLow(x)) > 0 && x != n)
                {
                    Case[y][x] = P[0] + 1;
                    if (CheckWin() == P[0] + 1)
                    {
                        output++;
                        if (StrategicAI && (State[x].Contains(1) || State[x].Contains(4)))
                        { State[n].Add(-5); Severity[n] += 2; }
                    }
                    Case[y][x] = 0;
                }
            }
            return output ;
        }

        private int CountPossWin(int n = 0)
        {
            int output = 0;
            for (int x = 1; x < 8; x++)
            {
                int y;
                //In Case of Possible Win or Loss
                if ((y = GetLow(x)) > 0 && x != n)
                {
                    Case[y][x] = P[1] + 1;
                    if (CheckWin() == P[1] + 1)
                    {
                        output++;
                        if (StrategicAI && (State[x].Contains(1) || State[x].Contains(4)))
                        { State[n].Add(-5); Severity[n] += 3; }
                    }
                    Case[y][x] = 0;
                }
            }
            return output;
        }

        private int ChooseColumn()
        {
            int output = new Random().Next(1, 8), Max = Severity[1];
            for (int i = 2; i < 8; i++)
                if (Severity[i] > Max)
                    Max = Severity[i];
            while (Severity[output] != Max)
            { output = new Random().Next(1, 8); }
            return output;
        }

        //private bool CheckElligibility(int output)
        //{
        //    Dictionary<int, int> Max = new Dictionary<int, int> { { 1, Severity[1] } };
        //    for (int i = 2; i < 8; i++)
        //    {
        //        if (Severity[i] > Max.Last().Value)
        //            Max.Add(i, Severity[i]);
        //    }
        //    return (Severity[output] == Max.Last().Value);
        //}

        private bool FutureStrategicCheck(int x)
        {
            int FutureBlocksCount = 0;
            foreach (List<int> s in State.Values)
                if (s.Contains(1))
                    FutureBlocksCount++;
            var SaveState = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
            //State.CopyTo(SaveState, 0);
            int Y = GetLow(x); if (Y < 2) return false;
            Case[Y][x] = P[1] + 1;
            Case[Y - 1][x] = P[0] + 1;
            for (int i = 1; i < 8; i++)
            {
                if (i != x)
                {
                    int y = GetLow(i);
                    if (y > 1)
                    {
                        Case[y][i] = P[1] + 1; Case[y - 1][i] = P[0] + 1;
                        if (!State[i].Contains(-4) && CheckWin() - 1 == P[0])
                            SaveState[i].Add(1);
                        Case[y][i] = Case[y - 1][i] = 0;
                    }
                }
            }
            int FBC = 0;
            foreach (List<int> s in SaveState.Values)
                if (s.Contains(1))
                    FBC++;
            Case[Y][x] = 0;
            Case[Y - 1][x] = 0;
            if (FutureBlocksCount < FBC)
                return true;
            return false;
        }

        private bool StrategicCheck(int x, int ID)
        {
            int FutureBlocksCount = 0;
            foreach (List<int> s in State.Values)
                if (s.Contains((ID == 1) ? 4 : 1))
                    FutureBlocksCount++;
            var SaveState = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
            int Y = GetLow(x); if (Y < 1) return false;
            Case[Y][x] = P[(ID == 2) ? 0 : ID] + 1;
            for (int i = 1; i < 8; i++)
            {
                int y = GetLow(i);
                if (y > 1)
                {
                    Case[y][i] = P[1] + 1; Case[y - 1][i] = P[0] + 1;
                    if (CheckWin() - 1 == P[0])
                        SaveState[i].Add(1);
                    else
                        SaveState[i].Add(0);
                    Case[y][i] = P[0] + 1; Case[y - 1][i] = P[1] + 1;
                    if (CheckWin() - 1 == P[1])
                        SaveState[i].Add(4);
                    Case[y][i] = Case[y - 1][i] = 0;
                }
                else if (y == 1)
                    SaveState[i].Add(2);
                else
                    SaveState[i].Add(3);
            }
            int FBC = 0;
            foreach (List<int> s in SaveState.Values)
                if (s.Contains((ID == 1) ? 4 : 1))
                    FBC++;
            Case[Y][x] = 0;
            if (FutureBlocksCount < FBC)
            {
                if (ID == 1)
                    return true;
                else
                {
                    SaveState = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
                    Y = GetLow(x); if (Y < 1) return false;
                    Case[Y][x] = P[1] + 1;
                    for (int i = 1; i < 8; i++)
                    {
                        int y = GetLow(i);
                        if (y > 1)
                        {
                            Case[y][i] = P[1] + 1; Case[y - 1][i] = P[0] + 1;
                            if (CheckWin() - 1 == P[0])
                                SaveState[i].Add(1);
                            Case[y][i] = Case[y - 1][i] = 0;
                        }
                    }
                    FBC = 0;
                    foreach (List<int> s in SaveState.Values)
                        if (s.Contains((ID == 1) ? 4 : 1))
                            FBC++;
                    Case[Y][x] = 0;
                    if (ID == 2 && FutureBlocksCount < FBC)
                        return true;
                    else if (ID == 0 && FutureBlocksCount >= FBC)
                        return true;
                }
            }
            return false;
        }

        CheckData CheckHorizontal(int x, int y, bool win = false)
        {
            if (win)
            {
                if (Btwn(x + 3, 1, 7)) 
                {
                    if (Case[y][x] != 0 && Case[y][x] == Case[y][x + 1] && Case[y][x] == Case[y][x + 2] && Case[y][x] == Case[y][x + 3])
                        return new CheckData(true, Case[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y), new Point(x + 2, y), new Point(x + 3, y) });
                }
                else { return new CheckData(false); }
            }
            else
            {
                if (Btwn(y + 1, 1, 6)) 
                {
                    if (Case[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                if (Btwn(x + 3, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y][x + 2] != 0 && Case[y][x + 2] == Case[y][x + 1] && Case[y][x + 1] == Case[y][x + 3])
                        return new CheckData(true, Case[y][x+1]);
                }
                if (Btwn(x + 2, 1, 7) && Btwn(x - 1, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y][x - 1] != 0 && Case[y][x - 1] == Case[y][x + 1] && Case[y][x - 1] == Case[y][x + 2])
                        return new CheckData(true, Case[y][x + 1]);
                }
                if (Btwn(x + 1, 1, 7) && Btwn(x - 2, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y][x - 2] != 0 && Case[y][x - 2] == Case[y][x - 1] && Case[y][x - 1] == Case[y][x + 1])
                        return new CheckData(true, Case[y][x + 1]);
                }
                if (Btwn(x - 3, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y][x - 3] != 0 && Case[y][x - 3] == Case[y][x - 2] && Case[y][x - 2] == Case[y][x - 1])
                        return new CheckData(true, Case[y][x - 1]);
                }
            }
            return new CheckData(false);
        }

        CheckData CheckVertical(int x, int y, bool win = false)
        {
            if (win)
            {
                if (Btwn(y + 3, 1, 6))
                {
                    if (Case[y][x] != 0 && Case[y][x] == Case[y + 1][x] && Case[y][x] == Case[y + 2][x] && Case[y][x] == Case[y + 3][x])
                        return new CheckData(true, Case[y][x], new List<Point> { new Point(x, y), new Point(x, y + 1), new Point(x, y + 2), new Point(x, y + 3) });
                }
                else { return new CheckData(false); }
            }
            else
            {
                if (Btwn(y + 1, 1, 6))
                {
                    if (Case[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                if (Btwn(y + 3, 1, 6))
                {
                    if (Case[y][x] == 0 && Case[y + 1][x] != 0 && Case[y + 1][x] == Case[y+2][x] && Case[y+1][x] == Case[y+3][x])
                        return new CheckData(true, Case[y+1][x]);
                }
            }
            return new CheckData(false);
        }

        CheckData CheckDiagDown(int x, int y, bool win = false)
        {
            if (win)
            {
                if (Btwn(y + 3, 1, 6) && Btwn(x + 3, 1, 7))
                {
                    if (Case[y][x] != 0 && Case[y][x] == Case[y+1][x + 1] && Case[y][x] == Case[y+2][x + 2] && Case[y][x] == Case[y+3][x + 3])
                        return new CheckData(true, Case[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y + 1), new Point(x + 2, y + 2), new Point(x + 3, y + 3) });
                }
                else { return new CheckData(false); }
            }
            else
            {
                if (Btwn(y + 1, 1, 6))
                {
                    if (Case[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                if (Btwn(y + 3, 1, 6) && Btwn(x + 3, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y + 2][x + 2] != 0 && Case[y + 2][x + 2] == Case[y + 1][x + 1] && Case[y + 2][x + 2] == Case[y + 3][x + 3])
                        return new CheckData(true, Case[y + 1][x + 1]);
                }
                if (Btwn(y + 2, 1, 6) && Btwn(x + 2, 1, 7) && Btwn(y - 1, 1, 6) && Btwn(x - 1, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y - 1][x - 1] != 0 && Case[y - 1][x - 1] == Case[y + 1][x + 1] && Case[y - 1][x - 1] == Case[y + 2][x + 2])
                        return new CheckData(true, Case[y + 1][x + 1]);
                }
                if (Btwn(y + 1, 1, 6) && Btwn(x + 1, 1, 7) && Btwn(y - 1, 1, 6) && Btwn(x - 1, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y - 2][x - 2] != 0 && Case[y - 2][x - 2] == Case[y - 1][x - 1] && Case[y - 1][x - 1] == Case[y + 1][x + 1])
                        return new CheckData(true, Case[y + 1][x + 1]);
                }
                if (Btwn(y - 3, 1, 6) && Btwn(x - 3, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y - 3][x - 3] != 0 && Case[y - 3][x - 3] == Case[y - 2][x - 2] && Case[y - 2][x - 2] == Case[y - 1][x - 1])
                        return new CheckData(true, Case[y - 1][x - 1]);
                }
            }
            return new CheckData(false);
        }

        CheckData CheckDiagUp(int x, int y, bool win = false)
        {
            if (win)
            {
                if (Btwn(y - 3, 1, 6) && Btwn(x + 3, 1, 7))
                {
                    if (Case[y][x] != 0 && Case[y][x] == Case[y - 1][x + 1] && Case[y][x] == Case[y - 2][x + 2] && Case[y][x] == Case[y - 3][x + 3]) 
                        return new CheckData(true, Case[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y - 1), new Point(x + 2, y - 2), new Point(x + 3, y - 3) });
                }
                else { return new CheckData(false); }
            }
            else
            {
                if (Btwn(y + 1, 1, 6))
                {
                    if (Case[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                if (Btwn(y - 3, 1, 6) && Btwn(x + 3, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y - 2][x + 2] != 0 && Case[y - 2][x + 2] == Case[y - 1][x + 1] && Case[y - 1][x + 1] == Case[y - 3][x + 3])
                        return new CheckData(true, Case[y - 1][x + 1]);
                }
                if (Btwn(y - 2, 1, 6) && Btwn(x + 2, 1, 7) && Btwn(y + 1, 1, 6) && Btwn(x - 1, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y + 1][x - 1] != 0 && Case[y + 1][x - 1] == Case[y - 1][x + 1] && Case[y + 1][x - 1] == Case[y - 2][x + 2])
                        return new CheckData(true, Case[y - 1][x + 1]);
                }
                if (Btwn(y - 1, 1, 6) && Btwn(x + 1, 1, 7) && Btwn(y + 2, 1, 6) && Btwn(x - 2, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y + 2][x - 2] != 0 && Case[y + 2][x - 2] == Case[y + 1][x - 1] && Case[y + 1][x - 1] == Case[y - 1][x + 1])
                        return new CheckData(true, Case[y + 1][x - 1]);
                }
                if (Btwn(y + 3, 1, 6) && Btwn(x - 3, 1, 7))
                {
                    if (Case[y][x] == 0 && Case[y + 3][x - 3] != 0 && Case[y + 3][x - 3] == Case[y + 2][x - 2] && Case[y + 2][x - 2] == Case[y + 1][x - 1])
                        return new CheckData(true, Case[y + 1][x - 1]);
                }
            }
            return new CheckData(false);
        }
    }
}
