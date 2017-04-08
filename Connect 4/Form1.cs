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

namespace Connect_4
{
    public partial class Form1 : Form
    {
        int CurrentMouseIndex = 0;
        public MainGame MG = new MainGame();
        public List<List<PictureBox>> C = new List<List<PictureBox>>();
        public Bitmap W = Properties.Resources.White_Circle;
        public Bitmap A = Properties.Resources.Arrow;
        public Bitmap[] LG = { Properties.Resources.Large_Gold_Red_Circle, Properties.Resources.Large_Gold_Blue_Circle };
        public Bitmap[] L = { Properties.Resources.Large_Red_Circle, Properties.Resources.Large_Blue_Circle };
        public Bitmap[] G = { Properties.Resources.Gold_Red_Circle, Properties.Resources.Gold_Blue_Circle };
        public Bitmap[] RB = { Properties.Resources.Red_Circle, Properties.Resources.Blue_Circle };
        public Bitmap[] TRB = { Properties.Resources.T_Red_Circle, Properties.Resources.T_Blue_Circle };
        public Bitmap[] Case = { Properties.Resources.White_Circle, Properties.Resources.Red_Circle, Properties.Resources.Blue_Circle, Properties.Resources.Gold_Red_Circle, Properties.Resources.Gold_Blue_Circle };
        delegate void ShowArrowDelegate();
        int tempInt, tempInt2, LossHelps = 0;
        int[] Helps = { 0, 0 };
        System.Timers.Timer HelpT = new System.Timers.Timer();

        public Form1()
        {
            InitializeComponent();
        }

        private void Col_Click(int index, bool AImove = false)
        {
            int i = 1, cap = MG.GetLow(index);
            if (cap > 0 && MG.Winner == 0 && (!MG.vsAI || MG.Turn == MG.P[0] || AImove))
            {
                if (LearnMCheckBox.Checked && MG.vsAI)
                {
                    if (Helps[0] != 0 && Helps[0] != index)
                    {
                        Cursor.Position = new Point(C[2][Helps[0]].Location.X + ActiveForm.Location.X + 25, 10 + C[2][Helps[0]].Location.Y + ActiveForm.Location.Y);
                        LossHelps++;
                        tempInt = Helps[0];
                        label_Help.Invoke(new Action(HelpWinText));
                        label_Help.Invoke(new Action(label_Help.Show));
                        HelpT.Stop();
                        HelpT.Interval = 7500;
                        HelpT.Start();
                        HelpPrompt(true);
                        return;
                    }
                    else if (Helps[1] != 0 && Helps[1] != index && LossHelps < 5 - MG.Diff && !AImove)
                    {
                        Cursor.Position = new Point(C[2][Helps[1]].Location.X + ActiveForm.Location.X + 25, 10 + C[2][Helps[1]].Location.Y + ActiveForm.Location.Y);
                        LossHelps++;
                        tempInt = index; tempInt2 = Helps[1];
                        label_Help.Invoke(new Action(HelpLossText));
                        label_Help.Invoke(new Action(label_Help.Show));
                        HelpT.Stop();
                        HelpT.Interval = 7500;
                        HelpT.Start();
                        HelpPrompt(true);
                        return;
                    }
                }
                System.Timers.Timer T = new System.Timers.Timer(30), T2 = new System.Timers.Timer(30);
                if (FGameCheckBox.Checked)
                    T.Interval = 10;
                T.Start(); T2.Start();
                MG.Winner = -1;
                T2.Elapsed += (S, E) =>
                {
                    T2.Dispose();
                    Col_Leave(index);
                };
                T.Elapsed += (s, e) =>
                {
                    if (i > 1)
                    { Pt: try { C[i - 1][index].Image = W; } catch (Exception) { Thread.Sleep(20); goto Pt; } }
                    if (i != cap)
                    { Pt: try { C[i][index].Image = TRB[MG.Turn]; } catch (Exception) { Thread.Sleep(20); goto Pt; } }
                    else
                    {
                        MG.Case[cap][index] = MG.Turn + 1;
                        C[cap][index].Image = RB[MG.Turn];
                        MG.Winner = 0;
                        T.Dispose();
                        PlayEnd(index);
                    }
                    i++;
                };
                if (MG.vsAI && MG.Turn == MG.P[0])
                {
                    Thread workingThread = new Thread(new ThreadStart(PlayAI))
                    { IsBackground = true };
                    workingThread.Start();
                }
            }
        }

