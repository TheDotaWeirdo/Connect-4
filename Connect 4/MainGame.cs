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
        public bool vsAI = true;
        public bool PredicitveAI = true;
        public int[] P = { -1, -1 };
        public int Turn = 0;
        public int Winner = -1;
        public int Diff = 1;
        public List<List<int>> Case = new List<List<int>>();
        public int[] Delay = { 1000, 500, 100, 0 };
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
            int output = 0;
            List<int> FBlock = new List<int>();
            if(Diff < 3)
                Thread.Sleep(Delay[Diff]);
            //In Case of Possible Win 
            Start: for (int y = 1; y < 7; y++)
            {
                for (int x = 1; x < 8; x++)
                {
                    CheckData[] CD = { CheckDiagDown(x, y), CheckDiagUp(x, y), CheckHorizontal(x, y), CheckVertical(x, y) };
                    for (int i = 0; i < 4; i++) 
                        if (CD[i].Check && CD[i].Color == (P[1]+1) && !FBlock.Contains(x))
                        {
                            output = x;
                            if (GetLow(output) > 0)
                                goto P2;
                        }
                }
            }
            //In Case of Possible Loss
            if (new Random().NextDouble() * 100 <= BlockChance[Diff])
            {
                for (int y = 1; y < 7; y++)
                {
                    for (int x = 1; x < 8; x++)
                    {
                        CheckData[] CD = { CheckDiagDown(x, y), CheckDiagUp(x, y), CheckHorizontal(x, y), CheckVertical(x, y) };
                        for (int i = 0; i < 4; i++)
                            if (CD[i].Check && CD[i].Color != (P[1] + 1) && !FBlock.Contains(x))
                            {
                                output = x;
                                if (GetLow(output) > 0)
                                    goto P2;
                            }
                    }
                }
            }
            if (PredicitveAI)
            {
                //Predictive Moves
                List<int>[] Moves = { new List<int>(), new List<int>() };
                if (new Random().NextDouble() * 100 <= PredictChance[Diff])
                {
                    for (int y = 1; y < 7; y++)
                    {
                        for (int x = 1; x < 8; x++)
                        {
                            CheckData CD = PredictiveCheck(x, y);
                            if (CD.Check && !FBlock.Contains(CD.Color))
                            {
                                Moves[Case[y][x] - 1].Add(CD.Color);
                            }
                        }
                    }
                }
                if (Moves[0].Count > 0 || Moves[1].Count > 0)
                {
                    if (new Random().NextDouble() > .65)
                    {
                        if (Moves[P[1]].Count > 0)
                            output = Moves[P[1]][new Random().Next(Moves[P[1]].Count)];
                        else if (Moves[P[0]].Count > 0)
                            output = Moves[P[0]][new Random().Next(Moves[P[0]].Count)];
                    }
                    else if (Moves[P[0]].Count > 0)
                        output = Moves[P[0]][new Random().Next(Moves[P[0]].Count)];
                    else if (Moves[P[1]].Count > 0)
                        output = Moves[P[1]][new Random().Next(Moves[P[1]].Count)];
                }
                P:
                //Future Block Prediction
                if (new Random().NextDouble() * 100 <= FutureBlockChance[Diff] && FBlock.Count < 7)
                {
                    int y = GetLow(output);
                    if (y > 1)
                    {
                        Case[y][output] = P[1] + 1; Case[y - 1][output] = P[0] + 1;
                        if (CheckWin(false) - 1 == P[0])
                        {
                            FBlock.Add(output);
                            Case[y][output] = Case[y - 1][output] = 0;
                            goto Start;
                        }
                        Case[y][output] = Case[y - 1][output] = 0;
                    }
                    else
                    {
                        FBlock.Add(output);
                        goto Start;
                    }
                }
                else if (FBlock.Count > 6)
                    for (int i = 0; i < FBlock.Count; i++)
                    {
                        FBlock[i] = 0;
                    }
                if (output > 0 && GetLow(output) > 0)
                    return output;
                output = new Random().Next(1, 8); goto P;
            }
            P2:
            if (output > 0 && GetLow(output) > 0)
                return output;
            output = new Random().Next(1, 8); goto P2;
        }

        public int CheckWin(bool VChange = true)
        {
            List<Point> LP = new List<Point>();
            for (int y = 1; y < 7; y++)
            {
                for (int x = 1; x < 8; x++)
                {
                    CheckData
                    CD = CheckDiagDown(x, y, true);
                    if (CD.Check)
                        AssessWin(CD, ref LP);
                    CD = CheckDiagUp(x, y, true);
                    if (CD.Check)
                        AssessWin(CD, ref LP);
                    CD = CheckHorizontal(x, y, true);
                    if (CD.Check)
                        AssessWin(CD, ref LP);
                    CD = CheckVertical(x, y, true);
                    if (CD.Check)
                        AssessWin(CD, ref LP);
                }
            }
            if (Winner > 0 && VChange)
                foreach (Point P in LP)
                    Case[P.Y][P.X] = Winner + 2;
            if (VChange)
                return Winner;
            int i = Winner;
            Winner = 0;
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
            return Winner = CD.Color;
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
