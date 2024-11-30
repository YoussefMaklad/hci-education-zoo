/*
	TUIO C# Demo - part of the reacTIVision project
	Copyright (c) 2005-2016 Martin Kaltenbrunner <martin@tuio.org>

	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Timers;
using System.Media; // Add this namespace for sound playback
using TUIO;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

public class TuioDemo : Form, TuioListener
{
    private TuioClient client;
    private Dictionary<long, TuioObject> objectList;
    private Dictionary<long, TuioCursor> cursorList;
    private Dictionary<long, TuioBlob> blobList;
    private List<string> menuItems = new List<string> { "sound", "Habitat", "Description", "Name", "newitem" };
    private int currentItemIndex = 0;
    private bool isRotating = false;
    private bool stableFor5Seconds = false;
    private SolidBrush menuBrush = new SolidBrush(Color.White);
    private SolidBrush highlightBrush = new SolidBrush(Color.Yellow);
    private System.Timers.Timer stabilityTimer;
    public static int width, height;
    private int window_width = 1024;
    private int window_height = 686;
    private int window_left = 100;
    private int window_top = 100;
    private int screen_width = Screen.PrimaryScreen.Bounds.Width;
    private int screen_height = Screen.PrimaryScreen.Bounds.Height;
    private System.Timers.Timer highlightTimer; // Timer for 10 seconds highlight
    private bool tigerOnMenuItemStable = false; // Flag to check if tiger is stable on an item
    private string currentMenuItem; // Current menu item where the tiger is stable
    private bool tigerVisible = false; // Check if tiger is on screen
    private bool fullscreen;
    private bool verbose;



    Font font = new Font("Arial", 10.0f);
    SolidBrush fntBrush = new SolidBrush(Color.White);
    SolidBrush bgrBrush = new SolidBrush(Color.FromArgb(0, 0, 64));
    SolidBrush curBrush = new SolidBrush(Color.FromArgb(192, 0, 192));
    SolidBrush objBrush = new SolidBrush(Color.FromArgb(64, 0, 0));
    SolidBrush blbBrush = new SolidBrush(Color.FromArgb(64, 64, 64));
    Pen curPen = new Pen(new SolidBrush(Color.Blue), 1);
    private Dictionary<string, string> audioFiles = new Dictionary<string, string>
    {
        { "Name", "tiger-roar-loudly-193229 (1).wav" },
        { "Habitat", "tigerdescription.wav" },
        { "Description", "tigername.wav" }, //tiger name
         { "sound", "tigerhabitat.wav" },
         { "newitem", "tiger-roar-loudly-193229.wav" },




    };


    public TuioDemo(int port)
    {

        verbose = false;
        fullscreen = false;
        width = window_width;
        height = window_height;

        this.ClientSize = new System.Drawing.Size(width, height);
        this.Name = "TuioDemo";
        this.Text = "TuioDemo";

        this.Closing += new CancelEventHandler(Form_Closing);
        this.KeyDown += new KeyEventHandler(Form_KeyDown);

        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.UserPaint |
                        ControlStyles.DoubleBuffer, true);

        objectList = new Dictionary<long, TuioObject>(128);
        cursorList = new Dictionary<long, TuioCursor>(128);
        blobList = new Dictionary<long, TuioBlob>(128);

        client = new TuioClient(port);
        client.addTuioListener(this);


        client.connect();
        // Initialize the timer for rotation stability
        stabilityTimer = new System.Timers.Timer(2000);  // 5 seconds
        stabilityTimer.Elapsed += OnStabilityTimerElapsed;
        stabilityTimer.AutoReset = false;


        // Initialize stability and highlight timers
        highlightTimer = new System.Timers.Timer(2000); // 5 seconds stability
        highlightTimer.Elapsed += OnHighlightTimerElapsed;
        highlightTimer.AutoReset = false;

    }
    private void OnStabilityTimerElapsedForMenu(object sender, ElapsedEventArgs e)
    {
        stableFor5Seconds = true;
        isRotating = false;

        // Check if the stable object is the tiger on a menu item
        if (showTiger && tigerVisible)
        {
            tigerOnMenuItemStable = true;
            currentMenuItem = menuItems[currentItemIndex];

            // Start the highlight timer
            highlightTimer.Stop();
            highlightTimer.Start();
        }

        // Update to the next menu item
        currentItemIndex = (currentItemIndex + 1) % menuItems.Count;
        Invalidate();  // Redraw to show the new highlighted item
    }
    private void UpdateTigerVisibility()
    {
        // Update tigerVisible based on the presence of the tiger's specific SymbolID
        tigerVisible = false;
        foreach (var obj in objectList.Values)
        {
            if (obj.SymbolID == 1)  // Assuming 1 is the SymbolID for the tiger
            {
                tigerVisible = true;
                break;
            }
        }
        showMenu = tigerVisible;  // Only show menu if the tiger is visible
    }

    // Adjust OnHighlightTimerElapsed to advance the index after playing the audio
    private void OnHighlightTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (tigerOnMenuItemStable && showTiger && tigerVisible)
        {
            // Play the sound based on the current menu item
            PlayHighlightSound(currentMenuItem);

            // Show message for feedback (optional)
            // MessageBox.Show($"Tiger has been stable on {currentMenuItem} for 5 seconds.");

            // Advance to the next menu item after playing audio
            currentItemIndex = (currentItemIndex + 1) % menuItems.Count;
            tigerOnMenuItemStable = false; // Reset stability check
        }
    }
    private void PlayHighlightSound(string menuItem)
    {
        if (audioFiles.ContainsKey(menuItem))
        {
            try
            {
                SoundPlayer player = new SoundPlayer(audioFiles[menuItem]);
                player.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error playing sound: " + ex.Message);
            }
        }
    }


    private void PlayHighlightSound()
    {
        try
        {
            SoundPlayer player = new SoundPlayer("tigername.wav"); // Replace with your sound file path
            player.Play();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error playing sound: " + ex.Message);
        }
    }

    private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {

        if (e.KeyData == Keys.F1)
        {
            if (fullscreen == false)
            {

                width = screen_width;
                height = screen_height;

                window_left = this.Left;
                window_top = this.Top;

                this.FormBorderStyle = FormBorderStyle.None;
                this.Left = 0;
                this.Top = 0;
                this.Width = screen_width;
                this.Height = screen_height;

                fullscreen = true;
            }
            else
            {

                width = window_width;
                height = window_height;

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Left = window_left;
                this.Top = window_top;
                this.Width = window_width;
                this.Height = window_height;

                fullscreen = false;
            }
        }
        else if (e.KeyData == Keys.Escape)
        {
            this.Close();

        }
        else if (e.KeyData == Keys.V)
        {
            verbose = !verbose;
        }

    }

    private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        client.removeTuioListener(this);

        client.disconnect();
        System.Environment.Exit(0);
    }

    public void addTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Add(o.SessionID, o);
        }
        UpdateTigerVisibility();  // Check if tiger is visible after adding object
        if (verbose) Console.WriteLine("add obj " + o.SymbolID + " (" + o.SessionID + ") " + o.X + " " + o.Y + " " + o.Angle);
    }

    public void updateTuioObject(TuioObject o)
    {
        double rotationSpeed = o.RotationSpeed;

        if (Math.Abs(rotationSpeed) > 0 && showTiger && tigerVisible)
        {
            // Detect rotation, start stability timer
            if (!isRotating)
            {
                isRotating = true;
                stableFor5Seconds = false;
                stabilityTimer.Stop();
                stabilityTimer.Start();
            }
        }
        else if (isRotating)
        {
            // Rotation has stopped, check stability
            stabilityTimer.Start();
        }
        UpdateTigerVisibility();  // Check if tiger is visible after updating object
    }

    // Adjusted OnStabilityTimerElapsed without advancing the index immediately
    private void OnStabilityTimerElapsed(object sender, ElapsedEventArgs e)
    {
        stableFor5Seconds = true;
        isRotating = false;

        // Check if the stable object is the tiger on a menu item
        if (showTiger)
        {
            tigerOnMenuItemStable = true;
            currentMenuItem = menuItems[currentItemIndex]; // Lock the current item

            // Start the highlight timer
            highlightTimer.Stop();
            highlightTimer.Start();
        }

        Invalidate();  // Redraw to show the highlighted item
    }




    /*
    private void OnHighlightTimerElapsed(object sender, ElapsedEventArgs e)
    {
        //MessageBox.Show("You have selected " + menuItems[currentItemIndex]);
        // Check if the currentItemIndex is valid
        if (showTiger)
        {
            //MessageBox.Show("You have selected " + menuItems[currentItemIndex]);
            // Play sound when an item is highlighted
            PlayHighlightSound();
            Invalidate();
        }

    }*/



    public void removeTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Remove(o.SessionID);
        }
        UpdateTigerVisibility();  // Check if tiger is visible after removing object
        if (verbose) Console.WriteLine("del obj " + o.SymbolID + " (" + o.SessionID + ")");
    }

    public void addTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Add(c.SessionID, c);
        }
        if (verbose) Console.WriteLine("add cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y);
    }

    public void updateTuioCursor(TuioCursor c)
    {
        if (verbose) Console.WriteLine("set cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y + " " + c.MotionSpeed + " " + c.MotionAccel);
    }

    public void removeTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Remove(c.SessionID);
        }
        if (verbose) Console.WriteLine("del cur " + c.CursorID + " (" + c.SessionID + ")");
    }

    public void addTuioBlob(TuioBlob b)
    {
        lock (blobList)
        {
            blobList.Add(b.SessionID, b);
        }
        if (verbose) Console.WriteLine("add blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area);
    }

    public void updateTuioBlob(TuioBlob b)
    {

        if (verbose) Console.WriteLine("set blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area + " " + b.MotionSpeed + " " + b.RotationSpeed + " " + b.MotionAccel + " " + b.RotationAccel);
    }

    public void removeTuioBlob(TuioBlob b)
    {
        lock (blobList)
        {
            blobList.Remove(b.SessionID);
        }
        if (verbose) Console.WriteLine("del blb " + b.BlobID + " (" + b.SessionID + ")");
    }

    public void refresh(TuioTime frameTime)
    {
        Invalidate();
    }

    private bool hasForest = false;
    private bool hasdesert = false;
    private bool hassea = false;
    private bool showTiger = false;
    private bool showMenu = false;
    private bool showLion = false;
    private bool showFox = false;
    private bool showfish = false;
    private bool showshark = false;
    private bool showwhale = false;
    private bool showlooney = false;
    private bool showpolarbear = false;
    private bool showpengiun = false;
    private bool showError = false;

    private bool mismatchedObjectDetected = false;
    private bool showInitialImage = true;

    public string AnimalName { get; private set; }
    public int NewRadius { get; private set; }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        Graphics g = pevent.Graphics;
        g.FillRectangle(bgrBrush, new Rectangle(0, 0, width, height));

        // Paths for images
        string forest = Path.Combine(Environment.CurrentDirectory, "forrest1.jpg");
        string sea = Path.Combine(Environment.CurrentDirectory, "sea1.jpg");
        string desert = Path.Combine(Environment.CurrentDirectory, "ice.jpeg");
        string initialImagePath = Path.Combine(Environment.CurrentDirectory, "poster.png");
        string errorpath = Path.Combine(Environment.CurrentDirectory, "error.png");
     

        if (showInitialImage && objectList.Count == 0 && File.Exists(initialImagePath))
        {
            using (Image initialImage = Image.FromFile(initialImagePath))
            {
                g.DrawImage(initialImage, new Rectangle(0, 0, width, height));
            }
            return;
        }

        showError = false;
        mismatchedObjectDetected = false;

        if (objectList.Count > 0)
        {
            lock (objectList)
            {
                foreach (TuioObject tobj in objectList.Values)
                {
                    // Check if any incorrect animal is detected based on the background
                    if ((hasForest && (tobj.SymbolID == 5 || tobj.SymbolID == 6 || tobj.SymbolID == 7 || tobj.SymbolID == 9 || tobj.SymbolID == 10 || tobj.SymbolID == 11)) ||
                        (hassea && (tobj.SymbolID == 1 || tobj.SymbolID == 2 || tobj.SymbolID == 3 || tobj.SymbolID == 9 || tobj.SymbolID == 10 || tobj.SymbolID == 11)) ||
                        (hasdesert && (tobj.SymbolID == 1 || tobj.SymbolID == 2 || tobj.SymbolID == 3 || tobj.SymbolID == 5 || tobj.SymbolID == 6 || tobj.SymbolID == 7)))
                    {
                        mismatchedObjectDetected = true;
                        break;
                    }

                    // Background and animal visibility logic
                    switch (tobj.SymbolID)
                    {
                        case 0: hasForest = true; hassea = false; hasdesert = false; showInitialImage = false; break;
                        case 4: hassea = true; hasForest = false; hasdesert = false; showInitialImage = false; break;
                        case 8: hasdesert = true; hasForest = false; hassea = false; showInitialImage = false; break;
                        case 1: showTiger = hasForest; showMenu = hasForest; break;
                        case 2: showLion = hasForest; break;
                        case 3: showFox = hasForest; break;
                        case 5: showfish = hassea; break;
                        case 6: showwhale = hassea; break;
                        case 7: showshark = hassea; break;
                        case 9: showpengiun = hasdesert; break;
                        case 10: showlooney= hasdesert; break;
                        case 11: showpolarbear = hasdesert; break;
                    }
                }
            }
        }

        // Show error if mismatched object is detected
        showError = mismatchedObjectDetected;

        // Draw background and error images based on flags
        if (showError)
        {
            using (Image errorImage = Image.FromFile(errorpath))
            {
                g.DrawImage(errorImage, new Rectangle(0, 0, width, height));
            }
        }
        else
        {
            // Draw appropriate background and animals based on visibility flags
            DrawBackgroundAndAnimals(g, forest, sea, desert);
        }
    }

    // Separate method for drawing background and animals
    private void DrawBackgroundAndAnimals(Graphics g, string forest, string sea, string desert)
    {
        if (hasForest && File.Exists(forest))
            DrawImage(g, forest, width, height);
        else if (hassea && File.Exists(sea))
            DrawImage(g, sea, width, height);
        else if (hasdesert && File.Exists(desert))
            DrawImage(g, desert, width, height);

        // Draw animals only on their respective backgrounds

        if (hasForest && showTiger)
        {
            AnimalName = "Tiger";
            Point centerPosition = new Point(width / 2, height / 2); 
            UpdateTigerVisibility();  // Ensure tiger visibility is up-to-date
            
            if (showMenu && tigerVisible)
            {
                DrawCircularMenu(g, centerPosition, 100, AnimalName);
            }
            DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "tiger2.png"), new Point(width / 4, 600), height / 2);
            //OnHighlightTimerElapsed(null,null);
            //PlayHighlightSound();
            if (showLion)
            {
                showMenu = false;
                DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "lion2.png"), new Point(width / 2, 600), height / 2);
            }
            if (showFox)
            {
                showMenu = false;
                DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "fox2.png"), new Point(800, 600), height / 2);
            }
        }
        if (hasForest && showLion)
        {

            Point centerPosition = new Point(width / 2, height / 2);

            DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "lion2.png"), new Point(width / 2, 600), height / 2);

        }
        if (hasForest && showFox)
        {

            Point centerPosition = new Point(width / 2, height / 2);

            DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "fox2.png"), new Point(800, 600), height / 2);
        }
        else if (hassea)
        {
            if (showfish) DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "fish2.png"), new Point(800, 600), height / 2);
            if (showshark) DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "shark2.png"), new Point(width / 4, 600), height / 2);
            if (showwhale) DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "whale1.png"), new Point(width / 2, 600), height / 2);
        }
        else if (hasdesert)
        {
            if (showpengiun) DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "d22.png"), new Point(width / 4, 600), height / 2);
            if (showlooney) DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "soa1.png"), new Point(width / 2, 600), height / 2);
            if (showpolarbear) DrawAnimal(g, Path.Combine(Environment.CurrentDirectory, "d222.png"), new Point(800, 600), height / 2);
        }
    }

    // Helper method to draw images
    private void DrawImage(Graphics g, string imagePath, int width, int height)
    {
        using (Image image = Image.FromFile(imagePath))
        {
            g.DrawImage(image, new Rectangle(0, 0, width, height));
        }
    }


    private void DrawCircularMenu(Graphics g, Point center, int radius, string AnimalName)
    {
        int angleIncrement = 360 / menuItems.Count;
        int ovalWidth = 100; // Width of the oval
        int ovalHeight = 50; // Height of the oval
        int textOffsetY = -210; // Offset to move the text above the ovals
        if (!showMenu) return;

        for (int i = 0; i < menuItems.Count - 1; i++)
        {
            double angle = (angleIncrement * i) * (Math.PI / 180);
            int x = (int)(center.X + radius * Math.Cos(angle));
            int y = (int)(center.Y + radius * Math.Sin(angle) + textOffsetY); // Offset the text upward

            // Draw the black oval
            using (SolidBrush blackBrush = new SolidBrush(Color.Black))
            {
                if (i == 0)
                    g.FillEllipse(blackBrush, 585, 123, ovalWidth, ovalHeight);
                if (i == 1)
                    g.FillEllipse(blackBrush, 485, 223, ovalWidth, ovalHeight);
                if (i == 2)
                    g.FillEllipse(blackBrush, 395, 123, ovalWidth, ovalHeight);
                if (i == 3)
                    g.FillEllipse(blackBrush, 485, 23, ovalWidth, ovalHeight);
            }

            menuItems[3] = AnimalName;

            // Draw the menu item text above the oval

            SolidBrush brush = (i == currentItemIndex) ? highlightBrush : menuBrush;
            if (i == 0)
                g.DrawString(menuItems[i], font, brush, new PointF(x, y + 5));
            if (i == 1)
                g.DrawString(menuItems[i], font, brush, new PointF(x - 30, y + 10));
            if (i == 2)
                g.DrawString(menuItems[i], font, brush, new PointF(x - 18, y - 50));
            if (i == 3)
                g.DrawString(menuItems[i], font, brush, new PointF(x + 88, y - 30));


        }
    }

    // Helper method to draw animal images
    private void DrawAnimal(Graphics g, string animalImagePath, Point position, int size)
    {
        if (File.Exists(animalImagePath))
        {
            using (Image animalImage = Image.FromFile(animalImagePath))
            {
                // Draw the animal image at the fixed position
                g.DrawImage(animalImage, new Rectangle(position.X - size / 2, position.Y - size, size, size));
            }
        }
    }
    private static void Main(string[] argv)
    {
        int port = 0;
        switch (argv.Length)
        {
            case 1:
                port = int.Parse(argv[0], null);
                if (port == 0) goto default;
                break;
            case 0:
                port = 3333;
                break;
            default:
                Console.WriteLine("usage: mono TuioDemo [port]");
                System.Environment.Exit(0);
                break;
        }

        TuioDemo app = new TuioDemo(port);
        Application.Run(app);
    }
}