        private void Col_Enter(int index)
        {
            if ((!MG.vsAI || MG.Turn == MG.P[0]) && MG.Winner == 0 && MG.GetLow(index) > 0)
            {
                try { C[0][index].Show(); }
                catch (InvalidOperationException)
                {
                    C[0][index].Invoke(new Action(C[0][index].Show));
                }
                try
                {
                    if (MG.Case[MG.GetLow(index)][index] == 0 && MG.GetLow(index) > 0)
                        C[MG.GetLow(index)][index].Image = TRB[MG.Turn];
                }
                catch (Exception) { tempInt = index; C[MG.GetLow(index)][index].Invoke(new Action(SetC)); }
                for (int i = 1; i < 8; i++)
                {
                    if (i != index)
                        Col_Leave(i);
                }
            }
            else
                Col_Leave(index);
        }

        private void Col_Leave(int index)
        {
            if (C[0][index].Visible)
            {
                try { C[0][index].Hide(); }
                catch (InvalidOperationException)
                {
                    C[0][index].Invoke(new Action(C[0][index].Hide));
                }
                int i = MG.GetLow(index);
                Poin2: try
                {
                    if (MG.Case[i][index] == 0 && i > 0)
                        C[i][index].Image = W;
                }
                catch (InvalidOperationException) { goto Poin2; }
            }
        }

        private void PlayAI()
        {
            Col_Click(MG.PlayAI(), true);
        }

        private void PlayEnd(int index)
        {
            MG.Turn = (MG.Turn == 0) ? 1 : 0;
            try { C[0][index].Hide(); }
            catch (InvalidOperationException) { }
            if (MG.CheckWin() > 0)
            {
                UpdateVisuals();
                if (MG.Winner == 1)
                    TopPicture.Image = Properties.Resources.Win_Red;
                else
                    TopPicture.Image = Properties.Resources.Win_Blue;
                TopPicture.Invoke(new Action(TopImgScaleBig));
                try { Turn_Right.Hide(); Turn_Left.Hide(); }
                catch (InvalidOperationException)
                {
                    Turn_Right.Invoke(new Action(Turn_Right.Hide));
                    Turn_Left.Invoke(new Action(Turn_Left.Hide));
                }
                try { button_Exit.Show(); button_Restart.Show(); }
                catch (InvalidOperationException)
                {
                    button_Exit.Invoke(new Action(button_Exit.Show));
                    button_Restart.Invoke(new Action(button_Restart.Show));
                }
            }
            else if (MG.CheckTie())
            {
                UpdateVisuals();
                TopPicture.Image = Properties.Resources.Win_Tie;
                TopPicture.Invoke(new Action(TopImgScaleBig));
                try { Turn_Right.Hide(); Turn_Left.Hide(); }
                catch (InvalidOperationException)
                {
                    Turn_Right.Invoke(new Action(Turn_Right.Hide));
                    Turn_Left.Invoke(new Action(Turn_Left.Hide));
                }
                try { button_Exit.Show(); button_Restart.Show(); }
                catch (InvalidOperationException)
                {
                    button_Exit.Invoke(new Action(button_Exit.Show));
                    button_Restart.Invoke(new Action(button_Restart.Show));
                }
            }
            else
            {
                if(CurrentMouseIndex > 0)
                    Col_Enter(CurrentMouseIndex);
                if (MG.P[0] == 0)
                {
                    if (MG.Turn == 0)
                    {
                        Turn_Left.Image = Properties.Resources.Large_Gold_Red_Circle;
                        Turn_Right.Image = Properties.Resources.T_Large_Blue_Circle;
                    }
                    else
                    {
                        Turn_Left.Image = Properties.Resources.T_Large_Red_Circle;
                        Turn_Right.Image = Properties.Resources.Large_Gold_Blue_Circle;
                    }
                }
                else
                {
                    if (MG.Turn == 0)
                    {
                        Turn_Right.Image = Properties.Resources.Large_Gold_Red_Circle;
                        Turn_Left.Image = Properties.Resources.T_Large_Blue_Circle;
                    }
                    else
                    {
                        Turn_Right.Image = Properties.Resources.T_Large_Red_Circle;
                        Turn_Left.Image = Properties.Resources.Large_Gold_Blue_Circle;
                    }
                }
                if (MG.vsAI && LearnMCheckBox.Checked && MG.Turn == MG.P[0])
                {
                    Helps[0] = MG.CheckPossibleWin();
                    Helps[1] = MG.CheckPossibleLoss();
                }
            }
        }

        private void UpdateVisuals()
        {
            Thread.Sleep(50);
            for (int x = 1; x < 7; x++)
            {
                for (int y = 1; y < 8; y++)
                {
                    try
                    {
                        if (MG.Case[x][y] >= 0)
                            C[x][y].Image = Case[MG.Case[x][y]];
                        else
                            C[x][y].Image = TRB[-1 - MG.Case[x][y]];
                    }
                    catch (Exception) { Thread.Sleep(50); }
                }
            }
        }

