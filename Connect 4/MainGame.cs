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
        public bool PredicitveAI = true;
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
        public Dictionary<int, int> State = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        public Dictionary<int, int> Severity = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        // States of each column are cases where specified conditions are met, a column can have more than one state at a time
        // State Severity of a column is calculated based on the multiple states applied in a column, the higher the severity the more likely the column will be played

        // -7: Possible Loss Prediction | checks if a play by the Player will create a possible Loss (6) (Loss in 4 moves)
        // -6: Possible Win Prediction | checks if a play by the Bot will create a possible Win (5) (win in 2 moves)
        // -5: 
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
            set { diff = value;
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
            Random RND = new Random(Guid.NewGuid().GetHashCode());
            int output = 0;
            for (int x = 1; x < 8; x++)
            {
                Severity[x] = State[x] = 0;
                int y = GetLow(x);
                //In Case of Possible Win or Loss
                if (y == 0)
                { State[x] = 3; Severity[x] = -100; }
                else
                {
                    State[x] = 0;
                    Case[y][x] = P[1] + 1;
                    if (CheckWin(false) == P[1] + 1 && RND.NextDouble()*100 <= WinChance)
                    {
                        State[x] = 5;
                        Severity[x] += 50;
                    }
                    else
                    {
                        Case[y][x] = P[0] + 1;
                        if (CheckWin(false) == P[0] + 1 && RND.NextDouble() * 100 <= BlockChance)
                        {
                            State[x] = 6;
                            Severity[x] += 25;
                        }
                    }
                    Case[y][x] = 0;
                    if (PredicitveAI)
                    {
                        if (y == 1)
                        { State[x] = 2; Severity[x] += -1; }
                        else
                        {
                            Case[y][x] = P[1] + 1; Case[y - 1][x] = P[0] + 1;
                            if (CheckWin(false) - 1 == P[0] && RND.NextDouble() * 100 <= FutureBlockChance)
                            { State[x] = 1; Severity[x] -= 5; }
                            Case[y][x] = P[0] + 1; Case[y - 1][x] = P[1] + 1;
                            if (CheckWin(false) - 1 == P[1] && RND.NextDouble() * 100 <= FutureMistakeChance)
                            { State[x] = 4; Severity[x] -= 5; }
                            Case[y][x] = Case[y - 1][x] = 0;
                            //
                            int temp;
                            Case[y][x] = P[1] + 1;
                            if ((temp = CountPossWinLoss()[0]) > 0 && RND.NextDouble() * 100 <= PredictWinChance)
                            { if (State[x] <= 0) State[x] = -6; Severity[x] += 2 * temp; }
                            Case[y][x] = P[0] + 1;
                            if ((temp = CountPossWinLoss()[1]) > 0 && RND.NextDouble() * 100 <= PredictLossChance)
                            { if (State[x] <= 0) State[x] = -7; Severity[x] += temp; }
                            Case[y][x] = 0;
                        }
                    }
                    if (StrategicAI)
                    {
                        if (StrategicCheck(x, 1) && RND.NextDouble() * 100 <= StrategicCheckChance)
                        { if (State[x] <= 0) State[x] = -4; Severity[x] += 3; }
                        if (StrategicCheck(x, 0) && RND.NextDouble() * 100 <= StrategicBlockChance)
                        { if (State[x] <= 0) State[x] = -3; Severity[x] += 3; }
                        if ((FutureStrategicCheck(x) || StrategicCheck(x, 2)) && RND.NextDouble() * 100 <= FutureStrategicBlockChance)
                        { if (State[x] <= 0) State[x] = -2; Severity[x] -= 3; }
                    }
                }
            }
            output = ChooseColumn(RND.Next(1, 8));
            while (Busy)
            { Thread.Sleep(1); }
            if (Finished)
                return 0;
            return output;
        }

        //public int PlayAI()
        //{
        //    Random RND = new Random(Guid.NewGuid().GetHashCode());
        //    int output = 0;
        //    for (int x = 1; x < 8; x++)
        //    {
        //        int y;
        //        //In Case of Possible Win or Loss
        //        if ((y = GetLow(x)) > 0)
        //        {
        //            Case[y][x] = P[1] + 1;
        //            if (CheckWin(false) == P[1] + 1)
        //            {
        //                State[x] = 5;
        //                StateSeverity[x] = 99;
        //            }
        //            else
        //            {
        //                Case[y][x] = P[0] + 1;
        //                if (CheckWin(false) == P[0] + 1)
        //                {
        //                    State[x] = 6;
        //                    StateSeverity[x] = 50;
        //                }
        //            }
        //            Case[y][x] = 0;
        //        }
        //        //Predicitve AI column state calculations
        //        if (PredicitveAI)
        //        {
        //            if (State[x] <= 0)
        //            {
        //                y = GetLow(x);
        //                if (y > 1)
        //                {
        //                    Case[y][x] = P[1] + 1; Case[y - 1][x] = P[0] + 1;
        //                    if (CheckWin(false) - 1 == P[0])
        //                    { State[x] = 1; StateSeverity[x] = -9; }
        //                    else
        //                    { State[x] = 0; StateSeverity[x] = 0; }
        //                    Case[y][x] = P[0] + 1; Case[y - 1][x] = P[1] + 1;
        //                    if (State[x] <= 0 && CheckWin(false) - 1 == P[1])
        //                    { State[x] = 4; StateSeverity[x] = -5; }
        //                    Case[y][x] = Case[y - 1][x] = 0;
        //                }
        //                else if (y == 1)
        //                { State[x] = 2; StateSeverity[x] = -2; }
        //                else
        //                { State[x] = 3; StateSeverity[x] = -99; }
        //            }
        //        }
        //    }
        //    //if (State.ContainsValue(5) && RND.NextDouble() * 100 <= WinChance) 
        //    //{ output = GetUnblocked(RND.Next(1, 8)); goto EndPoint; }
        //    //else if(State.ContainsValue(6) && RND.NextDouble() * 100 <= BlockChance)
        //    //{ output = GetUnblocked(RND.Next(1, 8)); goto EndPoint; }
        //    if (StrategicAI && RND.NextDouble() * 100 <= StrategicMovesChance)
        //        for (int x = 1; x < 8; x++)
        //        {
        //            if (State[x] == 0)
        //            {
        //                if (StrategicCheck(x, 1))
        //                { State[x] = -4; StateSeverity[x] += 2; }
        //                if (StrategicCheck(x, 0))
        //                { State[x] = -3; StateSeverity[x] += 3; }
        //                if (FutureStrategicCheck(x) || StrategicCheck(x, 2))
        //                { State[x] = -2; StateSeverity[x] -= 3; }
        //                //int Y = GetLow(x);
        //                //Case[Y][x] = P[1] + 1;
        //                //if (CheckElligibility(x))
        //                //{
        //                //    int temp, y;
        //                //    var SaveState = new Dictionary<int, int> { { 1, StateSeverity[1] }, { 2, StateSeverity[2] }, { 3, StateSeverity[3] }, { 4, StateSeverity[4] }, { 5, StateSeverity[5] }, { 6, StateSeverity[6] }, { 7, StateSeverity[7] } };
        //                //    for (int X = 1; X < 8; X++)
        //                //    {
        //                //        if (X != x)
        //                //        {
        //                //            y = GetLow(X);
        //                //            Case[y][X] = P[0] + 1;
        //                //            if ((temp = CountPossWinLoss(X)[1]) > 0)
        //                //            { State[x] -= 10; SaveState[x] -= temp; }
        //                //            Case[y][X] = 0;
        //                //        }
        //                //    }
        //                //    StateSeverity = SaveState;
        //                //}
        //                //Case[Y][x] = 0;
        //            }
        //        }
        //    if (PredicitveAI)
        //    {
        //        //Predictive Moves
        //        int temp;
        //        var SaveState = new Dictionary<int, int> { { 1, StateSeverity[1] }, { 2, StateSeverity[2] }, { 3, StateSeverity[3] }, { 4, StateSeverity[4] }, { 5, StateSeverity[5] }, { 6, StateSeverity[6] }, { 7, StateSeverity[7] } };
        //        Dictionary<int, int[]> d1, d2; d1 = new Dictionary<int, int[]>(); d2 = new Dictionary<int, int[]>();
        //        for (int X = 1; X < 8; X++)
        //        {
        //            int Y = GetLow(X);
        //            Case[Y][X] = P[1] + 1;
        //            if (CheckElligibility(X))//State[X] == 0)
        //            {
        //                if ((temp = CountPossWinLoss()[0]) > 0)
        //                { State[X] = -6; SaveState[X] += 2 * temp; }
        //            }
        //            Case[Y][X] = P[0] + 1;
        //            if (CheckElligibility(X))
        //            {
        //                if ((temp = CountPossWinLoss()[1]) > 0)
        //                { State[X] = -7; SaveState[X] += temp; }
        //            }
        //            Case[Y][X] = 0;
        //            if (StrategicAI)
        //            {

        //            }
        //        }
        //        StateSeverity = SaveState;
        //    }
        //    EndPoint:
        //    if (output > 0 && GetLow(output) > 0 && CheckElligibility(output))
        //    { while (Busy) { Thread.Sleep(1); } if (Finished) return 0; return output; }
        //    output = GetUnblocked(RND.Next(1, 8)); goto EndPoint;
        //}

        public int CheckWin(bool VChange = true)
        {
            int i = 0;
            List<Point> LP = new List<Point>();
            for (int y = 1; y < 7; y++)
            {
                for (int x = 1; x < 8; x++)
                {
                    CheckData CD;
                    if (y < 4 && x < 5)
                    {
                        CD = CheckDiagDown(x, y, true);
                        if (CD.Check)
                            i = AssessWin(CD, ref LP);
                    }
                    if (y > 3 && x < 5)
                    {
                        CD = CheckDiagUp(x, y, true);
                        if (CD.Check)
                            i = AssessWin(CD, ref LP);
                    }
                    if (x < 5)
                    {
                        CD = CheckHorizontal(x, y, true);
                        if (CD.Check)
                            i = AssessWin(CD, ref LP);
                    }
                    if (y < 4)
                    {
                        CD = CheckVertical(x, y, true);
                        if (CD.Check)
                            i = AssessWin(CD, ref LP);
                    }
                }
            }
            if (i > 0 && VChange)
            {
                Finished = true;
                Winner = i;
                for (int y = 1; y < 7; y++)
                    for (int x = 1; x < 8; x++)
                        Case[y][x] = -Case[y][x];
                foreach (Point P in LP)
                    Case[P.Y][P.X] = i + 2;
            }
            return i;
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

        private int AssessWin(CheckData CD, ref List<Point> LP)
        {
            for (int i = 0; i < CD.Points.Count; i++)
                LP.Add(CD.Points[i]);  
            return CD.Color;
        }

        public int GetLow(int col)
        {
            for (int i = 6; i > 0; i--)
                if (Case[i][col] == 0)
                    return i;
            return 0;
        }

        private int[] CountPossWinLoss(int n = 0)
        {
            int c5 = 0, c6 = 0;
            for (int x = 1; x < 8; x++)
            {
                int y;
                //In Case of Possible Win or Loss
                if ((y = GetLow(x)) > 0 && x != n) 
                {
                    Case[y][x] = P[1] + 1;
                    if (CheckWin(false) == P[1] + 1)
                    {
                        c5++;
                    }
                    else
                    {
                        Case[y][x] = P[0] + 1;
                        if (CheckWin(false) == P[0] + 1)
                        {
                            c6++;
                        }
                    }
                    Case[y][x] = 0;
                }
            }
            return new int[] { c5, c6 };
        }

        private int ChooseColumn(int output = 0)
        {
            Dictionary<int, int> Max = new Dictionary<int, int> { { 1, Severity[1] } };
            for (int i = 2; i < 8; i++)
                if (Severity[i] > Max.Last().Value)
                    Max.Add(i, Severity[i]);
            while (Severity[output] != Max.Last().Value)
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
            foreach (int s in State.Values)
                if (s == 1)
                    FutureBlocksCount++;
            var SaveState = new Dictionary<int, int> { { 1, State[1] }, { 2, State[2] }, { 3, State[3] }, { 4, State[4] }, { 5, State[5] }, { 6, State[6] }, { 7, State[7] } };
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
                        if (CheckWin(false) - 1 == P[0])
                            State[i] = 1;
                        Case[y][i] = Case[y - 1][i] = 0;
                    }
                }
            }
            int FBC = 0;
            foreach (int s in SaveState.Values)
                if (s == 1)
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
            foreach (int s in State.Values)
                if (s == ((ID == 1) ? 4 : 1))
                    FutureBlocksCount++;
            var SaveState = new Dictionary<int, int> { { 1, State[1] }, { 2, State[2] }, { 3, State[3] }, { 4, State[4] }, { 5, State[5] }, { 6, State[6] }, { 7, State[7] } };
            int Y = GetLow(x); if (Y < 1) return false;
            Case[Y][x] = P[(ID == 2) ? 0 : ID] + 1;
            for (int i = 1; i < 8; i++)
            {
                int y = GetLow(i);
                if (y > 1)
                {
                    Case[y][i] = P[1] + 1; Case[y - 1][i] = P[0] + 1;
                    if (CheckWin(false) - 1 == P[0])
                        SaveState[i] = 1;
                    else
                        SaveState[i] = 0;
                    Case[y][i] = P[0] + 1; Case[y - 1][i] = P[1] + 1;
                    if (CheckWin(false) - 1 == P[1])
                        SaveState[i] += 4;
                    Case[y][i] = Case[y - 1][i] = 0;
                }
                else if (y == 1)
                    SaveState[i] = 2;
                else
                    SaveState[i] = 3;
            }
            int FBC = 0;
            foreach (int s in SaveState.Values)
                if (s == ((ID == 1) ? 4 : 1))
                    FBC++;
            Case[Y][x] = 0;
            if (FutureBlocksCount < FBC)
            {
                if (ID == 1)
                    return true;
                else
                {
                    SaveState = new Dictionary<int, int> { { 1, State[1] }, { 2, State[2] }, { 3, State[3] }, { 4, State[4] }, { 5, State[5] }, { 6, State[6] }, { 7, State[7] } };
                    Y = GetLow(x); if (Y < 1) return false;
                    Case[Y][x] = P[1] + 1;
                    for (int i = 1; i < 8; i++)
                    {
                        int y = GetLow(i);
                        if (y > 1)
                        {
                            Case[y][i] = P[1] + 1; Case[y - 1][i] = P[0] + 1;
                            if (CheckWin(false) - 1 == P[0])
                                SaveState[i] = 1;
                            Case[y][i] = Case[y - 1][i] = 0;
                        }
                    }
                    FBC = 0;
                    foreach (int s in SaveState.Values)
                        if (s == ((ID == 1) ? 4 : 1))
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
