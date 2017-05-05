using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Connect_4.Properties.Resources;

namespace Connect_4
{
    public partial class Form1 : Form
    {
        int[] TopImgSize = new int[] { 100, 155 };
        int GlowIndex = 0, CurrentMouseIndex = 0;
        bool InfiniteLoop = false;
        public MainGame MG = new MainGame();
        public List<List<PictureBox>> C = new List<List<PictureBox>>();
        public Bitmap W = White_Circle;
        public Bitmap[] ArrowRB = { Arrow_Red, Arrow_Blue };
        public Bitmap[] RB = { Red_Circle, Blue_Circle };
        public Bitmap[] TRB = { T_Red_Circle, T_Blue_Circle };
        public Bitmap[] Case = { White_Circle, Red_Circle, Blue_Circle, Gold_Red_Circle, Gold_Blue_Circle };
        PictureBox tempPBox;
        Bitmap tempBitmap;
        int tempInt, tempInt2, LossHelps = 0;
        int[] Helps = { 0, 0 };
        System.Timers.Timer HelpT = new System.Timers.Timer();
#if DEBUG
        Label tempLabel;
#endif

        public Form1()
        {
            InitializeComponent();
        }

        private void Col_Click(int index, bool AImove = false)
        {
#if DEBUG
            tempLabel = label1; tempInt = 1; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label3; tempInt = 2; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label4; tempInt = 3; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label5; tempInt = 4; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label6; tempInt = 5; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label7; tempInt = 6; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label8; tempInt = 7; tempLabel.Invoke(new Action(UpdateDebugState));
#endif
            int i = 1, Cap = MG.GetLow(index);
            if (Cap > 0 && !MG.Finished && !MG.Busy && (!MG.vsAI || MG.Turn == MG.P[0] || AImove))
            {
                if (LearnMCheckBox.Checked && MG.vsAI)
                {
                    if (Helps[0] != 0 && Helps[0] != index && !AImove)
                    {
                        Cursor.Position = new Point(C[2][Helps[0]].Location.X + ActiveForm.Location.X + 25, 10 + C[2][Helps[0]].Location.Y + ActiveForm.Location.Y);
                        tempInt = Helps[0];
                        label_Help.Invoke(new Action(HelpWinText));
                        label_Help.Invoke(new Action(label_Help.Show));
                        HelpT.Stop();
                        HelpT.Interval = 10000;
                        HelpT.Start();
                        HelpPrompt(true);
                        return;
                    }
                    else if (Helps[0] == 0 && Helps[1] != 0 && Helps[1] != index && LossHelps < 5 - (MG.Diff / 15) && !AImove)
                    {
                        Cursor.Position = new Point(C[2][Helps[1]].Location.X + ActiveForm.Location.X + 25, 10 + C[2][Helps[1]].Location.Y + ActiveForm.Location.Y);
                        LossHelps++;
                        tempInt = index; tempInt2 = Helps[1];
                        label_Help.Invoke(new Action(HelpLossText));
                        label_Help.Invoke(new Action(label_Help.Show));
                        HelpT.Stop();
                        HelpT.Interval = 10000;
                        HelpT.Start();
                        HelpPrompt(true);
                        return;
                    }
                }
                MG.Moves++;
                System.Timers.Timer T = new System.Timers.Timer((FGameCheckBox.Checked) ? 16.25 : 31);
                if (LearnMCheckBox.Checked && MG.vsAI)
                    T.Interval *= 2.5;
                MG.Case[Cap][index] = MG.Turn + 1;
                MG.Busy = true;
                tempBitmap = W;
                tempPBox = C[Cap][index];
                C[Cap][index].Invoke(new Action(UpdateControl));
                //lock (C[0][index])
                    C[0][index].Invoke(new Action(C[0][index].Hide));
                if (!MG.vsAI)
                    for (int I = 1; I < 8; I++)
                        lock(C[0][I])
                            C[0][I].Image = ArrowRB[(MG.Turn == 0) ? 1 : 0];
                T.Start();
                T.Elapsed += (s,e) => 
                {
                    if (i > 1)
                    {
                        lock (C[i - 1][index])
                        {                            
                            try { C[i - 1][index].Image = W; }
                            catch (InvalidOperationException)
                            {
                                tempPBox = C[i - 1][index];
                                tempBitmap = W;
                                C[i - 1][index].Invoke(new Action(UpdateControl));
                            }
                        }
                    }
                    if (i != Cap)
                    {
                        lock (C[i][index])
                        {
                            try { C[i][index].Image = TRB[MG.Turn]; }
                            catch (InvalidOperationException)
                            {
                                tempPBox = C[i][index];
                                tempBitmap = TRB[MG.Turn];
                                if(i>0)
                                    C[i][index].Invoke(new Action(UpdateControl));
                            }
                        }
                    }
                    else
                    {
                        try { C[Cap][index].Image = RB[MG.Turn]; }
                        catch (InvalidOperationException)
                        {
                            tempPBox = C[Cap][index];
                            tempBitmap = RB[MG.Turn];
                            C[Cap][index].Invoke(new Action(UpdateControl));
                        }
                        T.Dispose();
                        PlayEnd(index);
                    }
                    i++;
                };
                if (MG.vsAI && MG.Turn == MG.P[0])
                {
                    Thread workingThread = new Thread(new ThreadStart(PlayAI))
                    { IsBackground = true, Priority = ThreadPriority.AboveNormal };
                    workingThread.Start();
                }
            }
        }

        private void Col_Enter(int index, bool Forced = false)
        {
            if (Forced || ((!MG.vsAI || MG.Turn == MG.P[0]) && !MG.Finished && !MG.Busy && MG.GetLow(index) > 0))
            {
                C[0][index].Invoke(new Action(C[0][index].Show));
                int i = MG.GetLow(index);
                lock (C[i][index])
                { if(i>0) C[i][index].Image = TRB[MG.Turn]; }
            }
        }

        private void Col_Leave(int index, bool Forced = false)
        {
            if (C[0][index].Visible || Forced)
            {
                C[0][index].Invoke(new Action(C[0][index].Hide));
                int i = MG.GetLow(index);
                lock (C[i][index])
                { if (i > 0) C[i][index].Image = W; }
            }
        }

