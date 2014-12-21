using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Kinect.Toolbox;
using Microsoft.Kinect;

namespace GesturesViewer
{
    partial class MainWindow
    {
        void LoadCircleGestureDetector()
        {
            using (Stream recordStream = File.Open(circleKBPath, FileMode.OpenOrCreate))
            {
                circleGestureRecognizer = new TemplatedGestureDetector("Circle", recordStream);
                circleGestureRecognizer.DisplayCanvas = gesturesCanvas;
                circleGestureRecognizer.OnGestureDetected += OnGestureDetected;

                MouseController.Current.ClickGestureDetector = circleGestureRecognizer;
            }
        }

        void LoadTriGestureDetector()
        {
            using (Stream recordStream = File.Open(triKBPath, FileMode.OpenOrCreate))
            {
                triangleGestureRecognizer = new TemplatedGestureDetector("Triangle", recordStream);
                triangleGestureRecognizer.DisplayCanvas = gesturesCanvas;
                triangleGestureRecognizer.OnGestureDetected += OnGestureDetected;
            }
        }



        private void recordGesture_Click(object sender, RoutedEventArgs e)
        {
            if (circleGestureRecognizer.IsRecordingPath)
            {
                circleGestureRecognizer.EndRecordTemplate();
                recordGesture.Content = "Record Gesture";
                return;
            }

            circleGestureRecognizer.StartRecordTemplate();
            recordGesture.Content = "Stop Recording";

            if (triangleGestureRecognizer.IsRecordingPath)
            {
                triangleGestureRecognizer.EndRecordTemplate();
                recordGesture.Content = "Record Gesture";
                return;
            }

            triangleGestureRecognizer.StartRecordTemplate();
            recordGesture.Content = "Stop Recording";
        }

        void OnGestureDetected(string gesture)
        {
            //int pos = detectedGestures.Items.Add(string.Format("{0} : {1}", gesture, DateTime.Now));

            //cir_bit &= 0x00;    //Circle_bit Initialize
            
            int pos = detectedGestures.Items.Add(gesture);
            detectedGestures.SelectedIndex = pos;

            if (!(detectedGestures.Items.IsEmpty))
            {
                detectedGestures.ScrollIntoView(detectedGestures.Items[detectedGestures.Items.Count - 1]);
                // Last Message_syncronized
                switch (swipeGestureRecognizer.icon_bit)
                {
                    case 2:
                        if (detectedGestures.Items[detectedGestures.Items.Count - 1].Equals("Circle"))
                        {
                            swipeGestureRecognizer.cir_bit = 0x01;
                            if (detectedGestures.Items[detectedGestures.Items.Count - 1].Equals("Triangle"))
                            {
                                swipeGestureRecognizer.cir_bit = 0x09;
                            }
                        }
                        break;
                    case 0:
                    case 1:
                    case 3:
                    case 4:
                    case 5:
                        if (detectedGestures.Items[detectedGestures.Items.Count - 1].Equals("Circle"))
                        {
                            swipeGestureRecognizer.cir_bit = 0x01;

                            if (detectedGestures.Items[detectedGestures.Items.Count - 1].Equals("Circle"))
                            {
                                switch (swipeGestureRecognizer.hand_sig)
                                {
                                    /*                    V P I     I: icon_bit     P: pic_bit      V: video_bit
                                     *      0 0 0 0     0 0 0 0
                                     */
                                    case 11:
                                    case 22:
                                        swipeGestureRecognizer.cir_bit = 0x03;
                                    break;
                                }

                            }
                           
                        }
                    break;
                // Last_index Message compare icon_bit binding cir_bit;
                }
            }
            
        }

        void CloseGestureDetector()
        {
            if (circleGestureRecognizer == null || triangleGestureRecognizer == null )
                return;

            using (Stream recordStream = File.Create(circleKBPath))
            {
                circleGestureRecognizer.SaveState(recordStream);
            }
            circleGestureRecognizer.OnGestureDetected -= OnGestureDetected;

            using (Stream recordStream = File.Create(triKBPath))
            {
                triangleGestureRecognizer.SaveState(recordStream);
            }
            triangleGestureRecognizer.OnGestureDetected -= OnGestureDetected;

        }
    }
}
