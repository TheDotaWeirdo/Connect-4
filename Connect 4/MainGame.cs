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
        public bool vsAI = true;
        public bool PredicitveAI = true;
        public bool StrategicAI = false;
        public bool FastGame = false;
        public int[] P = { -1, -1 };
        public int Turn = 0;
        public int Winner = 0;
        private int diff;
        public List<List<int>> Case = new List<List<int>>();
        public int Delay;// = { 1200, 800, 400, 200, 0 };
        double WinChance;// = { 90, 92.5, 95, 97.5, 100 };
        double BlockChance;// = { 80, 85, 90, 95, 100 };
        double FutureBlockChance;// = { 40, 55, 70, 85, 100 };
        double FutureMistakeChance;// = { 20, 40, 60, 80, 100 };
        double PredictChance;// = { 60, 70, 80, 90, 100 };
        double StrategicMovesChance;// = { 0, 25, 50, 75, 100 };
        public int[] State = { -1, -1, -1, -1, -1, -1, -1, -1 }; // State of each Column
        // -5: StrategicHorizontalCheck | horizontal 3-token link that can be won by each side
        // -4: StrategicCheck | checks if a play by the bot will create a future mistake state
        // -3: StrategicBlock | checks if a play by the player will create a future block state
        // -2: FutureStrategicBlock | checks if a play by the bot then player will create a block state
        // -1: unknown | either a starting state or a recent play
        // 0: Free | no certain states for the column
        // 1: FutureBlocked | case where playing in the column will result in a possible loss
        // 2: NearlyFull | case where the column is 1 token away from being full
        // 3: Full | case where the whole column is full
        // 4: FutureMistake | case where playing in the column will result in a loss of a possible win
        // 5: FutureBlocked&Mistake | mix of state 4 and state 1

        public int Diff {
            get { return diff; }
            set { diff = value;
                Delay = (100 - value) * ((FastGame) ? 4 : 12);
                WinChance = (value * .1) + 90;
                BlockChance = (value * .2) + 80;
                FutureBlockChance = (value * .6) + 40;
                FutureMistakeChance = (value * .8) + 20;
                PredictChance = (value * .4) + 60;
                StrategicMovesChance = value;
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

        public int PlayAI()
        {
            Random RND = new Random(Guid.NewGuid().GetHashCode());
            CheckData[] CD;
            int output = 0;
            List<int> PossWins = new List<int>(), PossLoss = new List<int>();
            //In Case of Possible Win or Loss
            for (int x = 1; x < 8; x++)
            {
                if(GetLow(x) > 0)
                    for (int y = 1; y < 7; y++)
                    {
                        CD = new CheckData[] { CheckDiagDown(x, y), CheckDiagUp(x, y), CheckHorizontal(x, y), CheckVertical(x, y) };
                        for (int i = 0; i < 4; i++)
                        {
                            if (CD[i].Check)
                            {
                                if (CD[i].Color == (P[1] + 1) && GetLow(x) > 0)
                                    PossWins.Add(x);
                                else if (CD[i].Color != (P[1] + 1) && GetLow(x) > 0)
                                    PossLoss.Add(x);
                            }
                        }
                    }
                if(PredicitveAI)
                {
                    if (State[x] <= 0)
                    {
                        int y = GetLow(x);
                        if (y > 1)
                        {
                            Case[y][x] = P[1] + 1; Case[y - 1][x] = P[0] + 1;
                            if (CheckWin(false) - 1 == P[0])
                                State[x] = 1;
                            else
                                State[x] = 0;
                            Case[y][x] = P[0] + 1; Case[y - 1][x] = P[1] + 1;
                            if (CheckWin(false) - 1 == P[1])
                                State[x] += 4;
                            Case[y][x] = Case[y - 1][x] = 0;
                        }
                        else if (y == 1)
                            State[x] = 2;
                        else
                            State[x] = 3;
                    }
                }
            }
            if (PossWins.Count > 0 && RND.NextDouble() * 100 <= WinChance) 
            { output = PossWins[RND.Next(0, PossWins.Count)]; goto EndPoint; }
            else if(PossLoss.Count > 0 && RND.NextDouble() * 100 <= BlockChance)
            { output = PossLoss[RND.Next(0, PossLoss.Count)]; goto EndPoint; }
            if(StrategicAI && RND.NextDouble() * 100 <= StrategicMovesChance)
                for (int x = 1; x < 8; x++)
                {
                    if (State[x] == 0)
                        if (StrategicHorizontalCheck(x))
                            State[x] = -5;
                    if (State[x] == 0)
                        if (StrategicCheck(x, 1))
                            State[x] = -4;
                    if (State[x] == 0)
                        if (StrategicCheck(x, 0))
                            State[x] = -3;
                    if (State[x] == 0)
                        if (FutureStrategicCheck(x))
                            State[x] = -2;
                }
            PredictivePoint:
            if (PredicitveAI)
            {                
                if(StrategicAI && (State.Contains(-2) || State.Contains(-3) || State.Contains(-4) || State.Contains(-5)))
                { output = GetUnblocked(0); goto EndPoint; }
                //Predictive Moves
                List<int>[] Moves = { new List<int>(), new List<int>() };
                if (RND.NextDouble() * 100 <= PredictChance)
                {
                    for (int x = 1; x < 8; x++)
                    {
                        if(GetLow(x) > 0)
                            for (int y = 1; y < 7; y++)
                            {
                                CheckData cd = PredictiveCheck(x, y);
                                if (RND.NextDouble() * 100 <= FutureBlockChance && cd.Check && CheckElligibility(cd.Color)) 
                                {
                                    Moves[Case[y][x] - 1].Add(cd.Color);
                                }
                            }
                    }
                }
                if (Moves[0].Count > 0 || Moves[1].Count > 0)
                {
                    if (RND.NextDouble() > .75)
                    {
                        if (Moves[P[1]].Count > 0)
                            output = Moves[P[1]][RND.Next(Moves[P[1]].Count)];
                        else if (Moves[P[0]].Count > 0)
                            output = Moves[P[0]][RND.Next(Moves[P[0]].Count)];
                    }
                    else if (Moves[P[0]].Count > 0)
                        output = Moves[P[0]][RND.Next(Moves[P[0]].Count)];
                    else if (Moves[P[1]].Count > 0)
                        output = Moves[P[1]][RND.Next(Moves[P[1]].Count)];
                }
                if (!CheckElligibility(output) && State[output]==1&& RND.NextDouble() * 100 <= FutureBlockChance)
                { output = GetUnblocked(output); goto PredictivePoint; }
                else if (!CheckElligibility(output) && (State[output] == 4 || State[output]==5) && RND.NextDouble() * 100 <= FutureMistakeChance)
                { output = GetUnblocked(output); goto PredictivePoint; }
            }
            EndPoint:
            if (output > 0 && GetLow(output) > 0)
            { while (Busy) { Thread.Sleep(1); } if(Finished) return 0; return output; }            
            output = RND.Next(1, 8); goto PredictivePoint;
        }

        public int CheckPossibleWin()
        {
            for (int y = 1; y < 7; y++)
                for (int x = 1; x < 8; x++)
                {
                    CheckData[] CD = { CheckDiagDown(x, y), CheckDiagUp(x, y), CheckHorizontal(x, y), CheckVertical(x, y) };
                    for (int i = 0; i < 4; i++)
                        if (CD[i].Check && CD[i].Color == (P[0] + 1) )
                        {
                            if (GetLow(x) > 0)
                                return x;
                        }
                }
            return 0;
        }

        public int CheckPossibleLoss()
        {
            for (int y = 1; y < 7; y++)
            {
                for (int x = 1; x < 8; x++)
                {
                    CheckData[] CD = { CheckDiagDown(x, y), CheckDiagUp(x, y), CheckHorizontal(x, y), CheckVertical(x, y) };
                    for (int i = 0; i < 4; i++)
                        if (CD[i].Check && CD[i].Color != (P[0] + 1))
                        {
                            if (GetLow(x) > 0)
                                return x;
                        }
                }
            }
            return 0;
        }

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
            {
                if (Case[i][col] == 0)
                    return i;
            }
            return 0;
        }

        private int GetUnblocked(int output)
        {
            int i = output;
            while(!CheckElligibility(i))
            { i = new Random().Next(1, 8); }
            return i;
        }

        private bool CheckElligibility(int output)
        {
            var RND = new Random();
            if (State[output] == -5)
                return true;
            if (State.Contains(-5) && (RND.NextDouble() <= Convert.ToDouble(Diff / 4)))
                return false;
            if (State[output] == -4)
                return true;
            if (State.Contains(-4) && (RND.NextDouble() <= Convert.ToDouble(Diff / 4.25)))
                return false;
            if (State[output] == -3)
                return true;
            if (State.Contains(-3) && (RND.NextDouble() <= Convert.ToDouble(Diff / 4.25)))
                return false;
            if (State[output] == 0)
                return true;
            if (State.Contains(0) || State[output] == 3) 
                return false;
            if (State[output] == 2)
                return true;
            if (State.Contains(2))
                return false;
            if (State[output] == -2)
                return true;
            if (State.Contains(-2))
                return false;
            if (State[output] == 4)
                return true;
            if (State.Contains(4))
                return false;
            return true;
        }

        private bool FutureStrategicCheck(int x)
        {
            int FutureBlocksCount = CountBlocks(State);
            int[] SaveState = new int[8]; State.CopyTo(SaveState, 0);
            int Y = GetLow(x); if (Y < 2) return false;
            Case[Y][x] = P[1] + 1;
            Case[Y - 1][x] = P[0] + 1;
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
            int FBC = CountBlocks(SaveState);
            Case[Y][x] = 0;
            Case[Y - 1][x] = 0;
            if (FutureBlocksCount < FBC)
                return true;
            return false;
        }

        private bool StrategicCheck(int x, int ID)
        {
            int FutureBlocksCount = CountBlocks(State);
            int[] SaveState = new int[8]; State.CopyTo(SaveState, 0);
            int Y = GetLow(x); if (Y < 1) return false;
            Case[Y][x] = P[ID] + 1;
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
            int FBC = CountBlocks(SaveState);
            Case[Y][x] = 0;
            if (FutureBlocksCount < FBC)
                return true;
            return false;
        }

        private bool StrategicHorizontalCheck(int x)
        {
            int Y = GetLow(x); if (Y < 1) return false;
            Case[Y][x] = P[0] + 1;
            for (int i = 1; i < 8; i++)
            {
                if (i - 4 > 0)
                    if (Case[Y][i] == 0 && Case[Y][i - 4] == 0 && Case[Y][i - 1] == P[0] + 1 && Case[Y][i - 2] == P[0] + 1 && Case[Y][i - 3] == P[0] + 1)
                        if ((x == i || Y == GetLow(i)) && (x == i - 4 || Y == GetLow(i - 4))) 
                        { Case[Y][x] = 0; return true; }
                if (i + 4 < 8)
                    if (Case[Y][i] == 0 && Case[Y][i + 4] == 0 && Case[Y][i + 1] == P[0] + 1 && Case[Y][i + 2] == P[0] + 1 && Case[Y][i + 3] == P[0] + 1)
                        if ((x == i || Y == GetLow(i)) &&  (x == i + 4 || Y == GetLow(i + 4))) 
                            { Case[Y][x] = 0; return true; }
            }
            Case[Y][x] = 0;
            return false;
        }


        private int CountBlocks(int[] st)
        {
            int o = 0;
            foreach (int i in st)
                if (i == 1 || i == 4 || i == 5)
                    o++;
            return o;
        }

        CheckData CheckHorizontal(int x, int y, bool win = false)
        {
            if (win)
            {
                try
                {
                    if (Case[y][x] != 0 && Case[y][x] == Case[y][x + 1] && Case[y][x] == Case[y][x + 2] && Case[y][x] == Case[y][x + 3])
                        return new CheckData(true, Case[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y), new Point(x + 2, y), new Point(x + 3, y) });
                }
                catch (Exception) { return new CheckData(false); }
            }
            else
            {
                try
                {
                    if (Case[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y][x + 2] != 0 && Case[y][x + 2] == Case[y][x + 1] && Case[y][x + 1] == Case[y][x + 3])
                        return new CheckData(true, Case[y][x+1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y][x - 1] != 0 && Case[y][x - 1] == Case[y][x + 1] && Case[y][x - 1] == Case[y][x + 2])
                        return new CheckData(true, Case[y][x + 1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y][x - 2] != 0 && Case[y][x - 2] == Case[y][x - 1] && Case[y][x - 1] == Case[y][x + 1])
                        return new CheckData(true, Case[y][x + 1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y][x - 3] != 0 && Case[y][x - 3] == Case[y][x - 2] && Case[y][x - 2] == Case[y][x - 1])
                        return new CheckData(true, Case[y][x - 1]);
                }
                catch (Exception) { }
            }
            return new CheckData(false);
        }

        CheckData CheckVertical(int x, int y, bool win = false)
        {
            if (win)
            {
                try
                {
                    if (Case[y][x] != 0 && Case[y][x] == Case[y + 1][x] && Case[y][x] == Case[y + 2][x] && Case[y][x] == Case[y + 3][x])
                        return new CheckData(true, Case[y][x], new List<Point> { new Point(x, y), new Point(x, y + 1), new Point(x, y + 2), new Point(x, y + 3) });
                }
                catch (Exception) { return new CheckData(false); }
            }
            else
            {
                try
                {
                    if (Case[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y + 1][x] != 0 && Case[y + 1][x] == Case[y+2][x] && Case[y+1][x] == Case[y+3][x])
                        return new CheckData(true, Case[y+1][x]);
                }
                catch (Exception) { }
            }
            return new CheckData(false);
        }

        CheckData CheckDiagDown(int x, int y, bool win = false)
        {
            if (win)
            {
                try
                {
                    if (Case[y][x] != 0 && Case[y][x] == Case[y+1][x + 1] && Case[y][x] == Case[y+2][x + 2] && Case[y][x] == Case[y+3][x + 3])
                        return new CheckData(true, Case[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y + 1), new Point(x + 2, y + 2), new Point(x + 3, y + 3) });
                }
                catch (Exception) { return new CheckData(false); }
            }
            else
            {
                try
                {
                    if (Case[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y + 2][x + 2] != 0 && Case[y + 2][x + 2] == Case[y + 1][x + 1] && Case[y + 2][x + 2] == Case[y + 3][x + 3])
                        return new CheckData(true, Case[y + 1][x + 1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y - 1][x - 1] != 0 && Case[y - 1][x - 1] == Case[y + 1][x + 1] && Case[y - 1][x - 1] == Case[y + 2][x + 2])
                        return new CheckData(true, Case[y + 1][x + 1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y - 2][x - 2] != 0 && Case[y - 2][x - 2] == Case[y - 1][x - 1] && Case[y - 1][x - 1] == Case[y + 1][x + 1])
                        return new CheckData(true, Case[y + 1][x + 1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y - 3][x - 3] != 0 && Case[y - 3][x - 3] == Case[y - 2][x - 2] && Case[y - 2][x - 2] == Case[y - 1][x - 1])
                        return new CheckData(true, Case[y - 1][x - 1]);
                }
                catch (Exception) { }
            }
            return new CheckData(false);
        }

        CheckData CheckDiagUp(int x, int y, bool win = false)
        {
            if (win)
            {
                try
                {
                    if (Case[y][x] != 0 && Case[y][x] == Case[y - 1][x + 1] && Case[y][x] == Case[y - 2][x + 2] && Case[y][x] == Case[y - 3][x + 3]) 
                        return new CheckData(true, Case[y][x], new List<Point> { new Point(x, y), new Point(x + 1, y - 1), new Point(x + 2, y - 2), new Point(x + 3, y - 3) });
                }
                catch (Exception) { return new CheckData(false); }
            }
            else
            {
                try
                {
                    if (Case[y + 1][x] == 0 && y < 6)
                        return new CheckData(false);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y - 2][x + 2] != 0 && Case[y - 2][x + 2] == Case[y - 1][x + 1] && Case[y - 1][x + 1] == Case[y - 3][x + 3])
                        return new CheckData(true, Case[y - 1][x + 1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y + 1][x - 1] != 0 && Case[y + 1][x - 1] == Case[y - 1][x + 1] && Case[y + 1][x - 1] == Case[y - 2][x + 2])
                        return new CheckData(true, Case[y - 1][x + 1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y + 2][x - 2] != 0 && Case[y + 2][x - 2] == Case[y + 1][x - 1] && Case[y + 1][x - 1] == Case[y - 1][x + 1])
                        return new CheckData(true, Case[y + 1][x - 1]);
                }
                catch (Exception) { }
                try
                {
                    if (Case[y][x] == 0 && Case[y + 3][x - 3] != 0 && Case[y + 3][x - 3] == Case[y + 2][x - 2] && Case[y + 2][x - 2] == Case[y + 1][x - 1])
                        return new CheckData(true, Case[y + 1][x - 1]);
                }
                catch (Exception) { }
            }
            return new CheckData(false);
        }
        
        CheckData PredictiveCheck(int x, int y)
        {
            if(Case[y][x] != 0)
            {
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        try
                        {
                            if (!(i == 0 && j == 0) && Case[y][x] == Case[y + i][x + j])
                            {
                                if (Case[y + i + i][x + j + j] == 0 && GetLow(x + j + j) == (y + i + i) && x + j + j != 0) 
                                    return new CheckData(true, x + j + j);
                            }
                            if (!(i == 0 && j == 0) && Case[y][x] == Case[y + i + i][x + j + i])
                            {
                                if (Case[y + i][x + j] == 0 && GetLow(x + j) == (y + i) && x + j != 0)
                                    return new CheckData(true, x + j);
                            }
                        }
                        catch(Exception) { }
                    }
                }
            }
            return new CheckData(false);
        }
    }
}
