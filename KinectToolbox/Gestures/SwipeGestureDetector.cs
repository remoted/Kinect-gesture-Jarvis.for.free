using System;
using System.Windows;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;

namespace Kinect.Toolbox
{

    public class SwipeGestureDetector : GestureDetector
    {

        // media
        private const int WM_APPCOMMAND = 0x0319;


        public const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;
        public const int APPCOMMAND_MEDIA_PLAY = 46;
        public const int APPCOMMAND_MEDIA_PAUSE = 47;
        public const int APPCOMMAND_MEDIA_RECORD = 48;
        public const int APPCOMMAND_MEDIA_FAST_FORWARD = 49;
        public const int APPCOMMAND_MEDIA_REWIND = 50;
        public const int APPCOMMAND_MEDIA_CHANNEL_UP = 51;
        public const int APPCOMMAND_MEDIA_CHANNEL_DOWN = 52;
        int pCommand = APPCOMMAND_MEDIA_PLAY_PAUSE;



        public float SwipeMinimalLength {get;set;}
        public float SwipeMaximalHeight {get;set;}
        public int SwipeMininalDuration {get;set;}
        public int SwipeMaximalDuration {get;set;}
        public System.Int32 iHandle; //media 
        //[DllImport("user32.dll")]
        //public static extern int FindWindow(string lpClassnName, string lpWindowName);





        public SwipeGestureDetector(int windowSize = 20)
            : base(windowSize)
        {
            SwipeMinimalLength = 0.4f;
            SwipeMaximalHeight = 0.2f;
            SwipeMininalDuration = 250;
            SwipeMaximalDuration = 1500;
        }

