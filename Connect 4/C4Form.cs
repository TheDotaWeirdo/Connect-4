using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using static Connect_4.Properties.Resources;

namespace Connect_4
{
    public partial class C4Form : Form
    {
        private int CurrentCol = 4;
        private int LossHelps = 0;
        private int[] TopImgSize = { 100, 155 };
        private bool InfiniteLoop = false;
        private bool Falling = false;
        private bool helped = false;
        private bool Helped
        {
            get { return helped; }
            set
            {
                if (value == true)
                    Play(HelpTic);
                helped = value;
            }
        }
        private string Moves = "";
        private MainGame MG = new MainGame();
        private List<List<PictureBox>> C = new List<List<PictureBox>>();
        private Dictionary<string, Bitmap> BitLibrary = new Dictionary<string, Bitmap>();
        private System.Timers.Timer HelpT = new System.Timers.Timer(10000);
        private UnmanagedMemoryStream[] Hits = { null, Hit_1, Hit_2, Hit_3, Hit_4, Hit_5, Hit_6, Hit_7 };
        private SoundPlayer Player = new SoundPlayer();
        private Point LastPlay = new Point(0, 0);
        private delegate void ControlUpdate(PictureBox PB, Bitmap B);
        private ControlUpdate CUpdte = new ControlUpdate(UpdateControl);
#if DEBUG
        private delegate void DebugUpdate(Label L, int i, MainGame mg);
        private DebugUpdate DSUpdate = new DebugUpdate(UpdateDebugState);
#endif

        public C4Form()
        {
            InitializeComponent();
        }

        // Inserts a token in the index column.
        private void Col_Click(int index, bool AImove = false)
        {
            if (!AImove && Moves.Length > 0) return;
#if DEBUG
            DebugStateButton_Click(null, null);
#endif
            var Cap = MG.Case.GetLow(index);

            if ((!MG.Case.IsFull(index)) && (!MG.Finished) && (!MG.Busy) && ((!MG.vsAI) || (MG._Turn == ((MG.LeftIsRed) ? 0 : 1) || AImove)))
            {
                // Learning Mode CodeBlock, checks for possible helps
                if ((!AImove) && (HelpLearn(index)))
                    return; 
                // Hides the Help label when the player plays a correct move
                if (!AImove && label_Help.Visible)
                {
                    HelpPrompt(false);
                    label_Help.Invoke(new Action(label_Help.Hide));
                }
                // Changes the value where the token was dropped and increments moves count
                MG.Play(index);
                // Calculates the speed of which the token will be dropping
                var T = new System.Timers.Timer();
                if (MG.FastGame)
                    T.Interval = (16.25 + ((HWDiff(Width, Height) - 433) / 150)) * ((MG.LearnMode) ? 2 : 1);
                else
                    T.Interval = (32 + ((HWDiff(Width, Height) - 433) / 75)) * ((MG.LearnMode) ? 1.35 : 1);
                // Start of the animation process, MG.Busy is enabled so that nothing else happens when the token is being dropped
                MG.Busy = true;
                // Hides the arrow and makes the target location of the token white
                C[Cap][index].Invoke(CUpdte, new object[] { C[Cap][index], BitLibrary["W"] });
                C[0][index].Invoke(new Action(C[0][index].Hide));
                Turn_Circle.Invoke(CUpdte, new object[] { Turn_Circle, (MG._Turn == 0) ? T_Large_Red_Circle : T_Large_Blue_Circle });
                // In case of a HumanVsHuman, changes the color of the Arrows
                if (!MG.vsAI)
                    for (var I = 1; I < 8; I++)
                        lock(C[0][I])
                            C[0][I].Image = BitLibrary["Arrow"+((MG._Turn == 0) ? 1 : 0)];
                // Starts the Timer that drops the token
                var i = 1;
                T.Start();
                T.Elapsed += (s,e) => 
                {
                    // Makes previous token location white again
                    if (i > 1 && i < 7)
                        C[i - 1][index].Invoke(CUpdte, new object[] { C[i - 1][index], BitLibrary["W"] });
                    // Makes token location transparent
                    if (i >= Cap)
                    {
                        // Token arrived to final location, stops the timer and runs the Ending function
                        C[Cap][index].Invoke(CUpdte, new object[] { C[Cap][index], BitLibrary["C" + (MG._Turn + 3)] });
                        T.Dispose();
                        PlayEnd(index);
                    }
                    else
                        C[i][index].Invoke(CUpdte, new object[] { C[i][index], BitLibrary["TC" + MG._Turn] });
                    i++;
                };
                // While the token animation is in proccess, the AI calculates his next move in the background
                if (MG.vsAI && !AImove && MG.Winner == 0 && !MG.Tied)
                {
                    Thread workingThread = new Thread(new ThreadStart(PlayAI))
                    { IsBackground = true, Priority = ThreadPriority.AboveNormal };
                    workingThread.Start();
                }
                else if (MG.LearnMode && AImove)
                {
                    Helped = false;
                    Thread workingThread = new Thread(new ThreadStart(MG.AsignHelps))
                    { IsBackground = true };
                    workingThread.Start();
                }
            }
        }

        // Shows an arrow above index column and where the token will land on mouse hover.
        private void Col_Enter(int index, bool Forced = false)
        {
            if (Moves.Length > 0 && (!Forced || index.ToString() != Moves[0].ToString())) return;

            if (Forced || ((!MG.vsAI || MG._Turn == ((MG.LeftIsRed) ? 0 : 1)) && !MG.Finished && !MG.Busy && MG.Case.GetLow(index) > 0))
            {
                for (var tmp = 1; tmp < 8; tmp++)
                    if (tmp != index && C[0][tmp].Visible)
                        Col_Leave(tmp, true);
                C[0][index].Invoke(new Action(C[0][index].Show));
                var i = MG.Case.GetLow(index);
                if (i > 0)
                    lock (C[i][index])
                    { C[i][index].Image = BitLibrary["TC" + MG._Turn]; }
            }
            CurrentCol = index;
        }

        // Hides the arrow and token created in Col_Enter.
        private void Col_Leave(int index, bool Forced = false)
        {
            if (index == 0 || (!Forced && Moves.Length > 0)) return;

            if (CurrentCol != index)
                Col_Leave(CurrentCol);

            if (C[0][index].Visible || Forced)
            {
                C[0][index].Invoke(new Action(C[0][index].Hide));
                int i = MG.Case.GetLow(index);
                lock (C[i][index])
                { if (i > 0) C[i][index].Image = BitLibrary["W"]; }
            }
            CurrentCol = 0;
        }

        // Used to create a background thread that calculates the AI's next move while a token drops.
        private void PlayAI()
        {
            if (MG.Delay == 0)
            {
                var i = - 1;
                var T = new System.Timers.Timer(1);
                T.Start();
                T.Elapsed += (S, E) =>
                {
                    if(!MG.Busy && i != -1)
                    {
                        T.Dispose();
                        if (i > 0)
                            Col_Click(i, true);
                    }
                };
                i = MG.PlayAI();
            }
            else
            {
                var i = -1;
                var extraDelay = 0d;
                if (MG.FastGame)
                    extraDelay = (16.25 + ((HWDiff(Width, Height) - 433) / 150)) * ((MG.LearnMode) ? 2 : 1);
                else
                    extraDelay = (32 + ((HWDiff(Width, Height) - 433) / 75)) * ((MG.LearnMode) ? 1.35 : 1);
                var T = new System.Timers.Timer(MG.Delay + 3 * extraDelay);
                T.Start();
                T.Elapsed += (s, e) =>
                {
                    if (i == -1)
                        T.Interval = 5;
                    else
                    {
                        T.Dispose();
                        if (i > 0)
                            Col_Click(i, true);
                    }
                };
                i = MG.PlayAI();
            }
        }