        private void PlayAI()
        {
            try
            {
                if (MG.Delay == 0)
                {
                    int i = MG.PlayAI();
                    if (i > 0)
                        Col_Click(i, true);
                }
                else
                {
                    System.Timers.Timer T = new System.Timers.Timer(MG.Delay);
                    T.Start();
                    int i = -1;
                    T.Elapsed += (s, e) =>
                    {
                        T.Dispose();
                        while (i == -1) { }
                        if (i > 0)
                            Col_Click(i, true);
                    };
                    i = MG.PlayAI();
                }
            }
            catch (IndexOutOfRangeException) { }
        }

        private void PlayEnd(int index)
        {
            if (MG.AssessWin() > 0)
            {
                UpdateVisuals();
                if (MG.Winner == 1)
                    TopPicture.Image = Win_Red;
                else
                    TopPicture.Image = Win_Blue;
                TopPicture.Invoke(new Action(TopImgScaleBig));
                button_Exit.Invoke(new Action(button_Exit.Show));
                button_Restart.Invoke(new Action(button_Restart.Show));
                Turn_Right.Invoke(new Action(Turn_Right.Hide));
                Turn_Left.Invoke(new Action(Turn_Left.Hide));
            }
            else if (MG.CheckTie())
            {
                UpdateVisuals();
                TopPicture.Image = Win_Tie;
                TopPicture.Invoke(new Action(TopImgScaleBig));
                button_Exit.Invoke(new Action(button_Exit.Show));
                button_Restart.Invoke(new Action(button_Restart.Show));
                Turn_Right.Invoke(new Action(Turn_Right.Hide));
                Turn_Left.Invoke(new Action(Turn_Left.Hide));
            }
            else
            {
                MG.Turn = (MG.Turn == 0) ? 1 : 0;
                Turn_Left.Invoke(new Action(TLUpdate));
                Turn_Right.Invoke(new Action(TRUpdate));
                if (MG.vsAI && LearnMCheckBox.Checked && MG.Turn == MG.P[0])
                {
                    Helps[0] = Helps[1] = 0;
                    for (int x = 1; x < 8; x++)
                    {
                        int y;
                        if ((y = MG.GetLow(x)) > 0)
                        {
                            MG.Case[y][x] = MG.P[0] + 1;
                            if (MG.CheckWin() == MG.P[0] + 1)
                            {
                                Helps[0] = x;
                            }
                            else
                            {
                                MG.Case[y][x] = MG.P[1] + 1;
                                if (MG.CheckWin() == MG.P[1] + 1)
                                {
                                    Helps[1] = x;
                                }
                            }
                            MG.Case[y][x] = 0;
                        }
                    }
                    //Helps[0] = MG.CheckPossibleWin();
                    //Helps[1] = MG.CheckPossibleLoss();
                }
                MG.Busy = false;
                Thread.Sleep(35);
                if (CurrentMouseIndex > 0 && MG.GetLow(CurrentMouseIndex) > 0 && (MG.Turn == ((MG.LeftIsRed) ? 0 : 1) || !MG.vsAI))
                    Col_Enter(CurrentMouseIndex, true);
            }
        }

