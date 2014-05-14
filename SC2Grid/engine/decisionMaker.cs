using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SC2Grid
{

    public class decisionMaker
    {
        //TODO: Turn decisionMaker into a specification that reads external source.

        //constructor
        public decisionMaker()
        {
            mineralTimer.Tick += new EventHandler(mineral_cooldown);
            gasTimer.Tick += new EventHandler(gas_cooldown);
            supplyTimer.Tick += new EventHandler(supply_cooldown);
            apmTimer.Tick += new EventHandler(apm_cooldown);
            injectTimer.Tick += new EventHandler(inject_cooldown);
            workerTimer.Tick += new EventHandler(worker_cooldown);
            workerKeyTimer.Tick += new EventHandler(workerKeyTimer_cooldown);
        }

        //mineral properties
        int[] minerals = new int[3];
        bool mineralCooldown = false;
        System.Media.SoundPlayer mineralHigh = new System.Media.SoundPlayer(@"C:\Users\Scott\Documents\Visual Studio 2012\Projects\SC2Grid\SC2Grid\events\minerals.wav");
        DispatcherTimer mineralTimer = new DispatcherTimer();

        //gas properties
        int[] gas = new int[3];
        bool gasCooldown = false;
        System.Media.SoundPlayer gasHigh = new System.Media.SoundPlayer(@"C:\Users\Scott\Documents\Visual Studio 2012\Projects\SC2Grid\SC2Grid\events\gas.wav");
        DispatcherTimer gasTimer = new DispatcherTimer();

        //supply properties
        int[,] supply = new int[3,2];
        bool supplyCooldown = false;
        System.Media.SoundPlayer supplyHigh = new System.Media.SoundPlayer(@"C:\Users\Scott\Documents\Visual Studio 2012\Projects\SC2Grid\SC2Grid\events\supply.wav");
        DispatcherTimer supplyTimer = new DispatcherTimer();

        
        //inject properties
        //string[] inject;
        bool injectCooldown = false;
        System.Media.SoundPlayer injectLarva = new System.Media.SoundPlayer(@"C:\Users\Scott\Documents\Visual Studio 2012\Projects\SC2Grid\SC2Grid\events\single.wav");
        DispatcherTimer injectTimer = new DispatcherTimer();
        

        //apm properties
        System.Media.SoundPlayer apmLow = new System.Media.SoundPlayer(@"C:\Users\Scott\Documents\Visual Studio 2012\Projects\SC2Grid\SC2Grid\events\apm.wav");
        bool apmCooldown = false;
        DispatcherTimer apmTimer = new DispatcherTimer();
        
        //worker properties
        System.Media.SoundPlayer buildWorker = new System.Media.SoundPlayer(@"C:\Users\Scott\Documents\Visual Studio 2012\Projects\SC2Grid\SC2Grid\events\worker.wav");
        bool workerCooldown = false;
        bool workerKeyPressed = false;
        DispatcherTimer workerTimer = new DispatcherTimer();
        DispatcherTimer workerKeyTimer = new DispatcherTimer();

        public void ResetWorkerTimer()
        {
            workerKeyPressed = true;
           // workerCooldown = true;
            workerKeyTimer.Interval = new TimeSpan(0, 0, 14); //17 seconds to build a worker
            workerKeyTimer.Start();
        }

        public int checkMinerals(int data)
        {   
            //normalize data
            minerals[2] = minerals[1];
            minerals[1] = minerals[0];
            minerals[0] = data;
                if ((minerals[0] - minerals[1]) >= 1000)
                    return minerals[1];
                //check cooldown
                else if(!mineralCooldown)
                    //check condition
                    if (minerals[0] >= 1000)
                    {
                        //fire event, trip cooldown, return data
                        mineralHigh.PlaySync();
                        mineralCooldown = true;
                        mineralTimer.Interval = new TimeSpan(0, 0, 0, 5);
                        mineralTimer.Start();
                        return minerals[0];
                    }
                return minerals[1];

        }

        public int checkGas(int data)
        {
            //normalize data
            gas[2] = gas[1];
            gas[1] = gas[0];
            gas[0] = data;
            if ((gas[0] - gas[1]) >= 1000)
                return gas[1];
            //check cooldown
            else if (!gasCooldown)
                //check condition
                if (gas[0] >= 1000)
                {
                    //fire event, trip cooldown, return data
                    gasHigh.PlaySync();
                    gasCooldown = true;
                    gasTimer.Interval = new TimeSpan(0, 0, 0, 5);
                    gasTimer.Start();
                    return gas[0];
                }
            return gas[0];

        }

        public string checkSupply(string data)
        {
            //look for good read
            if (data.Contains("/"))
            {
                //3rd set of data
                supply[2, 0] = supply[1, 0];
                supply[2, 1] = supply[1, 1];

                //previous data
                supply[1, 0] = supply[0, 0];
                supply[1, 1] = supply[0, 1];

                //new data in [0,0] and [0,1]
                string[] splitData = data.Trim().Split('/');
                if (splitData[0] != "")
                supply[0, 0] = Convert.ToInt32(splitData[0]);
                if (splitData[1] != "")
                supply[0, 1] = Convert.ToInt32(splitData[1]);

                //Normalize Data (if supply goes up by more 200, or below zero ignore)
                //if (true)
                if (((supply[0, 1]) - (supply[0, 0]) > 200) || ((supply[0, 1]) - (supply[0, 0]) < 0))
                {
                    return supply[1,1] + "/" + supply[0,1];
                }


                //check cooldown
                if (!supplyCooldown)
                    // If within 3 units of max supply, alert
                    if (((supply[0, 0] + 3) >= supply[0, 1]) && supply[0, 1] != 200 && supply[0, 1] != 10 && supply[0, 1] != 11)
                    {
                        //fire event, and trip cooldown
                        supplyHigh.PlaySync();
                        supplyCooldown = true;
                        supplyTimer.Interval = new TimeSpan(0, 0, 0, 5);
                        supplyTimer.Start();
                        return supply[0, 1] + "/" + supply[0, 0];
                    }
                    else return supply[0, 1] + "/" + supply[0, 0];
            }
            //don't return anything if the data is bad  
            return "";
        }

        public double checkAPM() 
        {
            double average;
            
            //check for registry key, read value
            Microsoft.Win32.RegistryKey sc2Key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Razer\\Starcraft2", true);
            if (sc2Key == null)
                return 0;
            else
            {
                average = Convert.ToDouble(sc2Key.GetValue("APMValue", "1"));
            }

            //check cooldown
            if(!apmCooldown)
                //if the avrage value is below 20, play an alert
                if (average < 20)
                {
                    //apmLow.PlaySync(); /*TODO: RECORD AN APM LOW WAV*/
                    apmCooldown = true;
                    apmTimer.Interval = new TimeSpan(0, 0, 0, 5);
                    apmTimer.Start();
                }
                return average;
        }

        public void checkInject(string data)
        {
            if (!injectCooldown)
                //check condition
                if (data.Contains("300"))
                {
                    //fire event, trip cooldown, return data
                    injectLarva.PlaySync();
                    injectCooldown = true;
                    injectTimer.Interval = new TimeSpan(0, 0, 0, 1);
                    injectTimer.Start();
                }
        }

        public void checkWorker()
        {
            if (workerKeyPressed)
                return;
            if (!workerCooldown)
            {
                buildWorker.PlaySync();
                workerCooldown = true;
                workerTimer.Interval = new TimeSpan(0, 0, 14); //17 seconds to build a worker
                workerTimer.Start();
            }
                
        }

        //reset mineral cooldown flag, and stop timer.
        private void mineral_cooldown(object sender, EventArgs e)
        {
            mineralCooldown = false;
            mineralTimer.Stop();
        }

        //reset gas cooldown flag, and stop timer.
        private void gas_cooldown(object sender, EventArgs e)
        {
            gasCooldown = false;
            gasTimer.Stop();
        }

        //reset supply cooldown flag, and stop timer.
        private void supply_cooldown(object sender, EventArgs e)
        {
            supplyCooldown = false;
            supplyTimer.Stop();   
        }

        //reset apm cooldown flag, and stop timer.
        private void apm_cooldown(object sender, EventArgs e)
        {
            apmCooldown = false;
            apmTimer.Stop();
        }

        //reset inject cooldown flag, and stop timer.
        private void inject_cooldown(object sender, EventArgs e)
        {
            injectCooldown = false;
            injectTimer.Stop();
        }

        //reset worker cooldown flag, and stop timer.
        private void worker_cooldown(object sender, EventArgs e)
        {
            workerCooldown = false;
            workerTimer.Stop();
        }

        private void workerKeyTimer_cooldown(object sender, EventArgs e)
        {
            workerKeyPressed = false;
            workerKeyTimer.Stop();
        }
    }
}