        // Actions that happen when a token arrives at the bottom.
        private void PlayEnd(int index)
        {
            if(LastPlay.X > 0)
                C[LastPlay.Y][LastPlay.X].Invoke(CUpdte, new object[] { C[LastPlay.Y][LastPlay.X], BitLibrary["C" + MG.Case[LastPlay.Y][LastPlay.X]] });
            LastPlay = new Point(index, MG.Case.GetLow(index) + 1);
            if (MG.Winner > 0)
            {
                UpdateVisuals();
                TopPicture.Invoke(new Action(TopImgScaleBig));
                button_Exit.Invoke(new Action(button_Exit.Show));
                button_Restart.Invoke(new Action(button_Restart.Show));
                button_Share.Invoke(new Action(button_Share.Show));
                Turn_Circle.Invoke(new Action(Turn_Circle.Hide));
                Undo_button.Invoke(new Action(Undo_button.Hide));
                Restart_button.Invoke(new Action(Restart_button.Hide));
                if (MG.Winner == 1)
                {
                    TopPicture.Image = Win_Red;
                    if (MG.LeftIsRed)
                        Play(Win);
                    else
                        Play(Loss);
                }
                else
                {
                    TopPicture.Image = Win_Blue;
                    if (MG.LeftIsRed)
                        Play(Loss);
                    else
                        Play(Win);
                }
            }
            else if (MG.Tied)
            {
                MG.Finished = true;
                UpdateVisuals(); 
                TopPicture.Image = Win_Tie;
                TopPicture.Invoke(new Action(TopImgScaleBig));
                button_Exit.Invoke(new Action(button_Exit.Show));
                button_Restart.Invoke(new Action(button_Restart.Show));
                button_Share.Invoke(new Action(button_Share.Hide));
                Turn_Circle.Invoke(new Action(Turn_Circle.Hide));
                Undo_button.Invoke(new Action(Undo_button.Hide));
                Restart_button.Invoke(new Action(Restart_button.Hide));
                Play(Tie);
            }
            else
            {
                Play(Hits[index]);
                MG.Turn();
                Turn_Circle.Invoke(new Action(TurnUpdate));
                MG.Busy = false;
                if (CurrentCol > 0 && MG.Case.GetLow(CurrentCol) > 0 && (MG._Turn == ((MG.LeftIsRed) ? 0 : 1) || !MG.vsAI))
                    Col_Enter(CurrentCol, true);                
            }
            if(Falling)
            {
                if (Falling = (Moves[0] != '_'))
                {
                    Col_Click(int.Parse(Moves[0].ToString()), true);
                    Moves = Moves.Substring(1, Moves.Length - 1);
                }
            }
        }

        // Checks for possible helps in Learning Mode and returns true if it found any.
        private bool HelpLearn(int index)
        {
            if (MG.MoveCount > 2 && !Helped && MG.LearnMode)
            {
                if (MG.PlayerState.Any(x => x.Value.Contains(5)))
                {
                    if (!MG.PlayerState[index].Contains(5) && MG.PlayerState.Any(x => (!x.Value.Contains(3) && x.Key != index)))
                    {
                        int tmp = MG.PlayerState.Keys.Where(x => MG.PlayerState[x].Contains(5)).FirstOrDefault();
                        Cursor.Position = new Point(C[2][tmp].Location.X + ActiveForm.Location.X + 25, 10 + C[2][tmp].Location.Y + ActiveForm.Location.Y);
                        label_Help.Text = $"Try playing the Column #{tmp}, it will create a 4-Token chain and Win you the Game!";
                        label_Help.Show();
                        HelpT.Stop();
                        HelpT.Start();
                        HelpPrompt(true);
                        Helped = true;
                        return true;
                    }
                }
                else if (MG.PlayerState.Any(x => x.Value.Contains(6)) && LossHelps < 5 - (MG.Diff / 15))
                {
                    if (!MG.PlayerState[index].Contains(6) && MG.PlayerState.Any(x => (!x.Value.Contains(3) && x.Key != index)))
                    {
                        int tmp = MG.PlayerState.Keys.Where(x => MG.PlayerState[x].Contains(6)).FirstOrDefault();
                        Cursor.Position = new Point(C[2][tmp].Location.X + ActiveForm.Location.X + 25, 10 + C[2][tmp].Location.Y + ActiveForm.Location.Y);
                        label_Help.Text = $"Playing the Column #{index} will let the AI Win, block the AI in Column #{tmp} instead";
                        label_Help.Show();
                        HelpT.Stop();
                        HelpT.Start();
                        HelpPrompt(true);
                        Helped = true;
                        return true;
                    }
                }
                else if (MG.PlayerState.Any(x => x.Value.Contains(1)) || MG.PlayerState.Any(x => x.Value.Contains(4)))
                {
                    if (MG.PlayerState[index].Contains(1) && !MG.PlayerState.All(x => (x.Value.Contains(1) || x.Value.Contains(4) || x.Value.Contains(3))) && MG.PlayerState.Any(x => (!x.Value.Contains(3) && x.Key != index)))
                    {
                        label_Help.Text = $"Playing the Column #{index} will open a Win for the AI right above you, try playing somewhere else";
                        label_Help.Show();
                        HelpT.Stop();
                        HelpT.Start();
                        HelpPrompt(true);
                        Helped = true;
                        return true;
                    }
                    else if (MG.PlayerState[index].Contains(4) && !MG.PlayerState.All(x => (x.Value.Contains(1) || x.Value.Contains(4) || x.Value.Contains(3))) && MG.PlayerState.Any(x => (!x.Value.Contains(3) && x.Key != index)))
                    {
                        label_Help.Text = $"Playing the Column #{index} will sacrifice one of your threats, try playing somewhere else";
                        label_Help.Show();
                        HelpT.Stop();
                        HelpT.Start();
                        HelpPrompt(true);
                        Helped = true;
                        return true;
                    }
                }
                else if (!MG.PlayerState[index].Contains(-4) && MG.PlayerState.Any(x => x.Value.Contains(-4)))
                {
                    int tmp = MG.PlayerState.Keys.Where(x => MG.PlayerState[x].Contains(-4)).FirstOrDefault();
                    Cursor.Position = new Point(C[2][tmp].Location.X + ActiveForm.Location.X + 25, 10 + C[2][tmp].Location.Y + ActiveForm.Location.Y);
                    label_Help.Text = $"Try playing in Column #{tmp}, it will create a Threat for you which forces the AI not to play there";
                    label_Help.Show();
                    HelpT.Stop();
                    HelpT.Start();
                    HelpPrompt(true);
                    Helped = true;
                    return true;
                }
            }
            return false;
        }

        // Updates all circles to their corresponding value in MG.Case.
        private void UpdateVisuals()
        {
            for (var y = 1; y < 7; y++)
                for (var x = 1; x < 8; x++)
                    if(BitLibrary.ContainsKey("C" + MG.Case[y][x].ToString()))
                        C[y][x].Invoke(CUpdte, new object[] { C[y][x], BitLibrary["C" + MG.Case[y][x].ToString()] });
         }