        protected bool ScanPositions(Func<Vector3, Vector3, bool> heightFunction, Func<Vector3, Vector3, bool> directionFunction, 
            Func<Vector3, Vector3, bool> lengthFunction, int minTime, int maxTime)
        {
            int start = 0;

            for (int index = 1; index < Entries.Count - 1; index++)
            {
                if (!heightFunction(Entries[0].Position, Entries[index].Position) || !directionFunction(Entries[index].Position, Entries[index + 1].Position))
                {
                    start = index;
                }

                if (lengthFunction(Entries[index].Position, Entries[start].Position))
                {
                    double totalMilliseconds = (Entries[index].Time - Entries[start].Time).TotalMilliseconds;
                    if (totalMilliseconds >= minTime && totalMilliseconds <= maxTime)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int icon_bit = 0;
        public int pic_bit = 0;
        public int video_bit = 0;
        public int sw_bit = 0x00;
        public int cir_bit = 0x00;
        public int hand_sig = 0;
        public int ppt_bit = 0x00;
        protected override void LookForGesture()
        {
            sw_bit &= 0x00;     //Initialize Bit

            // Swipe to right
            if (ScanPositions((p1, p2) => Math.Abs(p2.Y - p1.Y) < SwipeMaximalHeight, // Height
                    (p1, p2) => p2.X - p1.X > -0.01f, // Progression to right
                    (p1, p2) => Math.Abs(p2.X - p1.X) > SwipeMinimalLength, // Length
                    SwipeMininalDuration, SwipeMaximalDuration)) // Duration
            {
                RaiseGestureDetected("SwipeToRight");
               
                if (icon_bit != 0)
                {
                    if (cir_bit != 0x01){
                        icon_bit--;
                    }
 
                    if (cir_bit == 0x01 || cir_bit == 0x09){
                        switch (icon_bit)
                        {
                            //추가
                            case 2:
                                    System.Windows.Forms.SendKeys.SendWait("{LEFT}");
                                          break;                     
                            case 3:        
                            case 5:
                                //======================================================================//
                                iHandle = Win32.FindWindow("WMPlayerApp", "Windows Media Player"); //미디어 있으면 적용됨 --추가-- 11/10
                                Win32.SendMessage(iHandle, Win32.WM_COMMAND, 0x0000497a, 0x00000000); //이전곡 시작함     --추가--
                                //======================================================================//
                                break;
                            case 4:
                                pic_bit--;
                                hand_sig = 11; // RIght_Hand Signal gogo
                                break;

                        }
                    }

                }
                else
                    icon_bit = 0;
                return;
            }

                // Swipe to left
            if (ScanPositions((p1, p2) => Math.Abs(p2.Y - p1.Y) < SwipeMaximalHeight,  // Height
                    (p1, p2) => p2.X - p1.X < 0.01f, // Progression to right
                    (p1, p2) => Math.Abs(p2.X - p1.X) > SwipeMinimalLength, // Length
                    SwipeMininalDuration, SwipeMaximalDuration))// Duration
            {
                RaiseGestureDetected("SwipeToLeft");
                if (icon_bit != 5)
                {
                    if (cir_bit != 0x01) { 
                        icon_bit++; 
                    }
                    
                    if (cir_bit == 0x01 || cir_bit == 0x09){
                        switch (icon_bit)
                        {
                            case 2:
                                System.Windows.Forms.SendKeys.SendWait("{RIGHT}");
                                break;
                            case 3:
                            case 5:
                                //======================================================================//
                                iHandle = Win32.FindWindow("WMPlayerApp", "Windows Media Player"); //미디어 있으면 적용된 --추가-- 11/10
                                Win32.SendMessage(iHandle, Win32.WM_COMMAND, 0x0000497b, 0x00000000); //다음곡 시작함     --추가--
                                //======================================================================//
                                break;
                            case 4:
                                pic_bit++;
                                hand_sig = 22; // Left_Hand Signal
                                break;
                            
                        }
                        
                    }

                    /*                  pic_bit     Signal     
                     *  The Hand_Left   ++          22       
                     *  The hand_Right  --          11          
                     */
                }
                else
                    icon_bit = 5;
                return;
            }
               

            // Swipe to up
            if (ScanPositions((p1, p2) => Math.Abs(p2.X - p1.X) < SwipeMaximalHeight,
              (p1, p2) => p2.Y - p1.Y > -0.01f, (p1, p2) =>
              Math.Abs(p2.Y - p1.Y) > SwipeMinimalLength, SwipeMininalDuration, SwipeMaximalDuration))
            {
                
                /*                                                  
                 *          icon bit    0   0   0   0   0   0   0   0
                 *                              6th 5th 4th 3rd 2nd 1st
                */
                RaiseGestureDetected("SwipeToUp");
                
                switch (icon_bit) // 핵심부
                {
                    case 0:
                        sw_bit = 0x01;
                        break;
                    case 1:
                        sw_bit = 0x02;
                        break;
                    case 2:
                        sw_bit = 0x04;
                        break;
                    case 3:
                        sw_bit = 0x08;
                        break;
                    case 4:
                        sw_bit = 0x10;
                        break;
                    case 5:
                        sw_bit = 0x20;
                        break;
                    case 6:
                        break;
                    //Extend's
                }
                return;
            }

            // Swipe to down
            if (ScanPositions((p1, p2) => Math.Abs(p2.X - p1.X) < SwipeMaximalHeight,
              (p1, p2) => p2.Y - p1.Y < 0.01f, (p1, p2) =>
              Math.Abs(p2.Y - p1.Y) > SwipeMinimalLength, SwipeMininalDuration, SwipeMaximalDuration))
            {
                RaiseGestureDetected("Swipedown");  

                if (cir_bit == 0x01)
                {
                    switch (icon_bit)
                    {
                        case 2:
                            System.Windows.Forms.SendKeys.SendWait("{F5}"); //
                            break;
                        case 3:
                        case 5:
                            //======================================================================//
                            iHandle = Win32.FindWindow("WMPlayerApp", "Windows Media Player");     //미디어 있으면 적용된 --추가-- 11/10
                            Win32.SendMessage(iHandle, Win32.WM_COMMAND, 0x00004978, 0x00000000);  // play 만 누르면 stop/play가 가능하다. --추가--
                            //======================================================================//
      
                            break;
                    }
                }
                return;
            }
        }
    }
}
