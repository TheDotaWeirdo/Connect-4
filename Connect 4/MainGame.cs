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
        public int[] P = { -1, -1 };
        public int Turn = 0;
        public int Winner = 0;
        public int Diff = 1;
        public List<List<int>> Case = new List<List<int>>();
        public int[] Delay = { 1000, 500, 100, 1 };
        public int[] State = { -1, -1, -1, -1, -1, -1, -1, -1};
        double[] WinChance = { 90, 95, 99, 100 };
        double[] BlockChance = { 80, 90, 95, 100 };
        double[] FutureBlockChance = { 60, 75, 90, 100 };
        double[] PredictChance = { 70, 80, 90, 100 };

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
        }

        public int PlayAI()
        {
            Random RND = new Random(Guid.NewGuid().GetHashCode());
            int output = 0;
            //In Case of Possible Win 
            if (RND.NextDouble() * 100 <= WinChance[Diff])
            {
                for (int y = 1; y < 7; y++)
                {
                    for (int x = 1; x < 8; x++)
                    {
                        CheckData CD;
                        CD = CheckDiagDown(x, y);
                        if (CD.Check && CD.Color == (P[1] + 1))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                            { while (Busy) { } if (Finished) return 0; return output; }
                        }
                        CD = CheckDiagUp(x, y);
                        if (CD.Check && CD.Color == (P[1] + 1))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                            { while (Busy) { } if (Finished) return 0; return output; }
                        }
                        CD = CheckHorizontal(x, y);
                        if (CD.Check && CD.Color == (P[1] + 1))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                            { while (Busy) { } if (Finished) return 0; return output; }
                        }
                        CD = CheckVertical(x, y);
                        if (CD.Check && CD.Color == (P[1] + 1))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                            { while (Busy) { } if (Finished) return 0; return output; }
                        }
                    }
                }
            }
            //In Case of Possible Loss
            if (RND.NextDouble() * 100 <= BlockChance[Diff])
            {
                for (int y = 1; y < 7; y++)
                {
                    for (int x = 1; x < 8; x++)
                    {
                        CheckData CD;
                        CD = CheckDiagDown(x, y);
                        if (CD.Check && CD.Color != (P[1] + 1))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                            { while (Busy) { } if (Finished) return 0; return output; }
                        }
                        CD = CheckDiagUp(x, y);
                        if (CD.Check && CD.Color != (P[1] + 1))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                            { while (Busy) { } if (Finished) return 0; return output; }
                        }
                        CD = CheckHorizontal(x, y);
                        if (CD.Check && CD.Color != (P[1] + 1))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                            { while (Busy) { } if (Finished) return 0; return output; }
                        }
                        CD = CheckVertical(x, y);
                        if (CD.Check && CD.Color != (P[1] + 1))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                            { while (Busy) { } if (Finished) return 0; return output; }
                        }
                    }
                }
            }
            P:
            if (PredicitveAI)
            {
                for (int x = 1; x < 8; x++)
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
                            Case[y][x] = Case[y - 1][x] = 0;
                        }
                        else if (y == 1)
                            State[x] = 2;
                        else
                            State[x] = 3;
                    }
                }
                //Predictive Moves
                List<int>[] Moves = { new List<int>(), new List<int>() };
                if (RND.NextDouble() * 100 <= PredictChance[Diff])
                {
                    for (int y = 1; y < 7; y++)
                    {
                        for (int x = 1; x < 8; x++)
                        {
                            CheckData CD = PredictiveCheck(x, y);
                            if (CD.Check && (CheckElligibility(CD.Color) || RND.NextDouble() * 100 <= FutureBlockChance[Diff])) 
                            {
                                Moves[Case[y][x] - 1].Add(CD.Color);
                            }
                        }
                    }
                }
                if (Moves[0].Count > 0 || Moves[1].Count > 0)
                {
                    if (RND.NextDouble() > .65)
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
                //Future Block Prediction
                if (RND.NextDouble() * 100 <= FutureBlockChance[Diff])
                {                    
                    if (State[output] == 1 && (State.Contains(0) || State.Contains(2)))
                    { output = GetUnblocked(); goto P; }
                    else if (State[output] == 2 && State.Contains(0))
                    { output = GetUnblocked(); goto P; }
                }
            }
            if (output > 0 && GetLow(output) > 0)
            { while (Busy) { } if(Finished) return 0; return output; }
            output = RND.Next(1, 8); goto P;
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

        private int GetUnblocked()
        {
            int i = 0;
            if (State.Contains(0))
                while (State[i] != 0)
                    i = new Random().Next(1, 8);
            else if (State.Contains(2))
                while (State[i] != 2)
                    i = new Random().Next(1, 8);
            return i;
        }

        private bool CheckElligibility(int output)
        {
            return !((State[output] == 1 && (State.Contains(0) || State.Contains(2))) || (State[output] == 2 && State.Contains(0)));
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
                                if ((Case[y + i + i][x + j + j] == 0 && GetLow(x + j + j) == y + i + i) && (Case[y - i][x - j] == 0 && GetLow(x - j) == y - i))
                                    return new CheckData(true, x + j + j);
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