        // Changes the help icon to gold or dark grey.
        private void HelpPrompt(bool NotGold)
        {
            if(NotGold)
            { Help_PictureBox.Image = Properties.Resources.Help; }
            else
            { Help_PictureBox.Image = Gold_Help; }
        }

        // Resets the text of the Help Label.
        private void HelpResetText()
        {
            label_Help.Text = "Click on a column to place a token that will drop to the lowest empty space. Win the Game by matching 4 tokens";
        }

        // Scales the top picture to its minimum size.
        private void TopImgScaleSmall()
        {
            TopPicture.Height = TopImgSize[0];
        }

        // Scales the top picture to its maximum size.
        private void TopImgScaleBig()
        {
            TopPicture.Height = TopImgSize[1];
        }

        // Updates the Turn Circle to the appropriate color.
        private void TurnUpdate()
        {
            if (MG.LeftIsRed)
            {
                if (MG._Turn == 0)
                    Turn_Circle.Image = Red_Loading;
                else
                    Turn_Circle.Image = Blue_Loading;
            }
            else
            {
                if (MG._Turn == 0)
                    Turn_Circle.Image = Red_Loading;
                else
                    Turn_Circle.Image = Blue_Loading;
            }
        }

        // Prompts the Yes/No question to Restart.
        private void PromptRestart()
        {
            MG.Busy = true;
            Restart_PB.Visible = true;
            Restart_BG_YN.Visible = true;
            Restart_PB_BG.Visible = true;
            Restart_btn_N.Visible = true;
            Restart_btn_Y.Visible = true;
            Turn_Circle.Enabled = false;
            TopPicture.Enabled = false;
            Undo_button.Enabled = false;
            Restart_button.Enabled = false;
            button_Fall.Enabled = false;
            button_Tick.Enabled = false;
            Restart_BG_YN.BringToFront();
            Restart_PB_BG.BringToFront();
            Restart_btn_N.BringToFront();
            Restart_btn_Y.BringToFront();
            Restart_PB.BringToFront();
            if (!MG.Finished)
                for (var x = 1; x < 8; x++)
                    for (var y = 1; y < 7; y++)
                        MG.Case[y][x] *= -1;
            UpdateVisuals();
            int i = CurrentCol;
            Col_Leave(CurrentCol, true);
            CurrentCol = i;
        }

        // Prompts the Yes/No question to Exit.
        private void PromptExit()
        {
            MG.Busy = true;
            Exit_PB.Visible = true;
            Restart_BG_YN.Visible = true;
            Restart_PB_BG.Visible = true;
            Exit_btn_N.Visible = true;
            Exit_btn_Y.Visible = true;
            Turn_Circle.Enabled = false;
            TopPicture.Enabled = false;
            Undo_button.Enabled = false;
            Restart_button.Enabled = false;
            button_Fall.Enabled = false;
            button_Tick.Enabled = false;
            Restart_BG_YN.BringToFront();
            Restart_PB_BG.BringToFront();
            Exit_btn_N.BringToFront();
            Exit_btn_Y.BringToFront();
            Exit_PB.BringToFront();
            if(!MG.Finished)
                for (var x = 1; x < 8; x++)
                    for (var y = 1; y < 7; y++)
                        MG.Case[y][x] *= -1;
            UpdateVisuals();
            int i = CurrentCol;
            Col_Leave(CurrentCol, true);
            CurrentCol = i;
        }

        // Sets the game values in MainGame based on the controls.
        private void SetInGamePrefs()
        {
            MG.Diff = DiffBar.Value;
            MG.vsAI = AIcheckBox.Checked;
            MG.HumanizedAI = HumanizedCheckBox.Checked;
            MG.StrategicAI = StrategicCheckBox.Checked;
            MG.PredictiveAI = PredicitveCheckBox.Checked;
            MG.LearnMode = LearnMCheckBox.Checked;
            MG.FastGame = FGameCheckBox.Checked;
            MG.Starter = MG.P[MG._Turn];
            MG.Undos = (int)Math.Round((50 - (2 * MG.Diff)) / 25d) + 4;
        }

