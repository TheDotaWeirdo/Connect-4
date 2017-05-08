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
        int CurrentMouseIndex = 0, LossHelps = 0;
        bool InfiniteLoop = false, Helped = false;
        public delegate void ControlUpdate(PictureBox PB, Bitmap B);
        public ControlUpdate CUpdte = new ControlUpdate(UpdateControl);
        public MainGame MG = new MainGame();
        public List<List<PictureBox>> C = new List<List<PictureBox>>();
        Dictionary<string, Bitmap> BitLibrary = new Dictionary<string, Bitmap>();
        System.Timers.Timer HelpT = new System.Timers.Timer(10000);
#if DEBUG
        public delegate void DebugUpdate(Label L, int i, MainGame mg);
        public DebugUpdate DSUpdate = new DebugUpdate(UpdateDebugState);
#endif

        public Form1()
        {
            InitializeComponent();
        }

        private void Col_Click(int index, bool AImove = false)
        {
#if DEBUG
            label1.Invoke(DSUpdate, new object[] { label1, 1, MG });
            label3.Invoke(DSUpdate, new object[] { label3, 2, MG });
            label4.Invoke(DSUpdate, new object[] { label4, 3, MG });
            label5.Invoke(DSUpdate, new object[] { label5, 4, MG });
            label6.Invoke(DSUpdate, new object[] { label6, 5, MG });
            label7.Invoke(DSUpdate, new object[] { label7, 6, MG });
            label8.Invoke(DSUpdate, new object[] { label8, 7, MG });
#endif
            int i = 1, Cap = MG.GetLow(index, MG.Case);
            if (Cap > 0 && !MG.Finished && !MG.Busy && (!MG.vsAI || MG._Turn == ((MG.LeftIsRed) ? 0 : 1) || AImove))
            {
                // Learning Mode CodeBlock, checks for possible helps
                if (!Helped && !AImove && MG.LearnMode)
                {
                    if (MG.PlayerState[0].Any(x => x.Value.Contains(5)))
                    {
                        if (!MG.PlayerState[0][index].Contains(5) && MG.PlayerState[0].Any(x => (!x.Value.Contains(3) && x.Key != index)))
                        {
                            int tmp = MG.PlayerState[0].Keys.Where(x => MG.PlayerState[0][x].Contains(5)).FirstOrDefault();
                            Cursor.Position = new Point(C[2][tmp].Location.X + ActiveForm.Location.X + 25, 10 + C[2][tmp].Location.Y + ActiveForm.Location.Y);
                            label_Help.Text = $"Try playing the Column #{tmp}, it will create a 4-Token chain and Win you the Game!";
                            label_Help.Show();
                            HelpT.Stop();
                            HelpT.Start();
                            HelpPrompt(true);
                            Helped = true;
                            return;
                        }
                    }
                    else if (MG.PlayerState[0].Any(x => x.Value.Contains(6)) && LossHelps < 5 - (MG.Diff / 15))
                    {
                        if (!MG.PlayerState[0][index].Contains(6) && MG.PlayerState[0].Any(x => (!x.Value.Contains(3) && x.Key != index)))
                        {
                            int tmp = MG.PlayerState[0].Keys.Where(x => MG.PlayerState[0][x].Contains(6)).FirstOrDefault();
                            Cursor.Position = new Point(C[2][tmp].Location.X + ActiveForm.Location.X + 25, 10 + C[2][tmp].Location.Y + ActiveForm.Location.Y);
                            label_Help.Text = $"Playing the Column #{index} will let the AI Win, block the AI in Column #{tmp} instead";
                            label_Help.Show();
                            HelpT.Stop();
                            HelpT.Start();
                            HelpPrompt(true);
                            Helped = true;
                            return;
                        }
                    }
                    else if (MG.PlayerState[0].Any(x => x.Value.Contains(1))|| MG.PlayerState[0].Any(x => x.Value.Contains(4)))
                    {
                        if(MG.PlayerState[0][index].Contains(1) && !MG.PlayerState[0].All(x => (x.Value.Contains(1) || x.Value.Contains(4) || x.Value.Contains(3))) && MG.PlayerState[0].Any(x => (!x.Value.Contains(3) && x.Key != index)))
                        {
                            label_Help.Text = $"Playing the Column #{index} will open a Win for the AI right above you, try playing somewhere else";
                            label_Help.Show();
                            HelpT.Stop();
                            HelpT.Start();
                            HelpPrompt(true);
                            Helped = true;
                            return;
                        }
                        else if (MG.PlayerState[0][index].Contains(4) && !MG.PlayerState[0].All(x => (x.Value.Contains(1) || x.Value.Contains(4) || x.Value.Contains(3))) && MG.PlayerState[0].Any(x => (!x.Value.Contains(3) && x.Key != index)))
                        {
                            label_Help.Text = $"Playing the Column #{index} will sacrifice one of your threats, try playing somewhere else";
                            label_Help.Show();
                            HelpT.Stop();
                            HelpT.Start();
                            HelpPrompt(true);
                            Helped = true;
                            return;
                        }
                    }
                    else if (!MG.PlayerState[0][index].Contains(-4) && MG.PlayerState[0].Any(x => x.Value.Contains(-4)))
                    {
                        int tmp = MG.PlayerState[0].Keys.Where(x => MG.PlayerState[0][x].Contains(-4)).FirstOrDefault();
                        Cursor.Position = new Point(C[2][tmp].Location.X + ActiveForm.Location.X + 25, 10 + C[2][tmp].Location.Y + ActiveForm.Location.Y);
                        label_Help.Text = $"Try playing in Column #{tmp}, it will create a Threat for you which forces the AI not to play there";
                        label_Help.Show();
                        HelpT.Stop();
                        HelpT.Start();
                        HelpPrompt(true);
                        Helped = true;
                        return;
                    }
                }
                // Hides the Help label when the player plays a correct move
                if (!AImove && label_Help.Visible)
                { HelpPrompt(false); label_Help.Invoke(new Action(label_Help.Hide)); }
                // Changes the value where the token was dropped and increments moves count
                MG.Play(index);
                // Calculates the speed of which the token will be dropping
                System.Timers.Timer T = new System.Timers.Timer();
                if (MG.FastGame)
                    T.Interval = (16.25 + ((HWDiff(Width, Height) - 425) / 150)) * ((MG.LearnMode) ? 3.5 : 1);
                else
                    T.Interval = (32 + ((HWDiff(Width, Height) - 425) / 20)) * ((MG.LearnMode) ? 1.7 : 1);
                // Start of the animation process, MG.Busy is enabled so that nothing else happens when the token is being dropped
                MG.Busy = true;
                // Hides the arrow and makes the target location of the token white
                C[Cap][index].Invoke(CUpdte, new object[] { C[Cap][index], BitLibrary["W"] });
                C[0][index].Invoke(new Action(C[0][index].Hide));
                // In case of a HumanVsHuman, changes the color of the Arrows
                if (!MG.vsAI)
                    for (int I = 1; I < 8; I++)
                        lock(C[0][I])
                            C[0][I].Image = BitLibrary["Arrow"+((MG._Turn == 0) ? 1 : 0)];
                // Starts the Timer that drops the token
                T.Start();
                T.Elapsed += (s,e) => 
                {
                    // Makes previous token location white again
                    if (i > 1)
                        C[i - 1][index].Invoke(CUpdte, new object[] { C[i - 1][index], BitLibrary["W"] });
                    // Makes token location transparent
                    if (i != Cap)
                        C[i][index].Invoke(CUpdte, new object[] { C[i][index], BitLibrary["TC" + MG._Turn] });
                    else
                    {
                        // Token arrived to final location, stops the timer and runs the Ending function
                        C[Cap][index].Invoke(CUpdte, new object[] { C[Cap][index], BitLibrary["C" + (MG._Turn + 1)] });
                        T.Dispose();
                        PlayEnd(index);
                    }
                    i++;
                };
                // While the token animation is in proccess, the AI calculates his next move in the background
                if (MG.vsAI && !AImove && MG.Winner == 0 && !MG.Tied)
                {
                    Thread workingThread = new Thread(new ThreadStart(PlayAI))
                    { IsBackground = true, Priority = ThreadPriority.AboveNormal };
                    workingThread.Start();
                }
            }
        }

        private void Col_Enter(int index, bool Forced = false)
        {
            if (Forced || ((!MG.vsAI || MG._Turn == ((MG.LeftIsRed) ? 0 : 1)) && !MG.Finished && !MG.Busy && MG.GetLow(index, MG.Case) > 0))
            {
                C[0][index].Invoke(new Action(C[0][index].Show));
                int i = MG.GetLow(index, MG.Case);
                if (i > 0)
                    lock (C[i][index])
                    { C[i][index].Image = BitLibrary["TC" + MG._Turn]; }
            }
        }

        private void Col_Leave(int index, bool Forced = false)
        {
            if (C[0][index].Visible || Forced)
            {
                C[0][index].Invoke(new Action(C[0][index].Hide));
                int i = MG.GetLow(index, MG.Case);
                lock (C[i][index])
                { if (i > 0) C[i][index].Image = BitLibrary["W"]; }
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
            if (MG.Winner > 0)
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
            else if (MG.Tied)
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
                MG.Turn();
                Turn_Left.Invoke(new Action(TLUpdate));
                Turn_Right.Invoke(new Action(TRUpdate));
                MG.Busy = false;
                if (CurrentMouseIndex > 0 && MG.GetLow(CurrentMouseIndex, MG.Case) > 0 && (MG._Turn == ((MG.LeftIsRed) ? 0 : 1) || !MG.vsAI))
                    Col_Enter(CurrentMouseIndex, true);
                if (MG.LearnMode && MG._Turn == MG.P[0])
                {
                    Helped = false;
                    MG.AsignHelps();
                }
            }
        }

        private void UpdateVisuals()
        {
            for (int y = 1; y < 7; y++)
                for (int x = 1; x < 8; x++)
                  C[y][x].Invoke(CUpdte, new object[] { C[y][x], BitLibrary["C" + MG.Case[y][x].ToString()] });
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
                          if (MG._Turn == MG.P[1] && MG.vsAI)
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

        private void TLUpdate()
        {
            if (MG.LeftIsRed)
            {
                if (MG._Turn == 0)
                    Turn_Left.Image = Red_Loading;
                else
                    Turn_Left.Image = T_Large_Red_Circle;
            }
            else
            {
                if (MG._Turn == 0)
                    Turn_Left.Image = T_Large_Blue_Circle;
                else
                    Turn_Left.Image = Blue_Loading;
            }
        }

        private void TRUpdate()
        {
            if (MG.LeftIsRed)
            {
                if (MG._Turn == 0)
                    Turn_Right.Image = T_Large_Blue_Circle;
                else
                    Turn_Right.Image = Blue_Loading;
            }
            else
            {
                if (MG._Turn == 0)
                    Turn_Right.Image = Red_Loading;
                else
                    Turn_Right.Image = T_Large_Red_Circle;
            }
        }

        private static void UpdateControl(PictureBox PB, Bitmap B)
        {
            lock (PB)
            {
                PB.Image = B;
            }
        }

        private void SetInGamePrefs()
        {
            MG.Diff = DiffBar.Value;
            MG.vsAI = AIcheckBox.Checked;
            MG.HumanizedAI = HumanizedCheckBox.Checked;
            MG.StrategicAI = StrategicCheckBox.Checked;
            MG.PredictiveAI = PredicitveCheckBox.Checked;
            MG.LearnMode = LearnMCheckBox.Checked;
            MG.FastGame = FGameCheckBox.Checked;
        }

        private void PromptRestart()
        {
            MG.Busy = true;
            Restart_PB.Visible = true;
            Restart_BG_YN.Visible = 
            Restart_PB_BG.Visible = 
            Restart_btn_N.Visible = 
            Restart_btn_Y.Visible = true;
            Turn_Left.Enabled = false;
            Turn_Right.Enabled = false;
            TopPicture.Enabled = false;
            for (int x = 1; x < 8; x++)
                for (int y = 1; y < 7; y++)
                    MG.Case[y][x] *= -1;
            UpdateVisuals();
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
                SetInGamePrefs();
                TopPicture.Hide();
                TopPicture.Enabled = true;
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
                if (MG.LearnMode)
                { Help_PictureBox.Invoke(new Action(Help_PictureBox.Show)); }
                else
                { Help_PictureBox.Invoke(new Action(Help_PictureBox.Hide)); }
                GameOptions.Hide();
                LossHelps = 0;
                if (!MG.vsAI)
                    for (int i = 1; i < 8; i++)
                        C[0][i].Image = BitLibrary["Arrow" + MG._Turn];
                else
                    for (int i = 1; i < 8; i++)
                        C[0][i].Image = BitLibrary["Arrow" + ((MG.LeftIsRed) ? 0 : 1)];
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

        private void PredicitveChkChanged(object sender, EventArgs e)
        {
            if (InfiniteLoop) return;
            if (PredicitveCheckBox.Checked)
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
        }

        private void HumanizedChkChanged(object sender, EventArgs e)
        {
            if (InfiniteLoop) return;
            if (HumanizedCheckBox.Checked)
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
            if (StrategicCheckBox.Checked)
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
        }

        private void LearnMCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (InfiniteLoop) return;
            if (LearnMCheckBox.Checked)
            {
                DiffBar.Value = Math.Min(25, DiffBar.Value);
                if (DiffBar.Value == 25)
                    DiffBar_Scroll(null, null);
            }
        }

        private void Button_Restart_Click(object sender, EventArgs e)
        {
            if (MG.Busy && !MG.Finished) return;
            if (!MG.Finished &&  MG.Moves > 3)
            { PromptRestart(); return; }
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
            LossHelps = 0;
            UpdateVisuals();
            button_Exit.Hide(); button_Restart.Hide();
            GameOptions.Show();
            Turn_Right.Hide();
            Help_PictureBox.Hide();
            Turn_Left.Hide();
            TopPicture.Image = Properties.Resources.Connect_4;
            TopPicture.Invoke(new Action(TopImgScaleSmall));
            TopPicture.Enabled = false;
            for (int i = 1; i < 8; i++)
            {
                for (int j = 1; j < 7; j++)
                {
                    C[j][i].Hide();
                }
            }
        }

        private void Restart_btn_Y_Click(object sender, EventArgs e)
        {
            MG.Finished = true;
            MG.Busy = false;
            Restart_btn_N_Click(null, null);
            Button_Restart_Click(null, null);
        }

        private void Restart_btn_N_Click(object sender, EventArgs e)
        {
            MG.Busy = false;
            Restart_PB.Visible = false;
            Restart_BG_YN.Visible =
            Restart_PB_BG.Visible =
            Restart_btn_N.Visible =
            Restart_btn_Y.Visible = false;
            Turn_Left.Enabled = true;
            Turn_Right.Enabled = true;
            TopPicture.Enabled = true;
            for (int x = 1; x < 8; x++)
                for (int y = 1; y < 7; y++)
                    MG.Case[y][x] *= -1;
            UpdateVisuals();
        }

        private void DiffBar_Scroll(object sender, EventArgs e)
        {
            if (!InfiniteLoop)
            {
                InfiniteLoop = true;
                HumanizedCheckBox.Checked = (DiffBar.Value == 100);
                if (!PredicitveCheckBox.Checked)
                    PredicitveCheckBox.Checked = (DiffBar.Value >= 50);
                else
                    PredicitveCheckBox.Checked = (DiffBar.Value >= 25);
                if (!StrategicCheckBox.Checked)
                    StrategicCheckBox.Checked = (DiffBar.Value >= 75);
                else
                    StrategicCheckBox.Checked = (DiffBar.Value >= 50);
                if (!LearnMCheckBox.Checked)
                    LearnMCheckBox.Checked = (DiffBar.Value <= 15);
                else
                    LearnMCheckBox.Checked = (DiffBar.Value <= 30);
                InfiniteLoop = false;
            }
            if (DiffBar.Value < 13)
            {
                Difficulty_Label.Text = "Easy";
                Difficulty_Label.ForeColor = Label_Easy.ForeColor = Color.LimeGreen;
                Label_Merciless.ForeColor = Label_Medium.ForeColor = Label_Intermediate.ForeColor = Label_Hard.ForeColor = Color.FromArgb(64, 64, 64);
                return;
            }
            if (DiffBar.Value < 38)
            {
                Difficulty_Label.Text = "Medium";
                Difficulty_Label.ForeColor = Label_Medium.ForeColor = Color.DodgerBlue;
                Label_Easy.ForeColor = Label_Merciless.ForeColor = Label_Intermediate.ForeColor = Label_Hard.ForeColor = Color.FromArgb(64, 64, 64);
                return;
            }
            if (DiffBar.Value < 63)
            {
                Difficulty_Label.Text = "Intermediate";
                Difficulty_Label.ForeColor = Label_Intermediate.ForeColor = Color.DarkOrange;
                Label_Easy.ForeColor = Label_Medium.ForeColor = Label_Merciless.ForeColor = Label_Hard.ForeColor = Color.FromArgb(64, 64, 64);
                return;
            }
            if (DiffBar.Value < 88)
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
            BitLibrary.Add("Arrow0", Arrow_Red);
            BitLibrary.Add("Arrow1", Arrow_Blue);
            BitLibrary.Add("W", White_Circle);
            BitLibrary.Add("Red", Red_Circle);
            BitLibrary.Add("Blue", Blue_Circle);
            BitLibrary.Add("TC1", T_Blue_Circle);
            BitLibrary.Add("TC0", T_Red_Circle);
            BitLibrary.Add("C-2", T_Blue_Circle);
            BitLibrary.Add("C-1", T_Red_Circle);
            BitLibrary.Add("C0", White_Circle);
            BitLibrary.Add("C1", Red_Circle);
            BitLibrary.Add("C2", Blue_Circle);
            BitLibrary.Add("C3", Gold_Red_Circle);
            BitLibrary.Add("C4", Gold_Blue_Circle);
            toolTip.SetToolTip(FGameCheckBox, "Fastens animations and lowers delay of AI moves");
            toolTip.SetToolTip(LearnMCheckBox, "Adds Tips to the match with insights on what to play");
            toolTip.SetToolTip(PredicitveCheckBox, "Enables Predicitve moves by the AI");
            toolTip.SetToolTip(StrategicCheckBox, "Enables Strategic moves by the AI, Predictive AI is Obligatory");
            toolTip.SetToolTip(HumanizedCheckBox, "Enables Humanized moves by the AI, Difficulty must be Maxed Out");
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
            bool b = MG.Busy; MG.Busy = true;
            if(ActiveForm != null)
                { ResizeForm(); }
            else
                ResizeForm();
            MG.Busy = b;
        }

        private void ResizeForm()
        {  // Max : 666, 1000
            int W, H, GOW, GOH, CS; int[] CBL;
            try
            { W = ActiveForm.Width; H = ActiveForm.Height; if (W < ActiveForm.MinimumSize.Width) return; }
            catch (Exception) { W = 433; H = 650; }
            TopImgSize[0] = (int)(H * 0.166);
            TopImgSize[1] = (int)(H * 0.2383);

            {   //In-Game
                CS = (HWDiff(W, H)) / 13 + 10;
                label_Help.Font = new Font("Century Gothic", HWDiff(W, H) / 47, FontStyle.Italic);
                Help_PictureBox.Size = new Size((int)(HWDiff(W, H) / 14.16), (int)(HWDiff(W, H) / 14.16));
                label_Help.Location = new Point(W / 21 + Help_PictureBox.Width, H - CS - label_Help.Height);
                label_Help.Width = W - (W / 20) - label_Help.Location.X + 10;
                Help_PictureBox.Location = new Point(W / 42, H - CS - (int)(label_Help.Height / 1.33) - (int)((Help_PictureBox.Height - 30) / 2));
                CBL = new int[2] { (W / 2 - (CS * 7 - 7) / 2 - 6), (30 + ((H - TopImgSize[0]) / 2 - (CS * 7 - 7) / 2 - 6) + TopImgSize[0]) };
                if (MG.LearnMode && CBL[1] + (CS * 7) + 20 > label_Help.Location.Y)
                {
                    CBL[1] -= CBL[1] + (CS * 7) + 20 - label_Help.Location.Y;
                    TopImgSize[0] -= CBL[1] + (CS * 7) + 20 - label_Help.Location.Y;
                    TopImgSize[1] -= CBL[1] + (CS * 7) + 20 - label_Help.Location.Y;
                }
                for (int x = 1; x < 8; x++)
                {
                    for (int y = 0; y < 7; y++)
                    {
                        C[y][x].Location = new Point(CBL[0] + CS * (x - 1), CBL[1] + CS * y);
                        C[y][x].Size = new Size(CS, CS);
                    }
                }
                button_Exit.Height = button_Restart.Height = button_Start.Height = (H - 650) / 25 + 45;
                button_Exit.Width = button_Restart.Width = button_Start.Width = (W - 433) / 6 + 100;
                button_Exit.Font = button_Start.Font = button_Restart.Font = new Font("Century Gothic", (float)(4.25 + HWDiff(W, H) / 42.5), FontStyle.Bold);
                if (MG.P[0] == -1)
                    button_Start.Font = new Font("Century Gothic", (float)(4.25 + HWDiff(W, H) / 42.5));
                button_Exit.Location = new Point((W - button_Exit.Width) * 3 / 4, (int)((((CBL[1] + CS - TopImgSize[1]) / 2.85) + TopImgSize[1]) ));
                button_Restart.Location = new Point((W - button_Exit.Width) / 4, (int)((((CBL[1] + CS - TopImgSize[1]) / 2.85) + TopImgSize[1]) ));
                Turn_Left.Height = Turn_Left.Width = Turn_Right.Height = Turn_Right.Width = (int)(1.5 * CS);
                Turn_Right.Location = new Point((W - Turn_Right.Height) * 3 / 4, (int)((((CBL[1] - TopImgSize[0]) / 2.85) + TopImgSize[0]) ));
                Turn_Left.Location = new Point((W - Turn_Right.Height) / 4, (int)((((CBL[1] - TopImgSize[0]) / 2.85) + TopImgSize[0]) ));
                Restart_PB_BG.Width = W;
                Restart_PB_BG.Location = new Point(0, H / 3 + 20);
                Restart_PB.Width = Math.Min(650, W * 9 / 10);
                Restart_btn_Y.Height = Restart_btn_N.Height  = ((H - 650) / 25 + 45) * 8 / 10;
                Restart_btn_Y.Width = Restart_btn_N.Width = ((W - 433) / 6 + 100) * 8 / 10;
                Restart_btn_Y.Font = Restart_btn_N.Font = new Font("Century Gothic", (float)(2.25 + HWDiff(W, H) / 42.5), FontStyle.Bold);
                Restart_btn_N.Location = new Point((W - Restart_btn_N.Width) * 3 / 4, H * 2 / 3 - 30);
                Restart_btn_Y.Location = new Point((W - Restart_btn_Y.Width) / 4, H * 2 / 3 - 30);
                Restart_BG_YN.Height = Restart_PB_BG.Height = Restart_btn_N.Height * 7 / 4;
                Restart_BG_YN.Width = W;
                Restart_BG_YN.Location= new Point(0, H * 2 / 3 - 30 - ((Restart_BG_YN.Height - Restart_btn_N.Height) / 2));
                Restart_PB.Location = new Point((W - Restart_PB.Width - 15) / 2, (Restart_PB_BG.Height - 58) / 2 + (H / 3) +20);
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
                GOW = W - 40;
                GOH = H - TopImgSize[0] - 58;
                TopPicture.Width = W - 40;
                LoadingBox.Size = new Size(W, H);
                LoadingBox.Location = new Point(0, 0);
                GameOptions.Width = GOW; GOW += 40;
                GameOptions.Height = GOH;
                TopPicture.Height = TopImgSize[(MG.Finished && !GameOptions.Visible) ? 1 : 0];
                FGameCheckBox.Location = new Point(FGameCheckBox.Location.X, GOH / 15 + 6);
                GameOptions.Location = new Point(12, 6 + TopImgSize[0]);
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
            label1.Invoke(DSUpdate, new object[] { label1, 1, MG });
            label3.Invoke(DSUpdate, new object[] { label3, 2, MG });
            label4.Invoke(DSUpdate, new object[] { label4, 3, MG });
            label5.Invoke(DSUpdate, new object[] { label5, 4, MG });
            label6.Invoke(DSUpdate, new object[] { label6, 5, MG });
            label7.Invoke(DSUpdate, new object[] { label7, 6, MG });
            label8.Invoke(DSUpdate, new object[] { label8, 7, MG });
#endif
        }
        private static void UpdateDebugState(Label L, int i, MainGame mg)
        {
#if DEBUG
            int l = 0;
            string s = "";
            foreach(int a in mg.AIState[i])
            {
                if (mg.AIState[i].Count > 1 && a == 0) { }
                else {
                    if (l == 2)
                    { l = 0; s += "\n"; }
                    s += "'"+a+"'";
                    l++;
                }
            }
            lock (L)
                L.Text = s + "\n|" + mg.AISeverity[i].ToString()+"|";
#endif
        }

    }
}
