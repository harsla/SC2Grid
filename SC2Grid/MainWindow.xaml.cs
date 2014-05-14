using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Drawing;

/*
 * Things to do:
 * - add events to queue in order to handle multiple events
 * - turn on/off alerts
 * - apm should be cumilitive/avrage (currentAPM+=; numberOfApmReads++)
 * - bug: inject timer fires multiple times
 * - event sounds for inject, build scv, mule, chrono 
 * - break program into classes
 * - save sr settings to registry
 * - normilize sr data (1000 difference, skip)
 * - debug mode toggle in ui
 * - screenReader SC2 dictonary should read alpha charicters
 * - implimnet game state by screen reader reading null, and or apm stop
 * - automatically find min, gas, supply 
 * - wpf toolkit
 *             
 *        Right monitor 1080p
 *        sr.SetWhitelist("1234567890/");
 *        Mineral.Content = sr.ReadScreen(3440, 16, 94, 20);
 *        Gas.Content = sr.ReadScreen(3564, 16, 92, 20);
 *        Supply.Content = sr.ReadScreen(3686, 46, 117, 20);
 *          
 */
namespace SC2Grid
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //DEBUG: DEMO MODE TO TRUE TO RAND INSTEAD FOR SCREEN GRAB
        bool demoMode = false;

        //Genorate some random seeds for demo mode
        Random miniralRandom = new Random(1);
        Random gasRandom = new Random(2);

        //constuc
        box currentlySelectedBox;
        
        // mineral cords
        box mineralBox = new box(1486, 17, 92, 20);

        //gas cords
        box gasBox = new box(1607, 17, 92, 20);

        //supply cords
        box supplyBox = new box(1730, 17, 117, 20);

        //inject message cord
        box injectBox = new box(50, 250, 220, 300);
        
        
        //load user32 to get current window
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();
        
        //load user32 to invalidate current window
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        ScreenReader sr = new ScreenReader();
        decisionMaker decideMinerals = new decisionMaker(); 
        decisionMaker decideGas = new decisionMaker();
        decisionMaker decideSupply = new decisionMaker();
        decisionMaker decideAPM = new decisionMaker();
        decisionMaker decideInject = new decisionMaker();
        decisionMaker decideBuildWorker = new decisionMaker();

        bool scrapeFlag = false;
        bool boxFlag = false;
        
        //maybe change to threading timer. 
        DispatcherTimer boxTimer = new DispatcherTimer();
        DispatcherTimer scrapeTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);

            InitializeComponent();
            scrapeTimer.Tick += new EventHandler(scrape_timer);
            scrapeTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);

            boxTimer.Tick += new EventHandler(box_timer);
            boxTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);

            //set the current box
            currentlySelectedBox = mineralBox;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S)
                decideBuildWorker.ResetWorkerTimer();
                
        }

        // Check, start, and stop box timer
        private void boxToggle()
        {
            if (boxFlag == false)
            {
                boxTimer.Start();
                boxFlag = true;
                save_button.Content = "Save";
            }
            else 
            {
                boxTimer.Stop();
                boxFlag = false;
                save_button.Content = "Config";
            }
        }

        // Check, start, and stop scrape timer
        private void scrapeToggle()
        {
            if (scrapeFlag == false)
            {
                scrapeTimer.Start();
                scrapeFlag = true;
                start_button.Content = "Stop";
                
            }
            else
            {
                scrapeTimer.Stop();
                scrapeFlag = false;
                start_button.Content = "Start";
            }
        }

        // Fire the scrape function and log any errors
        private void scrape_timer(object sender, EventArgs e) 
        {
            
            try
            {
                scrape();
            }
            catch (Exception error)
            {
                //return;
                //MessageBox.Show(Convert.ToString(error));
                using (StreamWriter log = new System.IO.StreamWriter("sc2grid.log"))
                {
                    //log.Write(error);
                    log.WriteLine(error);
                }
            
            }

        }

        // Display capture boxes per time interval
        private void box_timer(object sender, EventArgs e) 
        {
            drawInputBoxes();
        }

        // Do the Scrape
        private void scrape()
        {
            checkMinerals();
            checkGas();
            checkSupply();
            checkAPM();
            checkInject();
            checkBuildWorker();
        }

        // Button to start and stop the timer
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            scrapeToggle();
            //captureSS();
        }

        // Display the input boxes
        private void drawInputBoxes()
        {
            
            //get current window
            System.Drawing.Graphics Draw = System.Drawing.Graphics.FromHwnd(GetDesktopWindow());

            // Create pen
            System.Drawing.Pen drawMineral = new System.Drawing.Pen(System.Drawing.Color.Aqua, 1);
            System.Drawing.Pen drawGas = new System.Drawing.Pen(System.Drawing.Color.LightGreen, 1);
            System.Drawing.Pen drawSupply = new System.Drawing.Pen(System.Drawing.Color.White, 1);
            System.Drawing.Pen drawInject = new System.Drawing.Pen(System.Drawing.Color.Yellow, 1);


            //draw to screen
            Draw.DrawRectangle(drawMineral, mineralBox.draw_X, mineralBox.draw_Y, mineralBox.draw_W, mineralBox.draw_H);
            Draw.DrawRectangle(drawGas, gasBox.draw_X, gasBox.draw_Y, gasBox.draw_W, gasBox.draw_H);
            Draw.DrawRectangle(drawSupply, supplyBox.draw_X, supplyBox.draw_Y, supplyBox.draw_W, supplyBox.draw_H);
            Draw.DrawRectangle(drawInject, injectBox.draw_X, injectBox.draw_Y, injectBox.draw_W, injectBox.draw_H);

        }

        // Control to move the box up
        private void boxUp(object sender, RoutedEventArgs e) 
        {
            InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
            currentlySelectedBox.draw_Y = currentlySelectedBox.draw_Y - 2;
        }

        // Control to move the box down 
        private void boxDown(object sender, RoutedEventArgs e)
        {
            InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
            currentlySelectedBox.draw_Y = currentlySelectedBox.draw_Y + 2;
        }

        // Control to move the box left
        private void boxLeft(object sender, RoutedEventArgs e)
        {
            InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
            currentlySelectedBox.draw_X = currentlySelectedBox.draw_X - 2;
        }

        // Control to move the box Right
        private void boxRight(object sender, RoutedEventArgs e)
        {
            InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
            currentlySelectedBox.draw_X = currentlySelectedBox.draw_X + 2;
            drawInputBoxes();
        }

        // Save the input box location
        private void boxSave(object sender, RoutedEventArgs e) 
        {
            boxToggle();
        }

        //set currently selected box to minerals
        private void Minerals_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            currentlySelectedBox = mineralBox;
        }
        
        //set currently selected box to gas
        private void Gas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            currentlySelectedBox = gasBox;
        }
        
        //set currently selected box to supply
        private void Supply_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            currentlySelectedBox = supplyBox;
        }

        void checkMinerals(){
            int data;
            string strData;

            if (demoMode)
            {
                data = miniralRandom.Next(100, 1100);
                Mineral.Content = data;
            }
            else
            {
                sr.SetWhitelist("1234567890");
                strData = sr.ReadScreen(mineralBox.draw_X, mineralBox.draw_Y, mineralBox.draw_W, mineralBox.draw_H).Trim();
                if ((strData != null) && (strData != ""))
                    data = Convert.ToInt32(strData);
                else
                    data = 0;
                //Mineral.Content = data;
                //decideMinerals.checkMinerals(data);
                Mineral.Content = decideMinerals.checkMinerals(data).ToString();
            }
        }

        void checkGas() 
        {
            int data;
            string strData;

            if (demoMode)
            {
                data = gasRandom.Next(100, 1100);
                Gas.Content = data;
            }
            else
            {
                sr.SetWhitelist("1234567890");
                strData = sr.ReadScreen(gasBox.draw_X, gasBox.draw_Y, gasBox.draw_W, gasBox.draw_H).Trim();
                if (strData != "")
                    data = Convert.ToInt32(strData);
                else
                    data = 0;
                Gas.Content = data;
                decideGas.checkGas(data);
            }
        }

        void checkSupply()
        {
            string strData;
                sr.SetWhitelist("1234567890/");
                strData = sr.ReadScreen(supplyBox.draw_X, supplyBox.draw_Y, supplyBox.draw_W, supplyBox.draw_H).Trim();
                if (strData != "")
                {
                    Supply.Content = strData;
                    decideSupply.checkSupply(strData);
                }
                else
                    strData = "0";
            }
        
         void checkInject()
        {
            string data;

                sr.SetWhitelist("1234567890/");
                data = sr.ReadScreen(injectBox.draw_X, injectBox.draw_Y, injectBox.draw_W, injectBox.draw_H).Trim();
                if (data != "")
                    decideInject.checkInject(data);
         }

        void checkAPM() 
        {
            APM.Content = decideAPM.checkAPM().ToString();
        }

        void checkBuildWorker()
        {
            //decideBuildWorker.checkWorker();
        }
        
        //REMOVE THIS:: Temp fucntion to capture tessract train data 
        int imageCount = 1;
        void captureSS() 
        {
            string[] files = Directory.GetFiles("c:\\debug\\");
            ScreenReader CaptureScreenReader = new ScreenReader();
            CaptureScreenReader.SetLanguage("eng");
            CaptureScreenReader.SetWhitelist("StructeomplLarva");
            ////CaptureScreenReader.SetPSM(OCR.TesseractWrapper.ePageSegMode.PSM_SINGLE_WORD);

            foreach (string data in files)
            {
                System.Drawing.Bitmap image = new System.Drawing.Bitmap(data);
                MessageBox.Show(imageCount + "\n\n" + CaptureScreenReader.ReadScreen(image));
                imageCount++;
            }

        
        }
        
            /* // Bad-ass weese function to export all results
             * 
             private void Export_Click(object sender, RoutedEventArgs e)
            {
                ResultList list = OCRResults.DataContext as ResultList;

                int images = list.Count;
                int unitWidth = (int)list[0].imageData.Width;
                int unitHeight = (int)list[0].imageData.Height;

                int xcount = 9;
                int ycount = images / xcount;

                int width = xcount * unitWidth;
                int height = ycount * unitHeight;

                Bitmap image = new Bitmap(width, height, list[0].imageData.PixelFormat);

                using (Graphics graphics = Graphics.FromImage(image))
                {
                    for (int x = 0; x < xcount; x++)
                    {
                        for (int y = 0; y < ycount; y++)
                        {
                            int index = x + (y * xcount);
                            graphics.DrawImage(list[index].imageData, (float)(x * unitWidth), (float)(y * unitHeight));
                        }
                    }
                }

                image.Save("exported-images.tif", ImageFormat.Tiff);
                image.Dispose();
            }
            */
    }
}