        private void SetC()
        {
            C[MG.GetLow(tempInt)][tempInt].Image = TRB[MG.Turn];
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
                MG.Winner = 0;
                if (MG.Turn == MG.P[1] && MG.vsAI)
                {
                    Col_Click(MG.PlayAI(), true);
                }
            }
            else
            {
                int ind = 6;
                int[] ind2 = { 4, 4 };
                System.Timers.Timer T3 = new System.Timers.Timer(25);
                T3.Start();
                T3.Elapsed += (s3, E3) =>
                {
                    C[ind][ind2[0]].Invoke(new Action(C[ind][ind2[0]].Show));
                    C[ind][ind2[1]].Invoke(new Action(C[ind][ind2[1]].Show));
                    if ((ind % 2 == 0 && ind2[0] == 1) || (ind % 2 != 0 && ind2[0] == 4))
                        ind--;
                    else if (ind % 2 == 0) { ind2[0]--; ind2[1]++; }
                    else { ind2[0]++; ind2[1]--; }
                    if (ind == 0)
                    {
                        T3.Dispose();
                        Thread.Sleep(100);
                        Turn_Right.Invoke(new Action(Turn_Right.Show));
                        Turn_Left.Invoke(new Action(Turn_Left.Show));
                        MG.Winner = 0;
                        if (MG.Turn == MG.P[1] && MG.vsAI)
                        {
                            Col_Click(MG.PlayAI(), true);
                        }
                    }
                };
            }
        }