        private void UpdateVisuals()
        {
            for (int y = 1; y < 7; y++)
            {
                for (int x = 1; x < 8; x++)
                {
                    try
                    {
                        lock (C[y][x])
                        {
                            if (MG.Case[y][x] >= 0)
                            {
                                if (!Equals(C[y][x].Image, Case[MG.Case[y][x]]))
                                    C[y][x].Image = Case[MG.Case[y][x]];
                            }
                            else if (!Equals(C[y][x].Image, TRB[-1 - MG.Case[y][x]]))
                                C[y][x].Image = TRB[-1 - MG.Case[y][x]];
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private void SlideEffect()
        {            
            int[] index = { 5, 3 };
            System.Timers.Timer Timer = new System.Timers.Timer((MG.FastGame) ? 65 : 150);
            Timer.Start();
            Timer.Elapsed += (S, E) =>
            {
                if (--index[0] != 0 && ++index[1] != 8)
                {
                    SlideEffect(index[0]);
                    if (index[1] != 4)
                        SlideEffect(index[1]);
                }
                else
                {
                    Timer.Dispose();
                }
            };            
        }

        private void SlideEffect(int index)
        {
            int ind = 7;
            System.Timers.Timer Timer = new System.Timers.Timer((MG.FastGame) ? 9 : 27.5);
            Timer.Start();
            Timer.Elapsed += (S, E) =>
              {
                  if (--ind > 0)
                  {
                      C[ind][index].Invoke(new Action(C[ind][index].Show));
                  }
                  else
                  {
                      Timer.Dispose();
                      if (index == 1)
                      {
                          if (!MG.FastGame)
                              Thread.Sleep(150);
                          Turn_Right.Invoke(new Action(Turn_Right.Show));
                          Turn_Left.Invoke(new Action(Turn_Left.Show));
                          if (!MG.FastGame)
                              Thread.Sleep(150);
                          TopPicture.Invoke(new Action(TopPicture.Show));
                          MG.Busy = false;
                          Point P = Cursor.Position;
                          Cursor.Position = new Point(0, 0);
                          Cursor.Position = P;
                          if (MG.Turn == MG.P[1] && MG.vsAI)
                          {
                              Thread.Sleep((MG.FastGame) ? 100 : 400);
                              Col_Click(4, true);
                          }
                      }
                  }
              };
        }

        private void HelpPrompt(bool NotGold)
        {
            if(NotGold)
            { Help_PictureBox.Image = Properties.Resources.Help; }
            else
            { Help_PictureBox.Image = Gold_Help; }
        }

        private void TopImgScaleSmall()
        {
            TopPicture.Height = TopImgSize[0];
        }

        private void TopImgScaleBig()
        {
            TopPicture.Height = TopImgSize[1];
        }

        private void HelpResetText()
        { label_Help.Text = "Click on a column to place a token that will drop to the lowest empty space. Win the Game by matching 4 tokens"; }

        private void HelpWinText()
        { label_Help.Text = $"Try playing the Column #{tempInt}, it will create a 4-Token chain and Win you the Game!"; }

        private void HelpLossText()
        { label_Help.Text = $"Playing the Column #{tempInt} will let the AI Win, try playing on this Column #{tempInt2} instead"; }

        private void TLUpdate()
        {
            if (MG.LeftIsRed)
            {
                if (MG.Turn == 0)
                    Turn_Left.Image = Red_Loading;
                else
                    Turn_Left.Image = T_Large_Red_Circle;
            }
            else
            {
                if (MG.Turn == 0)
                    Turn_Left.Image = T_Large_Blue_Circle;
                else
                    Turn_Left.Image = Blue_Loading;
            }
        }

        private void TRUpdate()
        {
            if (MG.LeftIsRed)
            {
                if (MG.Turn == 0)
                    Turn_Right.Image = T_Large_Blue_Circle;
                else
                    Turn_Right.Image = Blue_Loading;
            }
            else
            {
                if (MG.Turn == 0)
                    Turn_Right.Image = Red_Loading;
                else
                    Turn_Right.Image = T_Large_Red_Circle;
            }
        }

        private void UpdateControl()
        {
            lock (tempPBox)
            {
                tempPBox.Image = tempBitmap;
            }
        }

        private void MoveGlow()
        {
            if(CurrentMouseIndex > 0)
                Glow.Location = C[GlowIndex][CurrentMouseIndex].Location; 
        }
        
        //Events

        private void Color_Select_Red_Click(object sender, EventArgs e)
        {
            if (MG.P[0] != 0)
            {
                Color_Select_Blue.Image = T_Large_Blue_Circle;
                Color_Select_Red.Image = Large_Select_Red_Circle;
                button_Start.ForeColor = Color.FromArgb(221, 46, 68);
                button_Start.FlatAppearance.BorderColor = Color.FromArgb(221, 46, 68);
                button_Start.FlatAppearance.MouseOverBackColor = Color.FromArgb(247, 224, 227);
                button_Start.FlatAppearance.MouseDownBackColor = Color.FromArgb(248, 194, 65);
                button_Start.Font = new Font(button_Start.Font.Name, button_Start.Font.Size, FontStyle.Bold);
                button_Start.BackColor = Color.White;
                if (Label_Err_Color.Visible)
                    Label_Err_Color.Hide();
                button_Start.Location = new Point((GameOptions.Width + 10 - button_Start.Width) / 2, GameOptions.Height - GameOptions.Height / ((Label_Err_Color.Visible) ? 9 : 7));
                MG.P[0] = 0; MG.P[1] = 1;
                MG.LeftIsRed = true;
            }
        }

        private void Color_Select_Blue_Click(object sender, EventArgs e)
        {
            if (MG.P[0] != 1)
            {
                Color_Select_Red.Image = T_Large_Red_Circle;
                Color_Select_Blue.Image = Large_Select_Blue_Circle;
                button_Start.ForeColor = Color.FromArgb(86, 172, 238);
                button_Start.FlatAppearance.BorderColor = Color.FromArgb(86, 172, 238);
                button_Start.FlatAppearance.MouseOverBackColor = Color.FromArgb(211, 234, 246);
                button_Start.FlatAppearance.MouseDownBackColor = Color.FromArgb(248, 194, 65);
                button_Start.Font = new Font(button_Start.Font.Name, button_Start.Font.Size, FontStyle.Bold);
                button_Start.BackColor = Color.White;
                if(Label_Err_Color.Visible)
                    Label_Err_Color.Hide();
                button_Start.Location = new Point((GameOptions.Width + 10 - button_Start.Width) / 2, GameOptions.Height - GameOptions.Height / ((Label_Err_Color.Visible) ? 9 : 7));
                MG.P[0] = 1; MG.P[1] = 0;
                MG.LeftIsRed = false;
            }
        }

        private void Start_Click(object sender, EventArgs e)
        {
            if (MG.P[0] != -1)
            {
                button_Start.Location = new Point((GameOptions.Width + 10 - button_Start.Width) / 2, GameOptions.Height - GameOptions.Height / ((Label_Err_Color.Visible) ? 9 : 7));
                LoadingBox.Show();
                MG.Loading = true;
                MG.Finished = false;
                TopPicture.Hide();
                System.Timers.Timer T = new System.Timers.Timer(50);
                T.Start();
                T.Elapsed += (s, E) =>
                {
                    if (!MG.Loading)
                    {
                        T.Dispose();
                        LoadingBox.Invoke(new Action(LoadingBox.Hide));
                        SlideEffect();
                    }
                };
                FormResize(null, null);
                Turn_Left.Invoke(new Action(TLUpdate));
                Turn_Right.Invoke(new Action(TRUpdate));
                if (LearnMCheckBox.Checked && MG.vsAI)
                { Help_PictureBox.Invoke(new Action(Help_PictureBox.Show)); }
                else
                { Help_PictureBox.Invoke(new Action(Help_PictureBox.Hide)); }
                GameOptions.Hide();
                LossHelps = 0;
                if(!MG.vsAI)
                    for (int i = 1; i < 8; i++)
                        C[0][i].Image = ArrowRB[MG.Turn];
                else
                    for (int i = 1; i < 8; i++)
                        C[0][i].Image = ArrowRB[MG.P[0]];
                MG.Loading = false;
            }
            else
            {
                try { Label_Err_Color.Show(); }
                catch (InvalidOperationException) { }
                button_Start.Location = new Point((GameOptions.Width + 10 - button_Start.Width) / 2, GameOptions.Height - GameOptions.Height / ((Label_Err_Color.Visible) ? 9 : 7));
            }
        }

        private void VsAI_Check(object sender, EventArgs e)
        {
            if(AIcheckBox.Checked)
            {
                DebugAI_CheckBox.Checked =
                MG.vsAI =
                AIDiff_Label.Enabled = 
                Label_Easy.Enabled =
                Label_Medium.Enabled =
                Label_Intermediate.Enabled =
                Label_Hard.Enabled =
                Label_Merciless.Enabled =
                PredicitveCheckBox.Enabled = 
                LearnMCheckBox.Enabled = 
                Difficulty_Label.Enabled =
                HumanizedCheckBox.Enabled =
                StrategicCheckBox.Enabled = true;
                DiffBar_Scroll(null, null);
            }
            else
            {
                DebugAI_CheckBox.Checked =
                MG.vsAI =
                AIDiff_Label.Enabled =
                Label_Easy.Enabled =
                Label_Medium.Enabled =
                Label_Intermediate.Enabled =
                Label_Hard.Enabled =
                Label_Merciless.Enabled =
                PredicitveCheckBox.Enabled =
                LearnMCheckBox.Enabled =
                Difficulty_Label.Enabled =
                HumanizedCheckBox.Enabled =
                StrategicCheckBox.Enabled = false;
            }
        }        

        private void FGameChkChanged(object sender, EventArgs e)
        {
            MG.FastGame = FGameCheckBox.Checked;
        }

        private void PredicitveChkChanged(object sender, EventArgs e)
        {
            if (InfiniteLoop) return;
            InfiniteLoop = true;
            if (MG.PredictiveAI = PredicitveCheckBox.Checked)
            {
                DiffBar.Value = Math.Max(50, DiffBar.Value);
                if (DiffBar.Value == 50)
                    DiffBar_Scroll(null, null);
            }
            else
            {
                DiffBar.Value = Math.Min(25, DiffBar.Value);
                if (DiffBar.Value == 25)
                    DiffBar_Scroll(null, null);
            }
            InfiniteLoop = false;
        }

        private void HumanizedChkChanged(object sender, EventArgs e)
        {
            if (InfiniteLoop) return;
            if (MG.HumanizedAI = HumanizedCheckBox.Checked)
            {
                if (DiffBar.Value != 100)
                {
                    DiffBar.Value = 100;
                    DiffBar_Scroll(null, null);
                }
            }
            InfiniteLoop = false;
        }

        private void StrategicChkChanged(object sender, EventArgs e)
        {
            if (InfiniteLoop) return;
            InfiniteLoop = true;
            if (MG.StrategicAI = StrategicCheckBox.Checked)
            {
                DiffBar.Value = Math.Max(75, DiffBar.Value);
                if (DiffBar.Value == 75)
                    DiffBar_Scroll(null, null);
                PredicitveCheckBox.Checked = true;
            }
            else
            {
                DiffBar.Value = Math.Min(50, DiffBar.Value);
                if (DiffBar.Value == 50)
                    DiffBar_Scroll(null, null);
            }
            InfiniteLoop = false;
        }

        private void LearnMCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            InfiniteLoop = true;
            if (LearnMCheckBox.Checked)
            {
                DiffBar.Value = Math.Min(25, DiffBar.Value);
                if (DiffBar.Value == 25)
                    DiffBar_Scroll(null, null);
            }
            InfiniteLoop = false;
        }

        private void Button_Restart_Click(object sender, EventArgs e)
        {
            if (!MG.Finished &&  MG.Moves > 3)
            { 
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to restart an ongoing game?", "Restart?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)            
                    return;
            }
            int[] _P = MG.P;
            int _Diff = MG.Diff;
            bool _vsAI = MG.vsAI;
            bool _FG = MG.FastGame;
            bool _LeftIsRed = MG.LeftIsRed;
            MG = new MainGame()
            { P = _P, Diff = _Diff, vsAI = _vsAI, FastGame = _FG, LeftIsRed = _LeftIsRed,
              StrategicAI = StrategicCheckBox.Checked,
              PredictiveAI = PredicitveCheckBox.Checked,
              HumanizedAI = HumanizedCheckBox.Checked };
            Helps = new int[] { 0, 0 };
            LossHelps = 0;
            UpdateVisuals();
            button_Exit.Hide(); button_Restart.Hide();
            GameOptions.Show();
            Turn_Right.Hide();
            Help_PictureBox.Hide();
            Turn_Left.Hide();
            TopPicture.Image = Properties.Resources.Connect_4;
            TopPicture.Invoke(new Action(TopImgScaleSmall));
            for (int i = 1; i < 8; i++)
            {
                for (int j = 1; j < 7; j++)
                {
                    C[j][i].Hide();
                }
            }
        }

        private void DiffBar_Scroll(object sender, EventArgs e)
        {
            MG.Diff = DiffBar.Value;
            if (!InfiniteLoop)
            {
                InfiniteLoop = true;
                HumanizedCheckBox.Checked = (MG.Diff == 100);
                if (!PredicitveCheckBox.Checked)
                    PredicitveCheckBox.Checked = (MG.Diff >= 50);
                else
                    PredicitveCheckBox.Checked = (MG.Diff >= 25);
                if (!StrategicCheckBox.Checked)
                    StrategicCheckBox.Checked = (MG.Diff >= 75);
                else
                    StrategicCheckBox.Checked = (MG.Diff >= 50);
                if (!LearnMCheckBox.Checked)
                    LearnMCheckBox.Checked = (MG.Diff <= 15);
                else
                    LearnMCheckBox.Checked = (MG.Diff <= 30);
                InfiniteLoop = false;
            }
            if (MG.Diff < 13)
            {
                Difficulty_Label.Text = "Easy";
                Difficulty_Label.ForeColor = Label_Easy.ForeColor = Color.LimeGreen;
                Label_Merciless.ForeColor = Label_Medium.ForeColor = Label_Intermediate.ForeColor = Label_Hard.ForeColor = Color.FromArgb(64, 64, 64);
                return;
            }
            if (MG.Diff < 38)
            {
                Difficulty_Label.Text = "Medium";
                Difficulty_Label.ForeColor = Label_Medium.ForeColor = Color.DodgerBlue;
                Label_Easy.ForeColor = Label_Merciless.ForeColor = Label_Intermediate.ForeColor = Label_Hard.ForeColor = Color.FromArgb(64, 64, 64);
                return;
            }
            if (MG.Diff < 63)
            {
                Difficulty_Label.Text = "Intermediate";
                Difficulty_Label.ForeColor = Label_Intermediate.ForeColor = Color.DarkOrange;
                Label_Easy.ForeColor = Label_Medium.ForeColor = Label_Merciless.ForeColor = Label_Hard.ForeColor = Color.FromArgb(64, 64, 64);
                return;
            }
            if (MG.Diff < 88)
            {
                Difficulty_Label.Text = "Hard";
                Difficulty_Label.ForeColor = Label_Hard.ForeColor = Color.Red;
                Label_Easy.ForeColor = Label_Medium.ForeColor = Label_Intermediate.ForeColor = Label_Merciless.ForeColor = Color.FromArgb(64, 64, 64);
                return;
            }
            Difficulty_Label.Text = "Merciless";
            Difficulty_Label.ForeColor = Label_Merciless.ForeColor = Color.DarkViolet;
            Label_Easy.ForeColor = Label_Medium.ForeColor = Label_Intermediate.ForeColor = Label_Hard.ForeColor = Color.FromArgb(64, 64, 64);
        }

        private void Button_Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Help_Show(object sender, EventArgs e)
        {
            label_Help.Show();
            HelpPrompt(true);
        }

        private void Help_Hide(object sender, EventArgs e)
        {
            label_Help.Hide();
            HelpPrompt(false);
        }

        private void HelpT_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            HelpT.Stop();
            label_Help.Invoke(new Action(HelpResetText));
            label_Help.Invoke(new Action(label_Help.Hide));
            HelpPrompt(false);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #if DEBUG
                DebugAI_CheckBox.Visible = true;
                DebugStateButton.Visible = true;
                label1.Visible = true;
                label3.Visible = true;
                label4.Visible = true;
                label5.Visible = true;
                label6.Visible = true;
                label7.Visible = true;
                label8.Visible = true;
            #endif
            if(Screen.FromControl(this).Bounds.Height < MaximumSize.Height)
            {
                int nms = MaximumSize.Height / Screen.FromControl(this).Bounds.Height;
                MaximumSize = new Size(MaximumSize.Width / nms, MaximumSize.Height / nms);
            }
            System.Timers.Timer T = new System.Timers.Timer(100);
            T.Elapsed += T_Elapsed;
            //T.Start();
            toolTip.SetToolTip(FGameCheckBox, "Fastens animations and lowers delay of AI moves");
            toolTip.SetToolTip(LearnMCheckBox, "Adds Tips to the match with insights on what to play");
            toolTip.SetToolTip(PredicitveCheckBox, "Enables Predicitve moves by the AI");
            toolTip.SetToolTip(StrategicCheckBox, "Enables Strategic moves by the AI, Predictive AI is Obligatory");
            toolTip.SetToolTip(HumanizedCheckBox, "Coming Soon..");
            toolTip.SetToolTip(Label_Easy, "Easy");
            toolTip.SetToolTip(Label_Medium, "Medium");
            toolTip.SetToolTip(Label_Intermediate, "Intermediate");
            toolTip.SetToolTip(Label_Hard, "Hard");
            toolTip.SetToolTip(Label_Merciless, "Merciless");
            toolTip.SetToolTip(TopPicture, "Restart");
            HelpT.Elapsed += HelpT_Elapsed;
            C.Add(new List<PictureBox> { null, C_1_0, C_2_0, C_3_0, C_4_0, C_5_0, C_6_0, C_7_0 });
            C.Add(new List<PictureBox> { null, C_1_1, C_2_1, C_3_1, C_4_1, C_5_1, C_6_1, C_7_1 });
            C.Add(new List<PictureBox> { null, C_1_2, C_2_2, C_3_2, C_4_2, C_5_2, C_6_2, C_7_2 });
            C.Add(new List<PictureBox> { null, C_1_3, C_2_3, C_3_3, C_4_3, C_5_3, C_6_3, C_7_3 });
            C.Add(new List<PictureBox> { null, C_1_4, C_2_4, C_3_4, C_4_4, C_5_4, C_6_4, C_7_4 });
            C.Add(new List<PictureBox> { null, C_1_5, C_2_5, C_3_5, C_4_5, C_5_5, C_6_5, C_7_5 });
            C.Add(new List<PictureBox> { null, C_1_6, C_2_6, C_3_6, C_4_6, C_5_6, C_6_6, C_7_6 });
            FormResize(null, null);
            C_1_0.Click += Col1_Click;
            C_1_1.Click += Col1_Click;
            C_1_2.Click += Col1_Click;
            C_1_3.Click += Col1_Click;
            C_1_4.Click += Col1_Click;
            C_1_5.Click += Col1_Click;
            C_1_6.Click += Col1_Click;
            C_2_0.Click += Col2_Click;
            C_2_1.Click += Col2_Click;
            C_2_2.Click += Col2_Click;
            C_2_3.Click += Col2_Click;
            C_2_4.Click += Col2_Click;
            C_2_5.Click += Col2_Click;
            C_2_6.Click += Col2_Click;
            C_3_0.Click += Col3_Click;
            C_3_1.Click += Col3_Click;
            C_3_2.Click += Col3_Click;
            C_3_3.Click += Col3_Click;
            C_3_4.Click += Col3_Click;
            C_3_5.Click += Col3_Click;
            C_3_6.Click += Col3_Click;
            C_4_0.Click += Col4_Click;
            C_4_1.Click += Col4_Click;
            C_4_2.Click += Col4_Click;
            C_4_3.Click += Col4_Click;
            C_4_4.Click += Col4_Click;
            C_4_5.Click += Col4_Click;
            C_4_6.Click += Col4_Click;
            C_5_0.Click += Col5_Click;
            C_5_1.Click += Col5_Click;
            C_5_2.Click += Col5_Click;
            C_5_3.Click += Col5_Click;
            C_5_4.Click += Col5_Click;
            C_5_5.Click += Col5_Click;
            C_5_6.Click += Col5_Click;
            C_6_0.Click += Col6_Click;
            C_6_1.Click += Col6_Click;
            C_6_2.Click += Col6_Click;
            C_6_3.Click += Col6_Click;
            C_6_4.Click += Col6_Click;
            C_6_5.Click += Col6_Click;
            C_6_6.Click += Col6_Click;
            C_7_0.Click += Col7_Click;
            C_7_1.Click += Col7_Click;
            C_7_2.Click += Col7_Click;
            C_7_3.Click += Col7_Click;
            C_7_4.Click += Col7_Click;
            C_7_5.Click += Col7_Click;
            C_7_6.Click += Col7_Click;
            C_1_0.MouseEnter += Col1_Enter;
            C_1_1.MouseEnter += Col1_Enter;
            C_1_2.MouseEnter += Col1_Enter;
            C_1_3.MouseEnter += Col1_Enter;
            C_1_4.MouseEnter += Col1_Enter;
            C_1_5.MouseEnter += Col1_Enter;
            C_1_6.MouseEnter += Col1_Enter;
            C_2_0.MouseEnter += Col2_Enter;
            C_2_1.MouseEnter += Col2_Enter;
            C_2_2.MouseEnter += Col2_Enter;
            C_2_3.MouseEnter += Col2_Enter;
            C_2_4.MouseEnter += Col2_Enter;
            C_2_5.MouseEnter += Col2_Enter;
            C_2_6.MouseEnter += Col2_Enter;
            C_3_0.MouseEnter += Col3_Enter;
            C_3_1.MouseEnter += Col3_Enter;
            C_3_2.MouseEnter += Col3_Enter;
            C_3_3.MouseEnter += Col3_Enter;
            C_3_4.MouseEnter += Col3_Enter;
            C_3_5.MouseEnter += Col3_Enter;
            C_3_6.MouseEnter += Col3_Enter;
            C_4_0.MouseEnter += Col4_Enter;
            C_4_1.MouseEnter += Col4_Enter;
            C_4_2.MouseEnter += Col4_Enter;
            C_4_3.MouseEnter += Col4_Enter;
            C_4_4.MouseEnter += Col4_Enter;
            C_4_5.MouseEnter += Col4_Enter;
            C_4_6.MouseEnter += Col4_Enter;
            C_5_0.MouseEnter += Col5_Enter;
            C_5_1.MouseEnter += Col5_Enter;
            C_5_2.MouseEnter += Col5_Enter;
            C_5_3.MouseEnter += Col5_Enter;
            C_5_4.MouseEnter += Col5_Enter;
            C_5_5.MouseEnter += Col5_Enter;
            C_5_6.MouseEnter += Col5_Enter;
            C_6_0.MouseEnter += Col6_Enter;
            C_6_1.MouseEnter += Col6_Enter;
            C_6_2.MouseEnter += Col6_Enter;
            C_6_3.MouseEnter += Col6_Enter;
            C_6_4.MouseEnter += Col6_Enter;
            C_6_5.MouseEnter += Col6_Enter;
            C_6_6.MouseEnter += Col6_Enter;
            C_7_0.MouseEnter += Col7_Enter;
            C_7_1.MouseEnter += Col7_Enter;
            C_7_2.MouseEnter += Col7_Enter;
            C_7_3.MouseEnter += Col7_Enter;
            C_7_4.MouseEnter += Col7_Enter;
            C_7_5.MouseEnter += Col7_Enter;
            C_7_6.MouseEnter += Col7_Enter;
            C_1_0.MouseLeave += Col1_Leave;
            C_1_1.MouseLeave += Col1_Leave;
            C_1_2.MouseLeave += Col1_Leave;
            C_1_3.MouseLeave += Col1_Leave;
            C_1_4.MouseLeave += Col1_Leave;
            C_1_5.MouseLeave += Col1_Leave;
            C_1_6.MouseLeave += Col1_Leave;
            C_2_0.MouseLeave += Col2_Leave;
            C_2_1.MouseLeave += Col2_Leave;
            C_2_2.MouseLeave += Col2_Leave;
            C_2_3.MouseLeave += Col2_Leave;
            C_2_4.MouseLeave += Col2_Leave;
            C_2_5.MouseLeave += Col2_Leave;
            C_2_6.MouseLeave += Col2_Leave;
            C_3_0.MouseLeave += Col3_Leave;
            C_3_1.MouseLeave += Col3_Leave;
            C_3_2.MouseLeave += Col3_Leave;
            C_3_3.MouseLeave += Col3_Leave;
            C_3_4.MouseLeave += Col3_Leave;
            C_3_5.MouseLeave += Col3_Leave;
            C_3_6.MouseLeave += Col3_Leave;
            C_4_0.MouseLeave += Col4_Leave;
            C_4_1.MouseLeave += Col4_Leave;
            C_4_2.MouseLeave += Col4_Leave;
            C_4_3.MouseLeave += Col4_Leave;
            C_4_4.MouseLeave += Col4_Leave;
            C_4_5.MouseLeave += Col4_Leave;
            C_4_6.MouseLeave += Col4_Leave;
            C_5_0.MouseLeave += Col5_Leave;
            C_5_1.MouseLeave += Col5_Leave;
            C_5_2.MouseLeave += Col5_Leave;
            C_5_3.MouseLeave += Col5_Leave;
            C_5_4.MouseLeave += Col5_Leave;
            C_5_5.MouseLeave += Col5_Leave;
            C_5_6.MouseLeave += Col5_Leave;
            C_6_0.MouseLeave += Col6_Leave;
            C_6_1.MouseLeave += Col6_Leave;
            C_6_2.MouseLeave += Col6_Leave;
            C_6_3.MouseLeave += Col6_Leave;
            C_6_4.MouseLeave += Col6_Leave;
            C_6_5.MouseLeave += Col6_Leave;
            C_6_6.MouseLeave += Col6_Leave;
            C_7_0.MouseLeave += Col7_Leave;
            C_7_1.MouseLeave += Col7_Leave;
            C_7_2.MouseLeave += Col7_Leave;
            C_7_3.MouseLeave += Col7_Leave;
            C_7_4.MouseLeave += Col7_Leave;
            C_7_5.MouseLeave += Col7_Leave;
            C_7_6.MouseLeave += Col7_Leave;
        }

        private void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(CurrentMouseIndex == 0)
            {
                Glow.Invoke(new Action(Glow.Hide));
                GlowIndex = 1;
            }
            else if(GlowIndex <= MG.GetLow(CurrentMouseIndex))
            {
                Glow.Invoke(new Action(Glow.Show));
                Glow.Invoke(new Action(MoveGlow));
                GlowIndex++;
            }
            else
            {
                GlowIndex = 1;
            }
        }

        private void Label_Easy_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 0;
            DiffBar_Scroll(null, null);
        }

