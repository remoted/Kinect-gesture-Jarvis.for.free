using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.Diagnostics;
using System.IO;
//using System.Windows.Forms;
using Microsoft.Win32;
using Kinect.Toolbox;
using Kinect.Toolbox.Record;
//IO.Path 와의 모호한 관계가 되니, combine 앞쪽에 따로 정의
using Microsoft.Kinect;
using Kinect.Toolbox.Voice;
using Microsoft.Kinect.Toolkit;

namespace GesturesViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
 
        int i = 1;
        

        public System.Int32 iHandle; //media 
        KinectSensor kinectSensor;
        SwipeGestureDetector swipeGestureRecognizer;
        TemplatedGestureDetector circleGestureRecognizer;
        TemplatedGestureDetector triangleGestureRecognizer;
        //Add Traingle

        readonly ColorStreamManager colorManager = new ColorStreamManager();
        readonly DepthStreamManager depthManager = new DepthStreamManager();
        AudioStreamManager audioManager;
        SkeletonDisplayManager skeletonDisplayManager;
        readonly ContextTracker contextTracker = new ContextTracker();
        EyeTracker eyeTracker;
        ParallelCombinedGestureDetector parallelCombinedGestureDetector;
        readonly AlgorithmicPostureDetector algorithmicPostureRecognizer = new AlgorithmicPostureDetector();
        TemplatedPostureDetector templatePostureDetector;
        private bool recordNextFrameForPosture;
        bool displayDepth = false;

        string circleKBPath;
        string letterT_KBPath;
        string triKBPath;
        //Add Triangle

         

        KinectRecorder recorder;
        KinectReplay replay;

        BindableNUICamera nuiCamera;

        private Skeleton[] skeletons;

        VoiceCommander voiceCommander;

        public MainWindow()
        {
            InitializeComponent();
        }

        void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (kinectSensor == null)
                    {
                        kinectSensor = e.Sensor;
                        Initialize();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect was disconnected");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect is no more powered");
                    }
                    break;
                default:
                    MessageBox.Show("Unhandled Status: " + e.Status);
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            circleKBPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"data\circleKB.save");
            letterT_KBPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"data\t_KB.save");
            triKBPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"data\triKB4.save");


            try
            {
                //listen to any status change for Kinects
                KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

                //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
                foreach (KinectSensor kinect in KinectSensor.KinectSensors)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        kinectSensor = kinect;
                        break;
                    }
                }

                if (KinectSensor.KinectSensors.Count == 0)
                    MessageBox.Show("No Kinect found");
                else
                    Initialize();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Initialize()
        {
            if (kinectSensor == null)
                return;

            audioManager = new AudioStreamManager(kinectSensor.AudioSource);
            audioBeamAngle.DataContext = audioManager;

            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.ColorFrameReady += kinectRuntime_ColorFrameReady;

            kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;

            kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters
                                                   {
                                                       Smoothing = 0.5f,
                                                       Correction = 0.5f,
                                                       Prediction = 0.5f,
                                                       JitterRadius = 0.05f,
                                                       MaxDeviationRadius = 0.04f
                                                   });
            kinectSensor.SkeletonFrameReady += kinectRuntime_SkeletonFrameReady;

            swipeGestureRecognizer = new SwipeGestureDetector();
            swipeGestureRecognizer.OnGestureDetected += OnGestureDetected;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);

            kinectSensor.Start();

            LoadCircleGestureDetector();
            LoadLetterTPostureDetector();
            LoadTriGestureDetector();
            //Add Triangle

            nuiCamera = new BindableNUICamera(kinectSensor);

            elevationSlider.DataContext = nuiCamera;

            voiceCommander = new VoiceCommander("record", "stop");
            voiceCommander.OrderDetected += voiceCommander_OrderDetected;

            StartVoiceCommander();

            kinectDisplay.DataContext = colorManager;

            parallelCombinedGestureDetector = new ParallelCombinedGestureDetector();
            parallelCombinedGestureDetector.OnGestureDetected += OnGestureDetected;
            parallelCombinedGestureDetector.Add(swipeGestureRecognizer);
            parallelCombinedGestureDetector.Add(circleGestureRecognizer);
            parallelCombinedGestureDetector.Add(triangleGestureRecognizer);

            //Add Triangle
            //Media_Loaded();
            //btnVideo_Click();

        }

        void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (replay != null && !replay.IsFinished)
                return;

            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;

                if (recorder != null && ((recorder.Options & KinectRecordOptions.Depth) != 0))
                {
                    recorder.Record(frame);
                }

                if (!displayDepth)
                    return;

                depthManager.Update(frame);
            }
        }

        void kinectRuntime_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (replay != null && !replay.IsFinished)
                return;

            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                if (recorder != null && ((recorder.Options & KinectRecordOptions.Color) != 0))
                {
                    recorder.Record(frame);
                }

                if (displayDepth)
                    return;

                colorManager.Update(frame);
            }
        }

        void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (replay != null && !replay.IsFinished)
                return;

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                if (recorder != null && ((recorder.Options & KinectRecordOptions.Skeletons) != 0))
                    recorder.Record(frame);

                frame.GetSkeletons(ref skeletons);

                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                    return;

                ProcessFrame(frame);
            }
        }

        
        
        void ProcessFrame(ReplaySkeletonFrame frame)
        {
            Dictionary<int, string> stabilities = new Dictionary<int, string>();
            
            foreach (var skeleton in frame.Skeletons)
            {
                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;

                contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
                stabilities.Add(skeleton.TrackingId, contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Stable" : "Non stable");
                if (!contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId))
                    continue;

                foreach (Joint joint in skeleton.Joints) //오른손기능구현부
                {
                    if (joint.TrackingState != JointTrackingState.Tracked)
                        continue;

                    if (joint.JointType == JointType.HandRight)
                    {
                        circleGestureRecognizer.Add(joint.Position, kinectSensor);
                        triangleGestureRecognizer.Add(joint.Position, kinectSensor);
                        //Add Triangle
                        switch (swipeGestureRecognizer.icon_bit)
                        {
                            case 0:
                                if (swipeGestureRecognizer.cir_bit == 0x01) //브라우져 영역
                                {
                                    controlMouse.IsChecked = true;
                                    Browser_grid.Visibility = Visibility.Visible;
                                }
                                break;
                            case 1:
                                if (swipeGestureRecognizer.cir_bit == 0x01) //빙맵 영역
                                {
                                    BingMap_Loaded();
                                }
                                break;
                            case 2:
                                if (swipeGestureRecognizer.cir_bit == 0x01) 
                                {
                                    gdDoc.Visibility = Visibility.Visible;
                                    btnDocument_Click();
                                    PPT_Loaded(); //추가
                                   
                                }
                                break;
                            case 3:
                                if (swipeGestureRecognizer.cir_bit == 0x01) //미디어 영역
                                {
                                    Media_Loaded();
                                }
                                break;
                            case 4:
                                if (swipeGestureRecognizer.cir_bit == 0x01)
                                {
                                    gdPicture.Visibility = Visibility.Visible;

                                    if (swipeGestureRecognizer.cir_bit == 0x03)
                                    {  //처음 서클비트가 1이들어가 그냥출력하게 해봄
                                        swipeGestureRecognizer.pic_bit = 0;
                                        gdPicture.Visibility = Visibility.Hidden;
                                        gdPicture.Visibility = Visibility.Visible;
                                        btnPicture_Click();
                                    }
                                    else
                                        btnPicture_Click();
                                }
                                break;
                            case 5:
                                if (swipeGestureRecognizer.cir_bit == 0x01)
                                {
                                    gdVideo.Visibility = Visibility.Visible;
                                    btnVideo_Click();
                                }
                                break;
                            default:

                                break;
                        }

                    }
                    else if (joint.JointType == JointType.HandLeft) //왼손 구현부
                    {
                        swipeGestureRecognizer.Add(joint.Position, kinectSensor);
                        if (controlMouse.IsChecked == true)
                            MouseController.Current.SetHandPosition(kinectSensor, joint, skeleton);
                       
                        //sw1 = pic_bit;


                        switch (swipeGestureRecognizer.sw_bit) //프로세스 영역 죽이는 구현부
                        {
                            case 0x01:
                                controlMouse.IsChecked = false;
                                Browser_grid.Visibility = Visibility.Hidden; //인터넷창 없앰

                                foreach (Process p in Process.GetProcessesByName("iexplore"))
                                {
                                    p.Kill();
                                }
                                foreach (Process p in Process.GetProcessesByName("wmplayer"))  //추가
                                {
                                    p.Kill();
                                }

                                swipeGestureRecognizer.cir_bit = 0x00; // Swipe Up 에 의한 Cir_bit의 초기화
                                break;
                            //Internet Swipe_Up => Run.... 닫기 동작에 대한 정의
                            case 0x02:
                                swipeGestureRecognizer.cir_bit = 0x00;
                                break;
                                
                            //Bing Map => Run....
                            case 0x04:

                                gdDoc.Visibility = Visibility.Hidden;
                                foreach (Process p in Process.GetProcessesByName("POWERPNT"))
                                {
                                    p.Kill();
                                }
                                
                                swipeGestureRecognizer.cir_bit = 0x00;
                                break;

                            case 0x08:  // Media Ended
                                foreach (Process p in Process.GetProcessesByName("wmplayer"))  //추가
                                {
                                    p.Kill();
                                }
                                swipeGestureRecognizer.cir_bit = 0x00;
                                break;

                            case 0x10:
                                gdPicture.Visibility = Visibility.Hidden;
                                swipeGestureRecognizer.cir_bit = 0x00;
                                break;
                            case 0x20:
                                foreach (Process p in Process.GetProcessesByName("wmplayer"))  //추가
                                {
                                    p.Kill();
                                }
                                gdVideo.Visibility = Visibility.Hidden;
                                swipeGestureRecognizer.cir_bit = 0x00;
                                break;
                        }

                    }
                }

                algorithmicPostureRecognizer.TrackPostures(skeleton);
                templatePostureDetector.TrackPostures(skeleton);

                if (recordNextFrameForPosture)
                {
                    templatePostureDetector.AddTemplate(skeleton);
                    recordNextFrameForPosture = false;
                }
            }

            //skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);

            stabilitiesList.ItemsSource = stabilities;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Clean();
        }

        private void Clean()
        {
            if (swipeGestureRecognizer != null)
            {
                swipeGestureRecognizer.OnGestureDetected -= OnGestureDetected;
            }

            if (audioManager != null)
            {
                audioManager.Dispose();
                audioManager = null;
            }

            if (parallelCombinedGestureDetector != null)
            {
                parallelCombinedGestureDetector.Remove(swipeGestureRecognizer);
                parallelCombinedGestureDetector.Remove(circleGestureRecognizer);
                parallelCombinedGestureDetector.Remove(triangleGestureRecognizer);

                parallelCombinedGestureDetector = null;
                //Add Triangle
            }

            CloseGestureDetector();

            ClosePostureDetector();

            if (voiceCommander != null)
            {
                voiceCommander.OrderDetected -= voiceCommander_OrderDetected;
                voiceCommander.Stop();
                voiceCommander = null;
            }

            if (recorder != null)
            {
                recorder.Stop();
                recorder = null;
            }

            if (eyeTracker != null)
            {
                eyeTracker.Dispose();
                eyeTracker = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.DepthFrameReady -= kinectSensor_DepthFrameReady;
                kinectSensor.SkeletonFrameReady -= kinectRuntime_SkeletonFrameReady;
                kinectSensor.ColorFrameReady -= kinectRuntime_ColorFrameReady;
                kinectSensor.Stop();
                kinectSensor = null;
            }
        }

        private void replayButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Title = "Select filename", Filter = "Replay files|*.replay" };

            if (openFileDialog.ShowDialog() == true)
            {
                if (replay != null)
                {
                    replay.SkeletonFrameReady -= replay_SkeletonFrameReady;
                    replay.ColorImageFrameReady -= replay_ColorImageFrameReady;
                    replay.Stop();
                }
                Stream recordStream = File.OpenRead(openFileDialog.FileName);

                replay = new KinectReplay(recordStream);

                replay.SkeletonFrameReady += replay_SkeletonFrameReady;
                replay.ColorImageFrameReady += replay_ColorImageFrameReady;
                replay.DepthImageFrameReady += replay_DepthImageFrameReady;

                replay.Start();
            }
        }

        void replay_DepthImageFrameReady(object sender, ReplayDepthImageFrameReadyEventArgs e)
        {
            if (!displayDepth)
                return;

            depthManager.Update(e.DepthImageFrame);
        }

        void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            if (displayDepth)
                return;

            colorManager.Update(e.ColorImageFrame);
        }

        void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            ProcessFrame(e.SkeletonFrame);
        }

       
         
        //private void btnPicture_Click(object sender, RoutedEventArgs e)
        private void btnPicture_Click()
        {
            int file_arr = 0; 
            int picture_stack = 0;
            bool pass= true;

            string[] filter = { "jpg", "png", "gif" };
            string path = @"C:\Users\Remoted\Desktop\Jarvis.For.Free.Real\picture";
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(s => filter.Contains(s.Split('.').Last().ToLower())).ToArray();
            stkPicturePanel.Children.Clear();
            imgPreViewPicture.Source = null;

            foreach (string file in files) //총 파일 수만큼 배열를 담을려고 돌림 
                file_arr++;
            
            string[] arr = new string[file_arr]; //파일만큼 배열를 만들고
            string[] arr1 = new string[file_arr]; //파일 이름 배열 추가


            foreach (string file in files)  //작은 그림 출력
            {
                    FileInfo info = new FileInfo(file);
                    PicturePanel panel = new PicturePanel();
                    arr[picture_stack] = info.FullName;  //배열에 경로 넣고
                    arr1[picture_stack] = info.Name;

                    panel.FileName = info.Name;
                    panel.FullName = info.FullName;  
                    if (panel.CreateImage())
                    {
                        stkPicturePanel.Children.Add(panel);
                        
                    }
                    ++picture_stack;
            } //ScrollViewer List implement




           while (pass) 
           {
               if (swipeGestureRecognizer.hand_sig == 22) //연결 할줄모르겟다
               {
                   PicturePanel panel = new PicturePanel();
                   try
                   {
                   panel.FileName = arr1[swipeGestureRecognizer.pic_bit];
                   panel.FullName = arr[swipeGestureRecognizer.pic_bit];

                   if (panel.CreateImage())
                   {
                       imgPreViewPicture.Source = panel.Image; // 중앙패널 띄우기
                       file_arr = 0;
                       break;
                   }
                   }
                   catch (IndexOutOfRangeException ex)
                   {
                       if (swipeGestureRecognizer.pic_bit >= file_arr)
                           swipeGestureRecognizer.pic_bit = 0;
                       else if (swipeGestureRecognizer.pic_bit <= -1)
                           swipeGestureRecognizer.pic_bit = file_arr -1;
                   }

               }

               else if (swipeGestureRecognizer.hand_sig == 11)
               {
                   try { 
                   PicturePanel panel = new PicturePanel();
          

                   panel.FileName = arr1[swipeGestureRecognizer.pic_bit];
                   panel.FullName = arr[swipeGestureRecognizer.pic_bit];

                   if (panel.CreateImage())
                   {
                       imgPreViewPicture.Source = panel.Image; // 중앙패널 띄우기
                       file_arr = 0;
                       break;
                   }
                   }
                   catch (IndexOutOfRangeException ex)
                   {
                       if (swipeGestureRecognizer.pic_bit >= file_arr)
                           swipeGestureRecognizer.pic_bit = 0;
                       else if (swipeGestureRecognizer.pic_bit <= -1)
                           swipeGestureRecognizer.pic_bit = file_arr-1;
                   }

               }
               else //끄기 넣던지
                   pass = false;
                   file_arr = 0;
           }
        }
       



        private void btnVideo_Click()
        {
            int file_arr = 0;
            int video_stack = 0;

            string[] filter = {"wmv"};
            string path = @"C:\Users\Remoted\Desktop\Jarvis.For.Free.Real\video";
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(s => filter.Contains(s.Split('.').Last().ToLower())).ToArray();

            stkVideoPanel.Children.Clear();
            imgPreViewVideo.Source = null;

            foreach (string file in files) //총 파일 수만큼 배열를 담을려고 돌림
                file_arr++;

            string[] arr = new string[file_arr]; //파일만큼 배열를 만들고      

            foreach (string file in files)
            {
                FileInfo info = new FileInfo(file);
                VideoPanel2 panel = new VideoPanel2();

                arr[video_stack] = info.FullName;  //배열에 경로 넣고
                panel.FileName = info.Name;
                panel.FullName = info.FullName;
                
                stkVideoPanel.Children.Add(panel);
                ++video_stack;
            }   // 비디오 스택 소환!

            if (i == 1)
            {
                try
                {
                    i++;
                    Process UserProcess1 = new Process();
                    UserProcess1.StartInfo.UseShellExecute = true;
                    UserProcess1.StartInfo.FileName = @"C:\Users\Remoted\Desktop\Jarvis.For.Free.Real\MyVideo_List.wpl";


                    UserProcess1.StartInfo.CreateNoWindow = true;
                    UserProcess1.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    UserProcess1.Start();

                }
                catch (Exception ex) { }
            }

   
        }

        private void btnDocument_Click()
        {
            string[] filter = { "txt", "ppt", "docx", "hwp" };

            string path = @"C:\Users\Remoted\Desktop\Jarvis.For.Free.Real\document";

            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(s => filter.Contains(s.Split('.').Last().ToLower())).ToArray();

            stkDocPanel.Children.Clear();
            imgPreViewDoc.Source = null;
            foreach (string file in files)
            {
                FileInfo info = new FileInfo(file);
                docPanel panel = new docPanel();
                panel.FileName = info.Name;
                panel.FullName = info.FullName;
                
                //if (panel.CreateImage())
                stkDocPanel.Children.Add(panel);
            }
        }



        private void scrollPictureList_MouseWheel(object sender, MouseWheelEventArgs e)  //인터넷 사용 할때 마우스
        {
            scrollPictureList.ScrollToHorizontalOffset(scrollPictureList.HorizontalOffset - e.Delta);
        }

        private void stkPicturePanel_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollPictureList.ScrollToHorizontalOffset(scrollPictureList.HorizontalOffset - e.Delta);

        }
        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            gdPicture.Visibility = Visibility.Hidden;
        }

        //Browser Targetting Source Started
        //private void browser_Loaded(object sender, RoutedEventArgs e)
        private void browser_Loaded(object sender, RoutedEventArgs e) //인터넷 구현부
        {
            browser.Navigate(new Uri("http://www.naver.com"));
            sw_bit = 0x01;

        }

        private void BingMap_Loaded()  //빙맵 구현부
        {

            //Clean();
            if (i == 1)
            {
                Process UserProcess2 = new Process();
                UserProcess2.StartInfo.UseShellExecute = true;
                UserProcess2.StartInfo.FileName = @"C:\Users\Remoted\Desktop\Jarvis.For.Free.Real\Jarvis_BingMap\KinectBingMap";

                UserProcess2.StartInfo.CreateNoWindow = true;
                UserProcess2.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                kinectSensor.Stop();
                UserProcess2.Start();
            }
        }
        private void Media_Loaded() // 미디어 구현부 -추가-
        {
            if (i == 1)
            {
                try
                {
                    i++;
                    Process UserProcess1 = new Process();
                    UserProcess1.StartInfo.UseShellExecute = true;
                    UserProcess1.StartInfo.FileName = @"C:\Users\Remoted\Desktop\Jarvis.For.Free.Real\Windows Media Player1.wpl";
                   
                    
                    UserProcess1.StartInfo.CreateNoWindow = true;
                    UserProcess1.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    UserProcess1.Start();
                   
                }
                catch (Exception ex) { }
            }

        }
        private void PPT_Loaded() // 피피티 구현부
        {
            if (i == 1)
            {
                try
                {
                    i++;
                    Process UserProcess6 = new Process();
                    UserProcess6.StartInfo.UseShellExecute = true;
                    UserProcess6.StartInfo.FileName = @"C:\Users\Remoted\Desktop\Jarvis.For.Free.Real\Jarvis.For.Free - MIT.ppt";



                    UserProcess6.StartInfo.CreateNoWindow = true;
                    UserProcess6.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    UserProcess6.Start();

                }
                catch (Exception ex) { }
            }

        }

        public int sw_bit { get; set; }
        }
    }
