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
        public bool LeftIsRed = true;
        public bool Finished = true;
        public bool Busy = true;
        public bool Loading = false;
        public bool vsAI = true;
        public bool PredictiveAI = true;
        public bool StrategicAI = false;
        public bool HumanizedAI = false;
        public bool FastGame = false;
        public int[] P = { -1, -1 };
        public int Turn = 0;
        public int Winner = 0;
        private int diff;
        public List<List<int>> Case = new List<List<int>>();
        public int Delay;
        public int Moves = 0;
        double WinChance;
        double BlockChance;
        double ThreatBlockChance;
        double ThreatChance;
        double PredictWinChance;
        double PredictLossChance;
        double StrategicCheckChance;
        double StrategicBlockChance;
        double FutureStrategicBlockChance;
        double SacrificeChance;
        public int Positiveness = 0;
        public List<int> PlayerPositiveness = new List<int>();
        public Dictionary<int, int> Effectiveness = new Dictionary<int, int> { { -10, 1 }, { -9, -1 }, { -8, 3 }, { -7, -2 }, { -6, 2 }, { -5, 3 }, { -4, 1 }, { -3, -1 }, { -2, -2 }, { -1, -2 }, { 0, 0 }, { 1, -2 }, { 2, 0 }, { 3, 0 }, { 4, 2 }, { 5, 2 }, { 6, -1 }, { 7, -1 }, { 8, 1 } };
        public Dictionary<int, List<int>> St = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
        public Dictionary<int, int> Se = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        public List<Dictionary<int, List<int>>> PlayerState = new List<Dictionary<int, List<int>>>();
        public List<Dictionary<int, int>> PlayerSeverity = new List<Dictionary<int, int>>();
        public Dictionary<int, List<int>> HumanState = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
        public Dictionary<int, int> HumanSeverity = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        public Dictionary<int, List<int>> AIState = new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } };
        public Dictionary<int, int> AISeverity = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
        // States of each column are cases where specified conditions are met, a column can have more than one state at a time
        // State Severity of a column is calculated based on the multiple states applied in a column, the higher the severity the more likely the column will be played

        // -10: FuturePredictiveCheck | checks if a play by the bot will create a Predicted Threat (8) state in another column
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

        public int Diff {
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
            ResetPlayerStSe();
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
            St = AIState; Se = AISeverity; Positiveness = 0;
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
            Random RND = new Random(Guid.NewGuid().GetHashCode());
            // Loops through all columns to calculate the states and severity
            for (int x = 1; x < 8; x++)
            {
                // Resets the column's old values
                Se[x] = 0;
                St[x] = new List<int> { 0 };
                int y = GetLow(x);
                // If the Column is full, state = 3
                if (y == 0)
                { St[x].Add(3); Se[x] = -100; Positiveness += Effectiveness[3]; }
                else
                {
                    // Checks for possible wins
                    Case[y][x] = P[1] + 1;
                    if (CheckWin() == P[1] + 1 && RND.NextDouble() * 100 <= WinChance)
                    {
                        St[x].Add(5);
                        Se[x] += 50;
                        Positiveness += Effectiveness[5];
                    }
                    else
                    {
                        // Checks for possible losses
                        Case[y][x] = P[0] + 1;
                        if (CheckWin() == P[0] + 1 && RND.NextDouble() * 100 <= BlockChance)
                        {
                            St[x].Add(6);
                            Se[x] += 15;
                            Positiveness += Effectiveness[6];
                        }
                    }
                    Case[y][x] = 0;
                    // In case the column is 1-token short from being full
                    if (y == 1)
                    { St[x].Add(2); Se[x] -= 1; Positiveness += Effectiveness[2]; }
                    else
                    {
                        // In case playing in the column will result in an open possible loss
                        if (RND.NextDouble() * 100 <= ThreatBlockChance && CheckThreat(x, P[0], 1))
                        { St[x].Add(1); Se[x] -= 5; Positiveness += Effectiveness[1]; }
                        // In case playing in the column will result in a loss of a possible win
                        if (RND.NextDouble() * 100 <= ThreatChance && CheckThreat(x, P[1], 1))
                        { St[x].Add(4); Se[x] -= 4; Positiveness += Effectiveness[4]; }
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
                    {  St[x].Add(-6); Se[x] += (int) Math.Floor(1.4 * (temp + 1)); Positiveness += Effectiveness[-6]; }
                    // In case the Player playing the column will result in a possible loss state
                    Case[y][x] = P[0] + 1;
                    if (RND.NextDouble() * 100 <= PredictLossChance && (temp = CountPossLoss(x)) > 0)
                    {  St[x].Add(-7); Se[x] += (int) Math.Floor(1.4 * (temp + 1)); Positiveness += Effectiveness[-7]; }
                    Case[y][x] = 0;

                    if (y - 2 > 0)
                    {
                        // In case playing in the column will result in an open possible loss
                        if (RND.NextDouble() * 100 <= ThreatBlockChance && CheckThreat(x, P[0], 2))
                        { St[x].Add(7); Se[x] -= 2; Positiveness += Effectiveness[7]; }
                        // In case playing in the column will result in a loss of a possible win
                        if (RND.NextDouble() * 100 <= ThreatChance && CheckThreat(x, P[1], 2))
                        { St[x].Add(8); Se[x] += 2; Positiveness += Effectiveness[8]; }
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
            }
        }

        public void Humanize()
        {
            ResetPlayerStSe();
            int p0 = P[0], p1 = P[1];
            P[1] = p0; P[0] = p1;
            St = PlayerState[0]; Se = PlayerSeverity[0];
            CalculateStates();
            PlayerPositiveness.Add(Positiveness);
            for (int x = 1; x < 8; x++)
            {
                if (!AIState[x].Contains(3))
                {
                    int y = GetLow(x);
                    Case[y][x] = P[0] + 1;
                    St = PlayerState[x]; Se = PlayerSeverity[x];
                    CalculateStates();
                    Case[y][x] = 0;
                }
                PlayerPositiveness.Add(Positiveness);
            }
            int M = AISeverity.Values.Max(), m = AISeverity.Values.Min();
            for (int j = 1; j < 8; j++)
            {
                if (!AIState[j].Contains(3))
                {
                    AISeverity[j] += (int)Math.Ceiling((PlayerPositiveness[0] - PlayerPositiveness[j]) / 7d) / Math.Max(1, Math.Max(M, -m));
                }
            }
            P[1] = p1; P[0] = p0;
            Humanize2();
        }

        public void Humanize2()
        {
            int max = CountEmptySpaces();
            int p0 = P[0], p1 = P[1];
            List<int> TotMoves = new List<int> { -99, 0, 0, 0, 0, 0, 0, 0 };
            for (int i = 1; i < 8; i++)
            {
                var SaveCase = CaseCopy(Case);
                int Winner = 0;
                var _Turn = Turn;
                while ((Winner = CheckWin()) == 0 && !CheckTie() && TotMoves[i] <= 5) 
                {
                    TotMoves[i]++;
                    if (_Turn == 0)
                    { St = HumanState; Se = HumanSeverity; P[0] = p0; P[1] = p1; }
                    else
                    { St = PlayerState[0]; Se = PlayerSeverity[0]; P[0] = p1; P[1] = p0; }
                    CalculateStates();
                    int temp = ChooseColumn();
                    Case[GetLow(temp)][temp] = P[_Turn] + 1;
                    _Turn = (_Turn == 0) ? 1 : 0;
                }
                int M = AISeverity.Values.Max(), m = AISeverity.Values.Min();
                if (Winner == P[0] + 1 && TotMoves[i] > 2)
                    AISeverity[i] += -5 / Math.Max(1, Math.Max(M, -m)); 
                else if (Winner == P[1] + 1 && TotMoves[i] > 2)
                    AISeverity[i] += 5 / Math.Max(1, Math.Max(M, -m)); 
                Case = CaseCopy(SaveCase);
            }
            P[0] = p0; P[1] = p1;
        }

        private int CountEmptySpaces()
        {
            int n = 0;
            for (int x = 1; x < 8; x++)
            {
                for (int y = 1; y < 7; y++)
                {
                    if (Case[y][x] == 0)
                        n++;
                }
            }
            return n;
        }

        private List<List<int>> CaseCopy(List<List<int>> saveCase)
        {
            List<List<int>> output = new List<List<int>>();
            for (int i = 0; i < 7; i++)
            {
                List<int> V = new List<int>();
                for (int j = 0; j < 8; j++)
                {
                    V.Add(saveCase[i][j]);
                }
                output.Add(V);
            }
            return output;
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

        private int CountPossLoss(int n)
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
                        if (StrategicAI && (St[x].Contains(1) || St[x].Contains(4)))
                        { St[n].Add(-1); Se[n] += 2; }
                    }
                    Case[y][x] = 0;
                }
            }
            return output ;
        }

        private int CountPossWin(int n)
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
                        if (StrategicAI && (St[x].Contains(1) || St[x].Contains(4)))
                        { St[n].Add(-5); Se[n] += 3; }
                    }
                    Case[y][x] = 0;
                }
            }
            return output;
        }

        private int ChooseColumn()
        {
            int output = new Random().Next(1, 8), Max = Se[1];
            for (int i = 2; i < 8; i++)
                if (Se[i] > Max)
                    Max = Se[i];
            while (Se[output] != Max)
            { output = new Random().Next(1, 8); }
            return output;
        }

        private bool SacrificeCheck(int x)
        {
            int y = GetLow(x); if (y - 2 <= 0) return false;
            Case[y][x] = P[1] + 1;
            Case[y-1][x] = P[0] + 1;
            Case[y-2][x] = P[1] + 1;
            if (CheckWin() == P[1] + 1)
            {
                Case[y][x] =
                Case[y - 1][x] =
                Case[y - 2][x] = 0;
                return true;
            }
            Case[y][x] =
            Case[y - 1][x] =
            Case[y - 2][x] = 0;
            return false;
        }

        private bool FutureStrategicCheck(int x)
        {
            int ThreatBlocksCount = 0;
            foreach (List<int> s in St.Values)
                if (s.Contains(1))
                    ThreatBlocksCount++;
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
                        if (!St[i].Contains(-4) && CheckWin() - 1 == P[0])
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
            if (ThreatBlocksCount < FBC)
                return true;
            return false;
        }

        private bool StrategicCheck(int x, int ID, int Severity)
        {
            int y = GetLow(x);
            Case[y][x] = P[ID] + 1;
            for (int i = 1; i < 8; i++)
            {
                if(x != i)
                {
                    if (Severity == 1)
                    {
                        if (!St[i].Contains((ID == 0) ? 1 : 4) && CheckThreat(i, P[ID], Severity))
                        { Case[y][x] = 0; return true; }
                    }
                    else if (Severity == 2)
                    {
                        if (!St[i].Contains((ID == 0) ? 7 : 8) && CheckThreat(i, P[ID], Severity))
                        { Case[y][x] = 0; return true; }
                    }
                }
            }
            Case[y][x] = 0;
            return false;
        }
        
        private void ResetPlayerStSe()
        {
            PlayerSeverity = new List<Dictionary<int, int>>();
            PlayerState = new List<Dictionary<int, List<int>>>();
            PlayerPositiveness = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                PlayerState.Add(new Dictionary<int, List<int>> { { 1, new List<int> { 0 } }, { 2, new List<int> { 0 } }, { 3, new List<int> { 0 } }, { 4, new List<int> { 0 } }, { 5, new List<int> { 0 } }, { 6, new List<int> { 0 } }, { 7, new List<int> { 0 } } });
                PlayerSeverity.Add(new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } });
            }
        }

        private int CountSeverity(Dictionary<int, int> A)
        {
            int a = 0;
            foreach (int i in A.Values)
                a += i;
            return a;
        }

        private bool CheckThreat(int Column, int Winner, int Layers)
        {
            int y = GetLow(Column), t = Winner;
            bool b = true;
            if (y - Layers <= 0) return false;
            if (Layers % 2 == 1) t = (t == 1) ? 0 : 1;
            for (int i = y; i >= y - Layers; i--)
            {
                Case[i][Column] = t + 1;
                t = (t == 1) ? 0 : 1;
                if (i != y - Layers && CheckWin() != 0)
                    b = false;
            }
            b = b && CheckWin() == Winner + 1;
            for (int i = y - Layers; i <= y; i++)
                Case[i][Column] = 0;
            return b;
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