        private void Label_Medium_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 25;
            DiffBar_Scroll(null, null);
        }

        private void Label_Intermediate_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 50;
            DiffBar_Scroll(null, null);
        }

        private void Label_Hard_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 75;
            DiffBar_Scroll(null, null);
        }

        private void Label_Merciless_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 100;
            DiffBar_Scroll(null, null);
        }

        //Click, Hover, Leave Events

        private void Col1_Click(object sender, EventArgs e)
        { Col_Click(1); }
        private void Col2_Click(object sender, EventArgs e)
        { Col_Click(2); }
        private void Col3_Click(object sender, EventArgs e)
        { Col_Click(3); }
        private void Col4_Click(object sender, EventArgs e)
        { Col_Click(4); }
        private void Col5_Click(object sender, EventArgs e)
        { Col_Click(5); }
        private void Col6_Click(object sender, EventArgs e)
        { Col_Click(6); }
        private void Col7_Click(object sender, EventArgs e)
        { Col_Click(7); }

        private void Col1_Enter(object sender, EventArgs e)
        { Col_Enter(1); CurrentMouseIndex = 1; }
        private void Col2_Enter(object sender, EventArgs e)
        { Col_Enter(2); CurrentMouseIndex = 2; }
        private void Col3_Enter(object sender, EventArgs e)
        { Col_Enter(3); CurrentMouseIndex = 3; }
        private void Col4_Enter(object sender, EventArgs e)
        { Col_Enter(4); CurrentMouseIndex = 4; }
        private void Col5_Enter(object sender, EventArgs e)
        { Col_Enter(5); CurrentMouseIndex = 5; }
        private void Col6_Enter(object sender, EventArgs e)
        { Col_Enter(6); CurrentMouseIndex = 6; }
        private void Col7_Enter(object sender, EventArgs e)
        { Col_Enter(7); CurrentMouseIndex = 7; }

        private void Col1_Leave(object sender, EventArgs e)
        { Col_Leave(1); CurrentMouseIndex = 0; }
        private void Col2_Leave(object sender, EventArgs e)
        { Col_Leave(2); CurrentMouseIndex = 0; }
        private void Col3_Leave(object sender, EventArgs e)
        { Col_Leave(3); CurrentMouseIndex = 0; }
        private void Col4_Leave(object sender, EventArgs e)
        { Col_Leave(4); CurrentMouseIndex = 0; }
        private void Col5_Leave(object sender, EventArgs e)
        { Col_Leave(5); CurrentMouseIndex = 0; }
        private void Col6_Leave(object sender, EventArgs e)
        { Col_Leave(6); CurrentMouseIndex = 0; }
        private void Col7_Leave(object sender, EventArgs e)
        { Col_Leave(7); CurrentMouseIndex = 0; }

        private int HWDiff(int W, int H)
        {
            H = (int)(H / 1.411);
            int w = W-425; int h = H - 425;
            if (w > h)
                return W - w + h;
            return H - h + w;
        }

        private void FormResize(object sender, EventArgs e)
        {  // Max : 725, 950
            if(ActiveForm != null)
                lock (ActiveForm)
                { ResizeForm(); }
            else
                ResizeForm();
        }

        private void ResizeForm()
        {  // Max : 725, 950
            int W, H, GOW, GOH, CS; int[] CBL;
            try
            { W = ActiveForm.Width; H = ActiveForm.Height; if (W < ActiveForm.MinimumSize.Width) return; }
            catch (Exception) { W = 425; H = 600; }
            TopImgSize[0] = (int)(H * 0.166);
            TopImgSize[1] = (int)(H * 0.2383);

            {   //In-Game
                CS = (HWDiff(W, H)) / 13 + 10;
                CBL = new int[2] { (W / 2 - (CS * 7 - 7) / 2 - 6), (int)((30 + ((H - TopImgSize[0]) / 2 - (CS * 7 - 7) / 2 - 6) + TopImgSize[0]) * ((LearnMCheckBox.Checked && AIcheckBox.Checked) ? .9 : 1)) };
                for (int x = 1; x < 8; x++)
                {
                    for (int y = 0; y < 7; y++)
                    {
                        lock (C[y][x])
                        {
                            C[y][x].Location = new Point(CBL[0] + CS * (x - 1), CBL[1] + CS * y);
                            C[y][x].Size = new Size(CS, CS);
                        }
                    }
                }
                Help_PictureBox.Size = new Size((int)(HWDiff(W, H) / 14.16), (int)(HWDiff(W, H) / 14.16));
                label_Help.Font = new Font("Century Gothic", HWDiff(W, H) / 47, FontStyle.Italic);
                label_Help.Location = new Point(W / 21 + Help_PictureBox.Width, H - CS - label_Help.Height);
                label_Help.Width = W - (W / 20) - label_Help.Location.X + 10;
                Help_PictureBox.Location = new Point(W / 42, H - CS - (int)(label_Help.Height / 1.33) - (int)((Help_PictureBox.Height - 30) / 2));
                button_Exit.Height = button_Restart.Height = button_Start.Height = (H - 600) / 25 + 45;
                button_Exit.Width = button_Restart.Width = button_Start.Width = (W - 425) / 6 + 100;
                button_Exit.Font = button_Start.Font = button_Restart.Font = new Font("Century Gothic", (float)(4.25 + HWDiff(W, H) / 42.5), FontStyle.Bold);
                if (MG.P[0] == -1)
                    button_Start.Font = new Font("Century Gothic", (float)(4.25 + HWDiff(W, H) / 42.5));
                button_Exit.Location = new Point((W - button_Exit.Width) * 3 / 4, (int)((((CBL[1] + CS - TopImgSize[1]) / 2.85) + TopImgSize[1]) * ((LearnMCheckBox.Checked && AIcheckBox.Checked) ? .9 : 1)));
                button_Restart.Location = new Point((W - button_Exit.Width) / 4, (int)((((CBL[1] + CS - TopImgSize[1]) / 2.85) + TopImgSize[1]) * ((LearnMCheckBox.Checked && AIcheckBox.Checked) ? .9 : 1)));
                Turn_Left.Height = Turn_Left.Width = Turn_Right.Height = Turn_Right.Width = (int)(1.5 * CS);
                Turn_Right.Location = new Point((W - Turn_Right.Height) * 3 / 4, (int)((((CBL[1] - TopImgSize[0]) / 2.85) + TopImgSize[0]) * ((LearnMCheckBox.Checked && AIcheckBox.Checked) ? .9 : 1)));
                Turn_Left.Location = new Point((W - Turn_Right.Height) / 4, (int)((((CBL[1] - TopImgSize[0]) / 2.85) + TopImgSize[0]) * ((LearnMCheckBox.Checked && AIcheckBox.Checked) ? .9 : 1)));
#if DEBUG
                label1.Location = new Point(CBL[0] + CS * (1 - 1) + C[1][1].Height / 3, CBL[1] + CS * 7);
                label3.Location = new Point(CBL[0] + CS * (2 - 1) + C[1][1].Height / 3, CBL[1] + CS * 7);
                label4.Location = new Point(CBL[0] + CS * (3 - 1) + C[1][1].Height / 3, CBL[1] + CS * 7);
                label5.Location = new Point(CBL[0] + CS * (4 - 1) + C[1][1].Height / 3, CBL[1] + CS * 7);
                label6.Location = new Point(CBL[0] + CS * (5 - 1) + C[1][1].Height / 3, CBL[1] + CS * 7);
                label7.Location = new Point(CBL[0] + CS * (6 - 1) + C[1][1].Height / 3, CBL[1] + CS * 7);
                label8.Location = new Point(CBL[0] + CS * (7 - 1) + C[1][1].Height / 3, CBL[1] + CS * 7);
#endif
            }

            {   //Main Menu
                GOW = Math.Min(660, W - 40);
                GOH = (int)(H * 1.05) - ((H + 24) / 11) - 27 - TopImgSize[0];
                TopPicture.Width = W - 40;
                LoadingBox.Size = new Size(W, H);
                LoadingBox.Location = new Point(0, 0);
                GameOptions.Width = GOW; GOW += 40;
                GameOptions.Height = GOH;
                TopPicture.Height = TopImgSize[(MG.Finished && !GameOptions.Visible) ? 1 : 0];
                FGameCheckBox.Location = new Point(FGameCheckBox.Location.X, GOH / 15 + 6);
                GameOptions.Location = new Point(Math.Max((W - 670) / 2, 12), 6 + TopImgSize[0]);
                W = Math.Min(700, W);
                Color_Select_Blue.Size = Color_Select_Red.Size = new Size((int)(HWDiff(W, H) / 3.5), (int)(HWDiff(W, H) / 3.5));
                Color_Select_Red.Location = new Point((int)(0.9 * ((GOW - 40 - Color_Select_Red.Width) / 4)), GOH / 2 + GOH / 16 - 10);
                Color_Select_Blue.Location = new Point((int)(1.1 * ((GOW - 40 - Color_Select_Red.Width) * 3 / 4)), GOH / 2 + GOH / 16 - 10);
                Label_Err_Color.Location = new Point((GOW - 40) / 2 - 86, GOH - (int)(GOH / 5.89));
                button_Start.Location = new Point((GOW - 30 - button_Start.Width) / 2, GOH - GOH / ((Label_Err_Color.Visible) ? 9 : 7));
                Color_Label.Location = new Point((int)((W - 370) * .2) + 15, GOH / 2);
                DiffBar.Width = W - 104;
                Label_Easy.Font = Label_Hard.Font = Label_Medium.Font = Label_Merciless.Font = Label_Intermediate.Font = new Font("Century Gothic", (float)(7.25 + HWDiff(W, H) / 42.5));
                int d = DiffBar.Width;
                Label_Merciless.Location = new Point(W - 98 - (Label_Easy.Width - 31) / 2, GOH / 4 + 10 + 48);
                Label_Easy.Location = new Point(DiffBar.Location.X - (Label_Easy.Width - 31) / 2, GOH / 4 + 10 + 48);
                Label_Hard.Location = new Point(DiffBar.Location.X + 1 + (int)((Label_Hard.Width - 31) * .3) + (d - Label_Easy.Width + 4) / 4 * 3, GOH / 4 + 10 + 48);
                Label_Medium.Location = new Point(DiffBar.Location.X - (int)((Label_Medium.Width - 31) * .3) + (d - Label_Easy.Width + 4) / 4, GOH / 4 + 10 + 48);
                Label_Intermediate.Location = new Point(DiffBar.Location.X + (d - Label_Easy.Width + 4) / 2, GOH / 4 + 10 + 48);
                Color_Label.Font = AIcheckBox.Font = new Font("Century Gothic", (float)(6.5 + HWDiff(W, H) / 63.75));
                Difficulty_Label.Font = HumanizedCheckBox.Font = PredicitveCheckBox.Font = StrategicCheckBox.Font = new Font("Century Gothic", (float)(3.75 + HWDiff(W, H) / 63.75));
                AIDiff_Label.Font = LearnMCheckBox.Font = FGameCheckBox.Font = new Font("Century Gothic", (float)(4.5 + HWDiff(W, H) / 63.75));
                Difficulty_Label.Location = new Point((int)(-18 + 1.45 * AIDiff_Label.Width), GOH / 4 + 69 - 3 * Difficulty_Label.Height);
                AIDiff_Label.Location = new Point(25, GOH / 4 + 60 - (int)(2.5 * AIDiff_Label.Height));
                DiffBar.Location = new Point(DiffBar.Location.X, GOH / 4 + 10 + 25);
                int p = GOH / 15 + 6, p1 = (int)(GameOptions.Width * .965) - HumanizedCheckBox.Width - (AIcheckBox.Width / 5);
                AIcheckBox.Location = new Point(p1, p); p += (int)(AIcheckBox.Height * 1.12);
                PredicitveCheckBox.Location = new Point(p1 + AIcheckBox.Width / 5, p); p += (int)(1.5 * PredicitveCheckBox.Height - 11.5);
                StrategicCheckBox.Location = new Point(p1 + AIcheckBox.Width / 5, p); p += (int)(1.5 * StrategicCheckBox.Height - 11.5);
                HumanizedCheckBox.Location = new Point(p1 + AIcheckBox.Width / 5, p);
                if (AIcheckBox.Location.X - (FGameCheckBox.Width + FGameCheckBox.Location.X) > (int)((FGameCheckBox.Width + FGameCheckBox.Location.X) * 1.3))
                    LearnMCheckBox.Location = new Point((int)((FGameCheckBox.Width + FGameCheckBox.Location.X) * 1.3), GOH / 15 + 6);
                else
                    LearnMCheckBox.Location = new Point(30, GOH / 12 + 30);
            }            
        }

        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MAXIMIZE = 0xF030;
        const int SC_MINIMIZE = 0xF020;
        Size OldSize = new Size(425, 600);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND)
            {
                switch ((int)m.WParam)
                {
                    case SC_MAXIMIZE:
                        if (ActiveForm.Size.Equals(ActiveForm.MaximumSize))
                            ActiveForm.Size = OldSize;
                        else
                        {
                            OldSize = ActiveForm.Size;
                            ActiveForm.Size = ActiveForm.MaximumSize;
                            if (ActiveForm.DesktopLocation.Y + ActiveForm.MaximumSize.Height > Screen.FromControl(this).Bounds.Height)
                                ActiveForm.Location = new Point(ActiveForm.Location.X, Screen.FromControl(this).Bounds.Height - ActiveForm.MaximumSize.Height);
                        }
                        FormResize(null, null);
                        return;
                }
            }
            base.WndProc(ref m);
        }

        private void DebugAI_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
#if DEBUG
            MG.vsAI = DebugAI_CheckBox.Checked;
#endif
        }
        private void DebugStateButton_Click(object sender, EventArgs e)
        {
#if DEBUG
            MG.PlayAI();
            tempLabel = label1; tempInt = 1; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label3; tempInt = 2; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label4; tempInt = 3; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label5; tempInt = 4; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label6; tempInt = 5; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label7; tempInt = 6; tempLabel.Invoke(new Action(UpdateDebugState));
            tempLabel = label8; tempInt = 7; tempLabel.Invoke(new Action(UpdateDebugState));
#endif
        }
        private void UpdateDebugState()
        {
#if DEBUG
            int i = 0;
            string s = "";
            foreach(int a in MG.AIState[tempInt])
            {
                if (MG.AIState[tempInt].Count > 1 && a == 0) { }
                else {
                    if (i == 2)
                    { i = 0; s += "\n"; }
                    s += "'"+a+"'";
                    i++;
                }
            }
            lock (tempLabel)
                tempLabel.Text = s + "\n|" + MG.AISeverity[tempInt].ToString()+"|";
#endif
        }

    }
}