        private void HelpPrompt(bool NotGold)
        {
            if(NotGold)
            { Help_PictureBox.Image = Properties.Resources.Help; }
            else
            { Help_PictureBox.Image = Properties.Resources.Gold_Help; }
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

        //Events

        private void Color_Select_Red_Click(object sender, EventArgs e)
        {
            if (MG.P[0] != 0)
            {
                Color_Select_Blue.Image = Properties.Resources.Large_Blue_Circle;
                Color_Select_Red.Image = Properties.Resources.Large_Gold_Red_Circle;
                button_Start.ForeColor = Color.FromArgb(221, 46, 68);
                button_Start.FlatAppearance.BorderColor = Color.FromArgb(221, 46, 68);
                button_Start.FlatAppearance.MouseOverBackColor = Color.FromArgb(247, 224, 227);
                button_Start.FlatAppearance.MouseDownBackColor = Color.FromArgb(248, 194, 65);
                button_Start.Font = new Font(button_Start.Font.Name, button_Start.Font.Size, FontStyle.Bold);
                button_Start.BackColor = Color.White;
                try { Label_Err_Color.Hide(); } catch (Exception) { }
                MG.P[0] = 0; MG.P[1] = 1;
            }
        }

        private void Color_Select_Blue_Click(object sender, EventArgs e)
        {
            if (MG.P[0] != 1)
            {
                Color_Select_Red.Image = Properties.Resources.Large_Red_Circle;
                Color_Select_Blue.Image = Properties.Resources.Large_Gold_Blue_Circle;
                button_Start.ForeColor = Color.FromArgb(86, 172, 238);
                button_Start.FlatAppearance.BorderColor = Color.FromArgb(86, 172, 238);
                button_Start.FlatAppearance.MouseOverBackColor = Color.FromArgb(211, 234, 246);
                button_Start.FlatAppearance.MouseDownBackColor = Color.FromArgb(248, 194, 65);
                button_Start.Font = new Font(button_Start.Font.Name, button_Start.Font.Size, FontStyle.Bold);
                button_Start.BackColor = Color.White;
                try { Label_Err_Color.Hide(); } catch (Exception) { }
                MG.P[0] = 1; MG.P[1] = 0;
            }
        }

        private void Start_Click(object sender, EventArgs e)
        {
            if (MG.P[0] != -1)
            {
                LoadingBox.Show();
                if (MG.P[0] == 0)
                {
                    if(MG.Turn == 0)
                    {
                        Turn_Left.Image = Properties.Resources.Large_Gold_Red_Circle;
                        Turn_Right.Image = Properties.Resources.T_Large_Blue_Circle;
                    }
                    else
                    {
                        Turn_Left.Image = Properties.Resources.T_Large_Red_Circle;
                        Turn_Right.Image = Properties.Resources.Large_Gold_Blue_Circle;
                    }
                }
                else
                {
                    if (MG.Turn == 0)
                    {
                        Turn_Right.Image = Properties.Resources.Large_Gold_Red_Circle;
                        Turn_Left.Image = Properties.Resources.T_Large_Blue_Circle;
                    }
                    else
                    {
                        Turn_Right.Image = Properties.Resources.T_Large_Red_Circle;
                        Turn_Left.Image = Properties.Resources.Large_Gold_Blue_Circle;
                    }
                }
                if (LearnMCheckBox.Checked && MG.vsAI)
                    Help_PictureBox.Invoke(new Action(Help_PictureBox.Show));
                else
                    Help_PictureBox.Invoke(new Action(Help_PictureBox.Hide));
                GameOptions.Hide();
                LossHelps = 0;
                System.Timers.Timer T = new System.Timers.Timer(500);
                T.Start();
                T.Elapsed += (s, E) =>
                {
                    T.Dispose();
                    LoadingBox.Invoke(new Action(LoadingBox.Hide));
                    SlideEffect();
                };
            }
            else
            {
                try { Label_Err_Color.Show(); }
                catch (InvalidOperationException) { }

            }
        }

        private void vsAI_Check(object sender, EventArgs e)
        {
            if(AIcheckBox.Checked)
            {
                MG.vsAI = true;
                Label_Diff.Enabled = true;
                RButtonEasy.Enabled = true;
                RButtonMed.Enabled = true;
                RButtonHard.Enabled = true;
                RButtonImpossible.Enabled = true;
                PredicitveCheckBox.Enabled = true;
                if(!RButtonImpossible.Checked && !RButtonHard.Checked)
                    LearnMCheckBox.Enabled = true;
            }
            else
            {
                MG.vsAI = false;
                Label_Diff.Enabled = false;
                RButtonEasy.Enabled = false;
                RButtonMed.Enabled = false;
                RButtonHard.Enabled = false;
                RButtonImpossible.Enabled = false;
                PredicitveCheckBox.Enabled = false;
                LearnMCheckBox.Enabled = false;
            }
        }

        private void Diff_ChkChanged(object sender, EventArgs e)
        {
            if (RButtonEasy.Checked)
            { MG.Diff = 0; LearnMCheckBox.Enabled = true; LearnMCheckBox.Checked = true; }
            if (RButtonMed.Checked)
            { MG.Diff = 1; LearnMCheckBox.Enabled = true; }
            if (RButtonHard.Checked)
            { MG.Diff = 2; LearnMCheckBox.Enabled = false; LearnMCheckBox.Checked = false; }
            if (RButtonImpossible.Checked)
            { MG.Diff = 3; LearnMCheckBox.Enabled = false; LearnMCheckBox.Checked = false; }
        }

        private void FGameChkChanged(object sender, EventArgs e)
        {
            if (FGameCheckBox.Checked)
                MG.Delay = new int[] { 300, 200, 100, 0 };
            else
                MG.Delay = new int[] { 1000, 500, 100, 0 };
        }

        private void PredicitveChkChanged(object sender, EventArgs e)
        {
            MG.PredicitveAI = PredicitveCheckBox.Checked;
        }

        private void button_Restart_Click(object sender, EventArgs e)
        {
            int[] temp = MG.P;
            MG = new MainGame();
            MG.P = temp;
            Diff_ChkChanged(null, null);
            vsAI_Check(null, null);
            UpdateVisuals();
            button_Exit.Hide(); button_Restart.Hide();
            GameOptions.Show();
            TopPicture.Image = Properties.Resources.Connect4;
            TopPicture.Invoke(new Action(TopImgScaleSmall));
            for (int i = 1; i < 8; i++)
            {
                for (int j = 1; j < 7; j++)
                {
                    C[j][i].Hide();
                }
            }
        }

        private void button_Exit_Click(object sender, EventArgs e)
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
            toolTip.SetToolTip(FGameCheckBox, "Fastens animations and lowers delay of AI moves");
            toolTip.SetToolTip(LearnMCheckBox, "Adds Tips to the match with insights on what to play");
            toolTip.SetToolTip(PredicitveCheckBox, "Enables predicitve moves by the AI");
            toolTip.SetToolTip(RButtonEasy, "Low chances for the AI to predict and block your moves with high delay");
            toolTip.SetToolTip(RButtonMed, "Average chances for the AI to predict and block your moves with medium delay");
            toolTip.SetToolTip(RButtonHard, "High chances for the AI to predict and block your moves with short delay");
            toolTip.SetToolTip(RButtonImpossible, "100% chance for the AI to predict and block your moves with 0 delay");
            toolTip.SetToolTip(TopPicture, "Connect 4, click to restart");
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
    }
}