        // Initializes the slide effect at the start of a game.
        private void SlideEffect()
        {
            var index = new int[] { 5, 3 };
            var Timer = new System.Timers.Timer((MG.FastGame) ? 65 : 150);
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

        // Generates the slide effect for each column index.
        private void SlideEffect(int index)
        {
            var ind = 7;
            var Timer = new System.Timers.Timer((MG.FastGame) ? 9 : 27.5);
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
                        Turn_Circle.Invoke(new Action(Turn_Circle.Show));
                        if (Moves.Length > 0)
                        {
                            button_Fall.Invoke(new Action(button_Fall.Show));
                            button_Tick.Invoke(new Action(button_Tick.Show));
                        }
                        if (!MG.FastGame)
                            Thread.Sleep(150);
                        TopPicture.Invoke(new Action(TopPicture.Show));
                        Undo_button.Invoke(new Action(Undo_button.Show));
                        Restart_button.Invoke(new Action(Restart_button.Show));
                        MG.Busy = false;
                        for (var x = 1; x < 8; x++)
                        {
                            for (var y = 1; y < 7; y++)
                            {
                                if (C[y][x].Bounds.Contains((Cursor.HotSpot)))
                                { CurrentCol = x; goto p; }
                            }
                        }
                        p: if (MG._Turn == MG.P[1] && MG.vsAI)
                        {
                            Thread.Sleep((MG.FastGame) ? 100 : 400);
                            Col_Click(4, true);
                        }
                        else
                            Col_Enter((CurrentCol == 0) ? 4 : CurrentCol);
                    }
                }
            };
        }

        // Plays the sound passed as 's'.
        private void Play(UnmanagedMemoryStream s)
        {
            if (SoundsCheckBox.Checked)
            {
                try
                {
                    s.Position = 0;
                    Player.Stop();
                    Player.Stream = s;
                    Player.Play();
                } catch (Exception) { }
            }
        }

        // Used to change a control's picture via Invoke.
        private static void UpdateControl(PictureBox PB, Bitmap B)
        {
            lock (PB)
                PB.Image = B;
        }

        /****************************  Events  ****************************/

        // Initializes Class fields and assigns events for each circle.
        private void OnFormLoad(object sender, EventArgs e)
        {
            C.Add(new List<PictureBox> { null, C_1_0, C_2_0, C_3_0, C_4_0, C_5_0, C_6_0, C_7_0 });
            C.Add(new List<PictureBox> { null, C_1_1, C_2_1, C_3_1, C_4_1, C_5_1, C_6_1, C_7_1 });
            C.Add(new List<PictureBox> { null, C_1_2, C_2_2, C_3_2, C_4_2, C_5_2, C_6_2, C_7_2 });
            C.Add(new List<PictureBox> { null, C_1_3, C_2_3, C_3_3, C_4_3, C_5_3, C_6_3, C_7_3 });
            C.Add(new List<PictureBox> { null, C_1_4, C_2_4, C_3_4, C_4_4, C_5_4, C_6_4, C_7_4 });
            C.Add(new List<PictureBox> { null, C_1_5, C_2_5, C_3_5, C_4_5, C_5_5, C_6_5, C_7_5 });
            C.Add(new List<PictureBox> { null, C_1_6, C_2_6, C_3_6, C_4_6, C_5_6, C_6_6, C_7_6 });
            FormResize(null, null);
            Player.Stream = Hit_1;
            Player.Stream = Hit_2;
            Player.Stream = Hit_3;
            Player.Stream = Hit_4;
            Player.Stream = Hit_5;
            Player.Stream = Hit_6;
            Player.Stream = Hit_7;
            Player.Stream = Ans_No;
            Player.Stream = Ans_Yes;
            Player.Stream = Loss;
            Player.Stream = Win;
            Player.Stream = Start;
            Player.Stream = HelpTic;
            Player.Stream = Tie;
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
            toolTip.SetToolTip(AIcheckBox, "Toggles the Human vs AI or Human vs AI");
            toolTip.SetToolTip(Color_Select_Blue, "Selects the Blue color for Player 1");
            toolTip.SetToolTip(Color_Select_Red, "Selects the Red color for Player 1");
            toolTip.SetToolTip(button_Start, "Start the Game");
            toolTip.SetToolTip(button_Share, "Copies all the moves of the game to your clipboard");
            toolTip.SetToolTip(button_Restart, "Go back to the Main Menu");
            toolTip.SetToolTip(button_Exit, "Exits the Game");
            toolTip.SetToolTip(Restart_btn_Y, "End the Game and go back to the Main Menu");
            toolTip.SetToolTip(Restart_btn_N, "Continues the Game");
            toolTip.SetToolTip(Exit_btn_Y, "Exit the Game");
            toolTip.SetToolTip(Exit_btn_N, "Continues the Game");
            toolTip.SetToolTip(button_Tick, "Drops a single token");
            toolTip.SetToolTip(button_Fall, "Drop all remaining tokens");
            toolTip.SetToolTip(Undo_button, "Undo the last 2 Moves");
            toolTip.SetToolTip(Restart_button, "Restart the Game");
            toolTip.SetToolTip(FGameCheckBox, "Faster animations & Lower delay for AI response");
            toolTip.SetToolTip(LearnMCheckBox, "Guides you through the games with tips and insights");
            toolTip.SetToolTip(PredicitveCheckBox, "Enables 1-move predictions for the AI");
            toolTip.SetToolTip(StrategicCheckBox, "Enables Tactical moves and Future Strategies for the AI");
            toolTip.SetToolTip(HumanizedCheckBox, "Lets the AI guess your next 5 moves to determine his best move and allows Pattern recognition for the AI");
            toolTip.SetToolTip(Label_Easy, "Easy");
            toolTip.SetToolTip(Label_Medium, "Medium");
            toolTip.SetToolTip(Label_Intermediate, "Intermediate");
            toolTip.SetToolTip(Label_Hard, "Hard");
            toolTip.SetToolTip(Label_Merciless, "Merciless");
            toolTip.SetToolTip(TopPicture, "Connect 4");
            HelpT.Elapsed += HelpT_Elapsed;
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
        }

        // Initializes game components, starts the slide effect.
        private void Start_Click(object sender, EventArgs e)
        {
            if (MG.P[0] != -1)
            {
                Play(Start);
                button_Start.Location = new Point((GameOptions.Width + 10 - button_Start.Width) / 2, GameOptions.Height - GameOptions.Height / ((Label_Err_Color.Visible) ? 9 : 7));
                LastPlay = new Point(0, 0);
                MG.Loading = true;
                MG.Finished = false;
                TopPicture.Enabled = true;
                TopPicture.Hide();
                LoadingBox.Show();
                SetInGamePrefs();
                MG.StartMoveCounting();
                Undo_button.Image = (MG.Diff > 50) ? Undo_D : Undo;
                Undo_button.Enabled = MG.Diff <= 50;
                var T = new System.Timers.Timer(50);
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
                Turn_Circle.Invoke(new Action(TurnUpdate));
                if (MG.LearnMode)
                { Help_PictureBox.Invoke(new Action(Help_PictureBox.Show)); }
                else
                { Help_PictureBox.Invoke(new Action(Help_PictureBox.Hide)); }
                GameOptions.Hide();
                LossHelps = 0;
                if (!MG.vsAI)
                    for (var i = 1; i < 8; i++)
                        C[0][i].Image = BitLibrary["Arrow" + MG._Turn];
                else
                    for (var i = 1; i < 8; i++)
                        C[0][i].Image = BitLibrary["Arrow" + ((MG.LeftIsRed) ? 0 : 1)];
                MG.Loading = false;
            }
            else
            {
                if (!Label_Err_Color.Visible) Label_Err_Color.Show();
                button_Start.Location = new Point((GameOptions.Width + 10 - button_Start.Width) / 2, GameOptions.Height - GameOptions.Height / ((Label_Err_Color.Visible) ? 9 : 7));
            }
        }

        // Resets all controls, generates new MainGame instance and goes back to the Game Options menu.
        private void Button_Restart_Click(object sender, EventArgs e)
        {
            if (MG.Busy && !MG.Finished) return;

            if (!MG.Finished && MG.MoveCount > 3)
            { PromptRestart(); return; }

            Play(Ans_Yes);
            Falling = false;
            Moves = "";
            if (CurrentCol != 0)
                Col_Leave(CurrentCol);
            var _P = MG.P;
            var _Diff = MG.Diff;
            var _vsAI = MG.vsAI;
            var _FG = MG.FastGame;
            var _LeftIsRed = MG.LeftIsRed;
            MG = new MainGame()
            {
                P = _P,
                Diff = _Diff,
                vsAI = _vsAI,
                FastGame = _FG,
                LeftIsRed = _LeftIsRed,
                StrategicAI = StrategicCheckBox.Checked,
                PredictiveAI = PredicitveCheckBox.Checked,
                HumanizedAI = HumanizedCheckBox.Checked
            };
            LossHelps = 0;
            UpdateVisuals();
            button_Exit.Hide(); button_Restart.Hide(); button_Share.Hide();
            GameOptions.Show();
            Help_PictureBox.Hide();
            Turn_Circle.Hide();
            Undo_button.Hide();
            Restart_button.Hide();
            TopPicture.Image = Properties.Resources.Connect_4;
            TopPicture.Invoke(new Action(TopImgScaleSmall));
            TopPicture.Enabled = false;
            for (var i = 1; i < 8; i++)
                for (var j = 1; j < 7; j++)
                    C[j][i].Hide();
        }

        // Exits.
        private void Button_Exit_Click(object sender, EventArgs e)
        {
            Play(Ans_No);
            Application.Exit();
        }

        // Copies the Game's Moves to the clipboard.
        private void Button_Share_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(MG.Moves + "_");
        }

        // Undos the last two moves.
        private void Undo_button_Click(object sender, EventArgs e)
        {
            if ((Moves.Length == 0 && !GameOptions.Visible && MG.MoveCount > 1 && MG.Undos > 0)
                && ((MG.vsAI && MG._Turn == 0 && MG.Diff <= 50) || !MG.vsAI))
            {
                if (MG.MoveCount < 2) return;
                Point p1 = MG.Undo(), p2 = MG.Undo();
                MG.Busy = true;
                var T1 = new System.Timers.Timer(40);
                var T2 = new System.Timers.Timer(40);
                T1.Elapsed += (s1, e1) =>
                {
                    C[p1.Y][p1.X].Invoke(CUpdte, new object[] { C[p1.Y][p1.X], BitLibrary["W"] });
                    p1.Y--;
                    if (p1.Y == 0) T1.Dispose();
                    else C[p1.Y][p1.X].Invoke(CUpdte, new object[] { C[p1.Y][p1.X], BitLibrary["TC" + MG.P[1]] });
                    if ((T1.Enabled & T2.Enabled) == true)
                        MG.Busy = false;
                };
                T2.Elapsed += (s2, e2) =>
                {
                    C[p2.Y][p2.X].Invoke(CUpdte, new object[] { C[p2.Y][p2.X], BitLibrary["W"] });
                    p2.Y--;
                    if (p2.Y == 0) T2.Dispose();
                    else C[p2.Y][p2.X].Invoke(CUpdte, new object[] { C[p2.Y][p2.X], BitLibrary["TC" + MG.P[0]] });
                    if ((T1.Enabled & T2.Enabled) == true)
                        MG.Busy = false;
                };
                T1.Start();
                Thread.Sleep(50);
                T2.Start();
                Undo_button.Image = (MG.Undos < 2) ? Undo_D : Undo;
                Undo_button.Enabled = MG.Undos > 1;
            }
        }

        // Yes answer to the Restart Prompt.
        private void Restart_btn_Y_Click(object sender, EventArgs e)
        {
            Play(Ans_Yes);
            MG.Finished = true;
            MG.Busy = false;
            button_Fall.Enabled = true;
            button_Tick.Enabled = true;
            Restart_btn_N_Click(null, null);
            Button_Restart_Click(null, null);
        }

        // No answer to the Restart Prompt.
        private void Restart_btn_N_Click(object sender, EventArgs e)
        {
            MG.Busy = false;
            Restart_PB.Visible = false;
            Restart_BG_YN.Visible = false;
            Restart_PB_BG.Visible = false;
            Restart_btn_N.Visible = false;
            Restart_btn_Y.Visible = false;
            Turn_Circle.Enabled = true;
            TopPicture.Enabled = true;
            Restart_button.Enabled = true;
            button_Fall.Enabled = true;
            button_Tick.Enabled = true;
            Undo_button.Enabled = (MG.Undos > 1);
            if (!MG.Finished)
                for (var x = 1; x < 8; x++)
                    for (var y = 1; y < 7; y++)
                        MG.Case[y][x] *= -1;
            UpdateVisuals();
            if (CurrentCol != 0)
                Col_Enter(CurrentCol);
            Play(Ans_No);
        }

        // Yes answer to the Exit Prompt.
        private void Exit_btn_Y_Click(object sender, EventArgs e)
        {
            Play(Ans_Yes);
            Application.Exit();
        }

        // No answer to the Exit Prompt.
        private void Exit_btn_N_Click(object sender, EventArgs e)
        {
            MG.Busy = false;
            Exit_PB.Visible = false;
            Restart_BG_YN.Visible = false;
            Restart_PB_BG.Visible = false;
            Exit_btn_N.Visible = false;
            Exit_btn_Y.Visible = false;
            Turn_Circle.Enabled = true;
            TopPicture.Enabled = true;
            Restart_button.Enabled = true;
            button_Fall.Enabled = true;
            button_Tick.Enabled = true;
            Undo_button.Enabled = (MG.Undos > 1);
            if (!MG.Finished)
                for (var x = 1; x < 8; x++)
                    for (var y = 1; y < 7; y++)
                        MG.Case[y][x] *= -1;
            UpdateVisuals();
            if (CurrentCol != 0)
                Col_Enter(CurrentCol);
            Play(Ans_No);
        }

        // Falls all remaining moves in the Game Review.
        private void Button_Fall_Click(object sender, EventArgs e)
        {
            Falling = true;
            button_Fall.Visible = button_Tick.Visible = false;
            Col_Click(int.Parse(Moves[0].ToString()), true);
            Moves = Moves.Substring(1, Moves.Length - 1);
            Play(Ans_Yes);
        }

        // Drops one token from the remaining moves in the Game Review.
        private void Button_Tick_Click(object sender, EventArgs e)
        {
            if (!MG.Busy)
            {
                Col_Click(int.Parse(Moves[0].ToString()), true);
                Moves = Moves.Substring(1, Moves.Length - 1);
                if (Moves[0] == '_')
                {
                    button_Fall.Visible = button_Tick.Visible = false;
                }
                else
                    Col_Enter(int.Parse(Moves[0].ToString()), true);
                Play(Ans_No);
            }
        }

        // Changes the color of the Start button and Circles and finally sets the player colors in MG.
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
                Play(Ans_Yes);
            }
        }
        
        // Changes the color of the Start button and Circles and finally sets the player colors in MG.
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
                Play(Ans_No);
            }
        }

        // Enables or Disables all controls related to the AI.
        private void VsAI_ChkChanged(object sender, EventArgs e)
        {
            if(AIcheckBox.Checked)
            {
                DiffBar.Enabled =
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
                DiffBar.Enabled =
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
            Play(Ans_No);
        }        
        
        // The following change the Difficulty bar as needed for each control change.
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
            Play(Ans_Yes);
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
            Play(Ans_Yes);
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
            Play(Ans_Yes);
        }

        private void LearnMCheckBoxChkChanged(object sender, EventArgs e)
        {
            if (InfiniteLoop) return;
            if (LearnMCheckBox.Checked)
            {
                DiffBar.Value = Math.Min(25, DiffBar.Value);
                if (DiffBar.Value == 25)
                    DiffBar_Scroll(null, null);
            }
            Play(Ans_No);
        }

        private void FGameCheckBoxChkChanged(object sender, EventArgs e)
        {
            Play(Ans_No);
        }

        private void SoundsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Play(Ans_Yes);
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

        // The Following sets the Diff Bar to 0 / 25 / 50 / 75 / 100.
        private void Label_Easy_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 0;
            DiffBar_Scroll(null, null);
            Play(Hits[2]);
        }

        private void Label_Medium_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 25;
            DiffBar_Scroll(null, null);
            Play(Hits[3]);
        }

        private void Label_Intermediate_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 50;
            DiffBar_Scroll(null, null);
            Play(Hits[4]);
        }

        private void Label_Hard_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 75;
            DiffBar_Scroll(null, null);
            Play(Hits[5]);
        }

        private void Label_Merciless_Click(object sender, EventArgs e)
        {
            DiffBar.Value = 100;
            DiffBar_Scroll(null, null);
            Play(Hits[6]);
        }

        // Shows and Hides the help label on hover and leave.
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

        // Resets and hides the Help label and icon when the timer ends.
        private void HelpT_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            HelpT.Stop();
            label_Help.Invoke(new Action(HelpResetText));
            label_Help.Invoke(new Action(label_Help.Hide));
            HelpPrompt(false);
        }

        // Returns the lowest value between W or H for the same base ratio.
        private int HWDiff(int W, int H)
        {
            H = H * 2 / 3;
            if (W > H)
                return H * 11 / 10;
            return W * 11 / 10;
        }

        // States the Game is Bust while the form controls change location and size.
        private void FormResize(object sender, EventArgs e)
        {
            var b = MG.Busy;
            MG.Busy = true;
            ResizeForm();
            MG.Busy = b;
        }

        // Changes every control's size and location according to the window size.
        private void ResizeForm()
        {
            //This is a complete shitstorm hanging by a thread
            int W, H, GOW, GOH, CS; int[] CBL;
            try
            { W = ActiveForm.Width; H = ActiveForm.Height; }
            catch (Exception) { W = 433; H = 650; }
            TopImgSize[0] = (int)(H * 0.166);
            TopImgSize[1] = (int)(H * 0.2383);
            GOW = Math.Min(1200, W - 40);
            GOH = H - TopImgSize[0] - 58;

            GameOptions.Location = (GOW < 1200) ? new Point(12, 6 + TopImgSize[0]) : new Point((W - GOW - 12) / 2, 6 + TopImgSize[0]);
            {   //In-Game
                CS = (HWDiff(W, H)) / 13 + 10;
                label_Help.Font = new Font("Century Gothic", HWDiff(W, H) / 47, FontStyle.Italic);
                Help_PictureBox.Size = new Size((int)(HWDiff(W, H) / 14.16), (int)(HWDiff(W, H) / 14.16));
                label_Help.Location = new Point(W / 21 + Help_PictureBox.Width, H - CS - label_Help.Height);
                label_Help.Width = W - (W / 20) - label_Help.Location.X + 10;
                Help_PictureBox.Location = new Point(W / 42, H - CS - (int)(label_Help.Height / 1.33) - (int)((Help_PictureBox.Height - 30) / 2));
                CBL = new int[2] { (GameOptions.Location.X + GOW / 2 - (CS * 7 ) / 2), (30 + ((H - TopImgSize[0]) / 2 - (CS * 7 - 7) / 2 - 6) + TopImgSize[0]) };
                if (MG.LearnMode && CBL[1] + (CS * 7) + 20 > label_Help.Location.Y)
                {
                    CBL[1] -= CBL[1] + (CS * 7) + 20 - label_Help.Location.Y;
                    TopImgSize[0] -= CBL[1] + (CS * 7) + 20 - label_Help.Location.Y;
                    TopImgSize[1] -= CBL[1] + (CS * 7) + 20 - label_Help.Location.Y;
                }
                for (var x = 1; x < 8; x++)
                {
                    for (var y = 0; y < 7; y++)
                    {
                        C[y][x].Location = new Point(CBL[0] + CS * (x - 1), CBL[1] + CS * y);
                        C[y][x].Size = new Size(CS, CS);
                    }
                }
                Undo_button.Width = Undo_button.Height = Restart_button.Width = Restart_button.Height = (int)(HWDiff(W, H) / 14.16);
                Undo_button.Location = new Point(15, 15);
                Restart_button.Location = new Point(W - 30 - (int)(HWDiff(W, H) / 14.16), 15);
                button_Tick.Height = button_Fall.Height = button_Share.Width = button_Share.Height = button_Exit.Height = button_Restart.Height = button_Start.Height = (H - 650) / 25 + 45;
                button_Tick.Width = button_Fall.Width = button_Exit.Width = button_Restart.Width = button_Start.Width = (Math.Min(1000, W) - 433) / 6 + 100;
                button_Tick.Font = button_Fall.Font = button_Exit.Font = button_Start.Font = button_Restart.Font = new Font("Century Gothic", (float)(4.25 + HWDiff(W, H) / 42.5), FontStyle.Bold);
                if (MG.P[0] == -1)
                    button_Start.Font = new Font("Century Gothic", (float)(4.25 + HWDiff(W, H) / 42.5));
                button_Exit.Location = new Point(GameOptions.Location.X + (GOW * 3 / 4) - button_Exit.Width / 2, (int)((((CBL[1] + CS - TopImgSize[1]) / 2.85) + TopImgSize[1])));
                button_Restart.Location = new Point(GameOptions.Location.X + (GOW / 4) - button_Exit.Width / 2, (int)((((CBL[1] + CS - TopImgSize[1]) / 2.85) + TopImgSize[1])));
                button_Share.Location = new Point(GameOptions.Location.X + (GOW - button_Share.Width) / 2, (int)((((CBL[1] + CS - TopImgSize[1]) / 2.85) + TopImgSize[1])));
                Turn_Circle.Height = Turn_Circle.Width = (int)(1.45 * CS);
                Turn_Circle.Location = new Point(GameOptions.Location.X + (GOW - Turn_Circle.Height) / 2, (int)((((CBL[1] - TopImgSize[0]) / 2.5) + TopImgSize[0])));
                button_Fall.Location = new Point(GameOptions.Location.X + (GOW / 4) - button_Exit.Width / 2, (int)((((CBL[1] - TopImgSize[0]) / 2.5) + (Turn_Circle.Height - button_Tick.Height) / 2 + TopImgSize[0])));
                button_Tick.Location = new Point(GameOptions.Location.X + (GOW * 3 / 4) - button_Exit.Width / 2, (int)((((CBL[1] - TopImgSize[0]) / 2.5) + (Turn_Circle.Height - button_Tick.Height) / 2 + TopImgSize[0])));
                Restart_PB_BG.Width = W;
                Restart_PB_BG.Location = new Point(0, H / 3 + 20);
                Restart_PB.Width = Exit_PB.Width = Math.Min(650, W * 9 / 10);
                Exit_btn_Y.Height = Exit_btn_N.Height = Restart_btn_Y.Height = Restart_btn_N.Height = ((H - 650) / 25 + 45) * 8 / 10;
                Exit_btn_Y.Width = Exit_btn_N.Width = Restart_btn_Y.Width = Restart_btn_N.Width = ((Math.Min(1000, W) - 433) / 6 + 100) * 8 / 10;
                Exit_btn_Y.Font = Exit_btn_N.Font = Restart_btn_Y.Font = Restart_btn_N.Font = new Font("Century Gothic", (float)(2.25 + HWDiff(W, H) / 42.5), FontStyle.Bold);
                Exit_btn_N.Location = Restart_btn_N.Location = new Point(GameOptions.Location.X + (GOW * 3 / 4) - Restart_btn_N.Width / 2, H * 2 / 3 - 30);
                Exit_btn_Y.Location = Restart_btn_Y.Location = new Point(GameOptions.Location.X + (GOW / 4) - Restart_btn_Y.Width / 2, H * 2 / 3 - 30);
                Restart_BG_YN.Height = Restart_PB_BG.Height = Restart_btn_N.Height * 7 / 4;
                Restart_BG_YN.Width = W;
                Restart_BG_YN.Location = new Point(0, H * 2 / 3 - 30 - ((Restart_BG_YN.Height - Restart_btn_N.Height) / 2));
                Exit_PB.Location = Restart_PB.Location = new Point((W - Restart_PB.Width - 15) / 2, (Restart_PB_BG.Height - 58) / 2 + (H / 3) + 20);
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
                TopPicture.Width = W - 40;
                LoadingBox.Size = new Size(W, H);
                LoadingBox.Location = new Point(0, 0);
                GameOptions.Width = GOW; GOW += 40;
                GameOptions.Height = GOH;
                TopPicture.Height = TopImgSize[(MG.Finished && !GameOptions.Visible) ? 1 : 0];
                FGameCheckBox.Location = new Point(FGameCheckBox.Location.X, GOH / 15 + 6);
                Color_Select_Blue.Size = Color_Select_Red.Size = new Size((int)(HWDiff(W, H) / 3.5), (int)(HWDiff(W, H) / 3.5));
                Color_Select_Red.Location = new Point((int)(0.9 * ((GOW - 40 - Color_Select_Red.Width) / 4)), GOH / 2 + GOH / 16 - 10);
                Color_Select_Blue.Location = new Point((int)(1.1 * ((GOW - 40 - Color_Select_Red.Width) * 3 / 4)), GOH / 2 + GOH / 16 - 10);
                Label_Err_Color.Location = new Point((GOW - 40) / 2 - 86, GOH - (int)(GOH / 5.89));
                button_Start.Location = new Point((GOW - 30 - button_Start.Width) / 2, GOH - GOH / ((Label_Err_Color.Visible) ? 9 : 7));
                Color_Label.Location = new Point((int)((W - 370) * .2) + 15, GOH / 2);
                DiffBar.Width = GOW - 104;
                Label_Easy.Font = Label_Hard.Font = Label_Medium.Font = Label_Merciless.Font = Label_Intermediate.Font = new Font("Century Gothic", (float)(7.25 + HWDiff(W, H) / 42.5));
                int d = DiffBar.Width;
                Label_Merciless.Location = new Point(GOW - 98 - (Label_Easy.Width - 31) / 2, GOH / 4 + 10 + 48);
                Label_Easy.Location = new Point(DiffBar.Location.X - (Label_Easy.Width - 31) / 2, GOH / 4 + 10 + 48);
                Label_Hard.Location = new Point(DiffBar.Location.X + 1 + (int)((Label_Hard.Width - 31) * .3) + (d - Label_Easy.Width + 4) / 4 * 3, GOH / 4 + 10 + 48);
                Label_Medium.Location = new Point(DiffBar.Location.X - (int)((Label_Medium.Width - 31) * .3) + (d - Label_Easy.Width + 4) / 4, GOH / 4 + 10 + 48);
                Label_Intermediate.Location = new Point(DiffBar.Location.X + (d - Label_Easy.Width + 4) / 2, GOH / 4 + 10 + 48);
                Color_Label.Font = GameOptions.Font = AIcheckBox.Font = new Font("Century Gothic", (float)(6.5 + HWDiff(W, H) / 63.75));
                Difficulty_Label.Font = HumanizedCheckBox.Font = PredicitveCheckBox.Font = StrategicCheckBox.Font = new Font("Century Gothic", (float)(3.75 + HWDiff(W, H) / 63.75));
                AIDiff_Label.Font = SoundsCheckBox.Font = LearnMCheckBox.Font = FGameCheckBox.Font = new Font("Century Gothic", (float)(4.5 + HWDiff(W, H) / 63.75));
                Difficulty_Label.Location = new Point((int)(-18 + 1.45 * AIDiff_Label.Width), GOH / 4 + 70 - 3 * Difficulty_Label.Height);
                AIDiff_Label.Location = new Point(25, GOH / 4 + 60 - (int)(2.5 * AIDiff_Label.Height));
                DiffBar.Location = new Point(DiffBar.Location.X, GOH / 4 + 10 + 25);
                int p = GOH / 15 + 6, p1 = (int)(GameOptions.Width * .965) - HumanizedCheckBox.Width - (AIcheckBox.Width / 5);
                AIcheckBox.Location = new Point(p1, p); p += (int)(AIcheckBox.Height * 1.12);
                PredicitveCheckBox.Location = new Point(p1 + AIcheckBox.Width / 5, p); p += (int)(1.5 * PredicitveCheckBox.Height - 11.5);
                StrategicCheckBox.Location = new Point(p1 + AIcheckBox.Width / 5, p); p += (int)(1.5 * StrategicCheckBox.Height - 11.5);
                HumanizedCheckBox.Location = new Point(p1 + AIcheckBox.Width / 5, p);
                if (AIcheckBox.Location.X - (FGameCheckBox.Width + FGameCheckBox.Location.X) > (int)((LearnMCheckBox.Width) * 1.2))
                {
                    LearnMCheckBox.Location = new Point((int)((FGameCheckBox.Width + FGameCheckBox.Location.X) * 1.2), GOH / 15 + 6);

                    if (AIcheckBox.Location.X - (LearnMCheckBox.Width + LearnMCheckBox.Location.X) > (int)((SoundsCheckBox.Width) * 1.2))
                        SoundsCheckBox.Location = new Point((int)((LearnMCheckBox.Width + LearnMCheckBox.Location.X) * 1.1), GOH / 15 + 6);
                    else
                        SoundsCheckBox.Location = new Point(30, LearnMCheckBox.Height + LearnMCheckBox.Location.Y);
                }
                else
                {
                    LearnMCheckBox.Location = new Point(30, FGameCheckBox.Height + FGameCheckBox.Location.Y);
                    SoundsCheckBox.Location = new Point(30, LearnMCheckBox.Height + LearnMCheckBox.Location.Y);
                }
            }
        }

        // Checks if S is an acceptable value for the Game Review.
        private bool CheckClipboard(string S)
        {
            if (S.Length < 12) return false;
            for (var i = 0; i < S.Length - 1; i++)
                if (!char.IsDigit(S[i]) && S[i] != '_') return false;
            for (var i = 4; i < S.Length - 1; i++)
                if (S[i] == '0' || S[i] == '8' || S[i] == '9') return false;
            return (S[3] == '0' || S[3] == '1');
        }

        // Handles keyboard presses in the Game.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (Falling) return true;

            if(keyData == (Keys.Control | Keys.V))
            {
                if (MG.MoveCount == 0 || GameOptions.Visible)
                {
                    var clip = Clipboard.GetText();
                    if (CheckClipboard(clip))
                    {
                        if (MG.P[0] == -1)
                            Color_Select_Red_Click(null, null);
                        if (GameOptions.Visible)
                            Start_Click(null, null);
                        else
                            button_Fall.Visible = button_Tick.Visible = true;
                        MG._Turn = int.Parse(clip[3].ToString());
                        MG.vsAI = false;
                        MG.Diff = int.Parse(clip.Substring(0, 3));
                        MG.HumanizedAI = (MG.Diff == 100);
                        MG.PredictiveAI = (MG.Diff >= 25);
                        MG.StrategicAI = (MG.Diff >= 75);
                        MG.LearnMode = false;
                        TurnUpdate();
                        Moves = clip.Substring(4, clip.Length - 4);
                        for (var I = 1; I < 8; I++)
                            lock (C[0][I])
                                C[0][I].Image = BitLibrary["Arrow" + MG._Turn];
                        Col_Enter(int.Parse(Moves[0].ToString()), true);
                    }
                }
            }
            
            if (keyData == (Keys.Control | Keys.Z))
            {
                Undo_button_Click(null, null);
            }

            if (GameOptions.Visible)
            {
                if (DiffBar.Enabled && (keyData == Keys.Right || keyData == Keys.D))
                {
                    DiffBar.Value = Math.Min(100, DiffBar.Value + 5);
                    DiffBar_Scroll(null, null);
                    return true;
                }

                if (DiffBar.Enabled && (keyData == Keys.Left || keyData == Keys.A))
                {
                    DiffBar.Value = Math.Max(0, DiffBar.Value - 5);
                    DiffBar_Scroll(null, null);
                    return true;
                }

                if (keyData == Keys.D1 || keyData == Keys.NumPad1)
                {
                    Color_Select_Red_Click(null, null);
                    return true;
                }

                if (keyData == Keys.D2 || keyData == Keys.NumPad2)
                {
                    Color_Select_Blue_Click(null, null);
                    return true;
                }

                switch (keyData)
                {
                    case Keys.F1: FGameCheckBox.Checked = !FGameCheckBox.Checked; return true;
                    case Keys.F2: if (AIcheckBox.Checked) LearnMCheckBox.Checked = !LearnMCheckBox.Checked; return true;
                    case Keys.F3: SoundsCheckBox.Checked = !SoundsCheckBox.Checked; return true;
                    case Keys.F4: AIcheckBox.Checked = !AIcheckBox.Checked; return true;
                    case Keys.F5: if (AIcheckBox.Checked) PredicitveCheckBox.Checked = !PredicitveCheckBox.Checked; return true;
                    case Keys.F6: if (AIcheckBox.Checked) StrategicCheckBox.Checked = !StrategicCheckBox.Checked; return true;
                    case Keys.F7: if (AIcheckBox.Checked) HumanizedCheckBox.Checked = !HumanizedCheckBox.Checked; return true;
                }
            }

            if (keyData == Keys.Back)
            {
                if (TopPicture.Enabled && !Exit_PB.Visible)
                {
                    Button_Restart_Click(null, null);
                    return true;
                }
                if (Restart_btn_N.Visible)
                {
                    Restart_btn_N_Click(null, null);
                    return true;
                }
                if (Exit_btn_N.Visible)
                {
                    Exit_btn_N_Click(null, null);
                    return true;
                }
            }

            if (keyData == Keys.Enter)
            {
                if (button_Restart.Visible)
                {
                    Button_Restart_Click(null, null);
                    return true;
                }
                if (Restart_btn_Y.Visible)
                {
                    Restart_btn_Y_Click(null, null);
                    return true;
                }
                if (Exit_btn_Y.Visible)
                {
                    Exit_btn_Y_Click(null, null);
                    return true;
                }
                if (button_Fall.Visible)
                {
                    Button_Fall_Click(null, null);
                    return true;
                }
                if (GameOptions.Visible)
                {
                    Start_Click(null, null);
                    return true;
                }
            }

            if (keyData == Keys.Escape)
            {
                if (button_Exit.Visible)
                    Application.Exit();
                else if (Restart_btn_N.Visible)
                    Restart_btn_N_Click(null, null);
                else if (Exit_btn_N.Visible)
                    Exit_btn_N_Click(null, null);
                else
                    PromptExit();
                return true;
            }

            if (button_Tick.Visible && (keyData == Keys.Down || keyData == Keys.S || keyData == Keys.Space))
            {
                Button_Tick_Click(null, null);
                return true;
            }

            if (!GameOptions.Visible && (keyData == Keys.Left || keyData == Keys.A))
            {
                int NMI;
                if (CurrentCol == 0) CurrentCol = 5;
                for (NMI = CurrentCol - 1; NMI != CurrentCol; NMI--)
                {
                    if (NMI == 0)
                        NMI = 7;
                    if (MG.Case[1][NMI] == 0)
                        break;
                }
                Col_Enter(NMI);
                return true;    // indicate that you handled this keystroke
            }

            if (!GameOptions.Visible && (keyData == Keys.Right || keyData == Keys.D))
            {
                int NMI;
                if (CurrentCol == 0) CurrentCol = 3;
                for (NMI = CurrentCol + 1; NMI != CurrentCol; NMI++)
                {
                    if (NMI == 8)
                        NMI = 1;
                    if (MG.Case[1][NMI] == 0)
                        break;
                }
                Col_Enter(NMI);
                return true;    // indicate that you handled this keystroke
            }

            if (!GameOptions.Visible && MG.Case[1][1] == 0 && (keyData == Keys.Q || keyData == Keys.NumPad1 || keyData == Keys.D1))
            { Col_Leave(CurrentCol); Col1_Enter(null, null); return true; }
            if (!GameOptions.Visible && MG.Case[1][2] == 0 && (keyData == Keys.W || keyData == Keys.NumPad2 || keyData == Keys.D2))
            { Col_Leave(CurrentCol); Col2_Enter(null, null); return true; }
            if (!GameOptions.Visible && MG.Case[1][3] == 0 && (keyData == Keys.E || keyData == Keys.NumPad3 || keyData == Keys.D3))
            { Col_Leave(CurrentCol); Col3_Enter(null, null); return true; }
            if (!GameOptions.Visible && MG.Case[1][4] == 0 && (keyData == Keys.R || keyData == Keys.NumPad4 || keyData == Keys.D4))
            { Col_Leave(CurrentCol); Col4_Enter(null, null); return true; }
            if (!GameOptions.Visible && MG.Case[1][5] == 0 && (keyData == Keys.T || keyData == Keys.NumPad5 || keyData == Keys.D5))
            { Col_Leave(CurrentCol); Col5_Enter(null, null); return true; }
            if (!GameOptions.Visible && MG.Case[1][6] == 0 && (keyData == Keys.Y || keyData == Keys.NumPad6 || keyData == Keys.D6))
            { Col_Leave(CurrentCol); Col6_Enter(null, null); return true; }
            if (!GameOptions.Visible && MG.Case[1][7] == 0 && (keyData == Keys.U || keyData == Keys.NumPad7 || keyData == Keys.D7))
            { Col_Leave(CurrentCol); Col7_Enter(null, null); return true; }

            if (CurrentCol != 0 && !GameOptions.Visible && (keyData == Keys.Down || keyData == Keys.S || keyData == Keys.Space || keyData == Keys.Enter))
            {
                Col_Click(CurrentCol);
                return true;    // indicate that you handled this keystroke
            }

            // Call the base class
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Prompts Exit if the User tried to close the App.
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if(e.CloseReason == CloseReason.UserClosing && !GameOptions.Visible)
            {
                if (Restart_PB.Visible)
                    Restart_btn_N_Click(null, null);
                e.Cancel =  true;
                if(!Exit_PB.Visible)
                    PromptExit();
            }
        }

        //Click, Hover, Leave Events.
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
        { Col_Enter(1); }
        private void Col2_Enter(object sender, EventArgs e)
        { Col_Enter(2); }
        private void Col3_Enter(object sender, EventArgs e)
        { Col_Enter(3); }
        private void Col4_Enter(object sender, EventArgs e)
        { Col_Enter(4); }
        private void Col5_Enter(object sender, EventArgs e)
        { Col_Enter(5); }
        private void Col6_Enter(object sender, EventArgs e)
        { Col_Enter(6); }
        private void Col7_Enter(object sender, EventArgs e)
        { Col_Enter(7); }

        private void Col1_Leave(object sender, EventArgs e)
        { Col_Leave(1); }
        private void Col2_Leave(object sender, EventArgs e)
        { Col_Leave(2); }
        private void Col3_Leave(object sender, EventArgs e)
        { Col_Leave(3); }
        private void Col4_Leave(object sender, EventArgs e)
        { Col_Leave(4); }
        private void Col5_Leave(object sender, EventArgs e)
        { Col_Leave(5); }
        private void Col6_Leave(object sender, EventArgs e)
        { Col_Leave(6); }
        private void Col7_Leave(object sender, EventArgs e)
        { Col_Leave(7); }

        // Debugging Methods.
        private void DebugAI_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
#if DEBUG
            MG.vsAI = DebugAI_CheckBox.Checked;
#endif
        }
        private void DebugStateButton_Click(object sender, EventArgs e)
        {
#if DEBUG
            if(e != null)
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
