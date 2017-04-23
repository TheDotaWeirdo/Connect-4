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
        int CurrentMouseIndex = 0;
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
#if DEBUG
        Label tempLabel;
#endif
        int tempInt, tempInt2, LossHelps = 0;
        int[] Helps = { 0, 0 };
        System.Timers.Timer HelpT = new System.Timers.Timer();

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
                    else if (Helps[0] == 0 && Helps[1] != 0 && Helps[1] != index && LossHelps < 5 - MG.Diff && !AImove)
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
                System.Timers.Timer T = new System.Timers.Timer((FGameCheckBox.Checked) ? 17.5 : 27.5);
                MG.Case[Cap][index] = MG.Turn + 1;
                MG.State[index] = -1;
                MG.Busy = true;
                tempBitmap = W;
                tempPBox = C[Cap][index];
                C[Cap][index].Invoke(new Action(UpdateControl));
                lock (C[0][index])
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
                    { IsBackground = true, Priority = ThreadPriority.BelowNormal };
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
            if (MG.CheckWin() > 0)
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
                    Helps[0] = MG.CheckPossibleWin();
                    Helps[1] = MG.CheckPossibleLoss();
                }
                MG.Busy = false;
                Thread.Sleep(35);
                if (CurrentMouseIndex > 0 && MG.GetLow(CurrentMouseIndex) > 0 && (MG.Turn == MG.P[0] || !MG.vsAI))
                    Col_Enter(CurrentMouseIndex, true);
            }
        }

        private void UpdateVisuals()
        {
            for (int x = 1; x < 7; x++)
            {
                for (int y = 1; y < 8; y++)
                {
                    try
                    {
                        lock (C[x][y])
                        {
                            if (MG.Case[x][y] >= 0)
                            {
                                if ((Bitmap)C[x][y].Image != Case[MG.Case[x][y]] && x > 0)
                                    C[x][y].Image = Case[MG.Case[x][y]];
                            }
                            else if ((Bitmap)C[x][y].Image != TRB[-1 - MG.Case[x][y]] && x > 0)
                                C[x][y].Image = TRB[-1 - MG.Case[x][y]];
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private void SlideEffect()
        {
            if (FGameCheckBox.Checked)
            {
                for (int i = 1; i < 8; i++)
                {
                    for (int j = 1; j < 7; j++)
                    {
                        C[j][i].Invoke(new Action(C[j][i].Show));
                    }
                }
                Turn_Right.Invoke(new Action(Turn_Right.Show));
                Turn_Left.Invoke(new Action(Turn_Left.Show));
                MG.Busy = false;
                Point P = Cursor.Position;
                Cursor.Position = new Point(0, 0);
                Cursor.Position = P;
                if (MG.Turn == MG.P[1] && MG.vsAI)
                    Col_Click(MG.PlayAI(), true);
            }
            else
            {
                int[] index = { 5, 3 };
                System.Timers.Timer Timer = new System.Timers.Timer(150);
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
        }

        private void SlideEffect(int index)
        {
            int ind = 7;
            System.Timers.Timer Timer = new System.Timers.Timer(27.5);
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
                          Thread.Sleep(85);
                          Turn_Right.Invoke(new Action(Turn_Right.Show));
                          Turn_Left.Invoke(new Action(Turn_Left.Show));
                          MG.Busy = false;
                          Point P = Cursor.Position;
                          Cursor.Position = new Point(0, 0);
                          Cursor.Position = P;
                          if (MG.Turn == MG.P[1] && MG.vsAI)
                          {
                              Thread.Sleep(400);
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
            TopPicture.Height = 100;
        }

        private void TopImgScaleBig()
        {
            TopPicture.Height = 155;
        }

        private void HelpResetText()
        { label_Help.Text = "Click on a column to place a token that will drop to the lowest empty space. Win the Game by matching 4 tokens"; }

        private void HelpWinText()
        { label_Help.Text = $"Try playing the Column #{tempInt}, it will create a 4-Token chain and Win you the Game!"; }

        private void HelpLossText()
        { label_Help.Text = $"Playing the Column #{tempInt} will let the AI Win, try playing on this Column #{tempInt2} instead"; }

        private void TLUpdate()
        {
            if (MG.P[0] == 0)
            {
                if (MG.Turn == 0)
                    Turn_Left.Image = Large_Gold_Red_Circle;
                else
                    Turn_Left.Image = T_Large_Red_Circle;
            }
            else
            {
                if (MG.Turn == 0)
                    Turn_Left.Image = T_Large_Blue_Circle;
                else
                    Turn_Left.Image = Large_Gold_Blue_Circle;
            }
        }

        private void TRUpdate()
        {
            if (MG.P[0] == 0)
            {
                if (MG.Turn == 0)
                    Turn_Right.Image = T_Large_Blue_Circle;
                else
                    Turn_Right.Image = Large_Gold_Blue_Circle;
            }
            else
            {
                if (MG.Turn == 0)
                    Turn_Right.Image = Large_Gold_Red_Circle;
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

#if DEBUG
        private void UpdateDebugState()
        {
            lock (tempLabel)
                tempLabel.Text = MG.State[tempInt].ToString();
        }
#endif
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
                MG.P[0] = 0; MG.P[1] = 1;
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
                MG.P[0] = 1; MG.P[1] = 0;
            }
        }

        private void Start_Click(object sender, EventArgs e)
        {
            if (MG.P[0] != -1)
            {
                LoadingBox.Show();
                System.Timers.Timer T = new System.Timers.Timer(500);
                T.Start();
                T.Elapsed += (s, E) =>
                {
                    T.Dispose();
                    LoadingBox.Invoke(new Action(LoadingBox.Hide));
                    SlideEffect();
                };
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
            }
            else
            {
                try { Label_Err_Color.Show(); }
                catch (InvalidOperationException) { }
            }
        }

        private void VsAI_Check(object sender, EventArgs e)
        {
            if(AIcheckBox.Checked)
            {
                MG.vsAI =
                AIDiff_Label.Enabled = 
                Label_Easy.Enabled =
                Label_Medium.Enabled =
                Label_Intermediate.Enabled =
                Label_Hard.Enabled =
                Label_Impossible.Enabled =
                PredicitveCheckBox.Enabled = 
                LearnMCheckBox.Enabled = 
                StrategicCheckBox.Enabled = true;
                DiffBar_Scroll(null, null);
            }
            else
            {
                MG.vsAI =
                AIDiff_Label.Enabled =
                Label_Easy.Enabled =
                Label_Medium.Enabled =
                Label_Intermediate.Enabled =
                Label_Hard.Enabled =
                Label_Impossible.Enabled =
                PredicitveCheckBox.Enabled =
                LearnMCheckBox.Enabled =
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
            MG.PredicitveAI = PredicitveCheckBox.Checked;
            if (PredicitveCheckBox.Checked)
            {
                InfiniteLoop = true;
                DiffBar.Value = Math.Max(25, DiffBar.Value);
                if (DiffBar.Value == 25)
                    DiffBar_Scroll(null, null);
                PredicitveCheckBox.Checked = true;
                InfiniteLoop = false;
            }
            else
            {
                StrategicCheckBox.Checked = false;
                DiffBar.Value = Math.Min(50, DiffBar.Value);
                if (DiffBar.Value == 50)
                    DiffBar_Scroll(null, null);
            }
        }

        private void StrategicChkChanged(object sender, EventArgs e)
        {
            MG.StrategicAI = StrategicCheckBox.Checked;
            if (StrategicCheckBox.Checked)
            {
                PredicitveCheckBox.Checked = true;
                DiffBar.Value = Math.Max(75, DiffBar.Value);
                if (DiffBar.Value == 75)
                    DiffBar_Scroll(null, null);
            }
            else
            {
                DiffBar.Value = Math.Min(75, DiffBar.Value);
                if (DiffBar.Value == 75)
                    DiffBar_Scroll(null, null);
            }
        }

        private void LearnMCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (LearnMCheckBox.Checked)
            {
                DiffBar.Value = Math.Min(25, DiffBar.Value);
                if (DiffBar.Value == 25)
                    DiffBar_Scroll(null, null);
            }
        }

        private void Button_Restart_Click(object sender, EventArgs e)
        {
            int[] _P = MG.P;
            int _Diff = MG.Diff;
            bool _vsAI = MG.vsAI;
            bool _FG = MG.FastGame;
            MG = new MainGame()
            { P = _P, Diff = _Diff, vsAI = _vsAI, FastGame = _FG };
            Helps = new int[] { 0, 0 };
            LossHelps = 0;
            MG.StrategicAI = StrategicCheckBox.Checked;
            MG.PredicitveAI = PredicitveCheckBox.Checked;
            UpdateVisuals();
            button_Exit.Hide(); button_Restart.Hide();
            GameOptions.Show();
            Turn_Right.Hide();
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
            if (MG.Diff < 13)
            {
                LearnMCheckBox.Checked = true;
                PredicitveCheckBox.Checked = false;
                StrategicCheckBox.Checked = false;
                return;
            }
            if (MG.Diff < 38)
            {
                if (MG.Diff < 25)
                    PredicitveCheckBox.Checked = false;
                StrategicCheckBox.Checked = false;
                return;
            }
            if (MG.Diff < 63)
            {
                LearnMCheckBox.Checked = false;
                StrategicCheckBox.Checked = false;
                return;
            }
            if (MG.Diff < 88)
            {
                LearnMCheckBox.Checked = false;
                PredicitveCheckBox.Checked = true;
                return;
            }
            LearnMCheckBox.Checked = false;
            PredicitveCheckBox.Checked = true;
            StrategicCheckBox.Checked = true;
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
                label1.Visible = true;
                label3.Visible = true;
                label4.Visible = true;
                label5.Visible = true;
                label6.Visible = true;
                label7.Visible = true;
                label8.Visible = true;
            #endif
            toolTip.SetToolTip(FGameCheckBox, "Fastens animations and lowers delay of AI moves");
            toolTip.SetToolTip(LearnMCheckBox, "Adds Tips to the match with insights on what to play");
            toolTip.SetToolTip(PredicitveCheckBox, "Enables Predicitve moves by the AI");
            toolTip.SetToolTip(StrategicCheckBox, "Enables Strategic moves by the AI, Predictive AI is Obligatory");
            toolTip.SetToolTip(Label_Easy, "Easy");
            toolTip.SetToolTip(Label_Medium, "Medium");
            toolTip.SetToolTip(Label_Intermediate, "Intermediate");
            toolTip.SetToolTip(Label_Hard, "Hard");
            toolTip.SetToolTip(Label_Impossible, "Impossible");
            toolTip.SetToolTip(TopPicture, "Restart");
            HelpT.Elapsed += HelpT_Elapsed;
            C.Add(new List<PictureBox> { null, C_1_0, C_2_0, C_3_0, C_4_0, C_5_0, C_6_0, C_7_0 });
            C.Add(new List<PictureBox> { null, C_1_1, C_2_1, C_3_1, C_4_1, C_5_1, C_6_1, C_7_1 });
            C.Add(new List<PictureBox> { null, C_1_2, C_2_2, C_3_2, C_4_2, C_5_2, C_6_2, C_7_2 });
            C.Add(new List<PictureBox> { null, C_1_3, C_2_3, C_3_3, C_4_3, C_5_3, C_6_3, C_7_3 });
            C.Add(new List<PictureBox> { null, C_1_4, C_2_4, C_3_4, C_4_4, C_5_4, C_6_4, C_7_4 });
            C.Add(new List<PictureBox> { null, C_1_5, C_2_5, C_3_5, C_4_5, C_5_5, C_6_5, C_7_5 });
            C.Add(new List<PictureBox> { null, C_1_6, C_2_6, C_3_6, C_4_6, C_5_6, C_6_6, C_7_6 });
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

#if DEBUG
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            MG.vsAI = DebugAI_CheckBox.Checked;
        }
#endif
    }
}
