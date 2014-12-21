using System.Linq;
using System.IO;
using Microsoft.Kinect;

namespace Kinect.Toolbox
{
    public class TemplatedGestureDetector : GestureDetector
    {
        public float Epsilon { get; set; }
        public float MinimalScore { get; set; }
        public float MinimalSize { get; set; }
        readonly LearningMachine learningMachine;
        RecordedPath path;
        readonly string gestureName;

        public bool IsRecordingPath
        {
            get { return path != null; }
        }

        public LearningMachine LearningMachine
        {
            get { return learningMachine; }
        }

        public TemplatedGestureDetector(string gestureName, Stream kbStream, int windowSize = 60)
            : base(windowSize)
        {
            if (gestureName == "Triangle")
            {
                Epsilon = 0.030f; // 0.035 -> 0.045
                MinimalScore = 0.358f; // 0.348 -> 0.400
                MinimalSize = 0.2f; // 0.2 -> 0.1
                this.gestureName = gestureName;
                learningMachine = new LearningMachine(kbStream);
                //조져야할 부분 --> Success
            }
            else if(gestureName == "Circle")
            {
                Epsilon = 0.035f;
                MinimalScore = 0.80f; //0.80 -> 0.75
                MinimalSize = 0.1f;
                this.gestureName = gestureName;
                learningMachine = new LearningMachine(kbStream);
            }
            else
            {
                Epsilon = 0.035f;
                MinimalScore = 0.80f; //0.80 -> 0.75
                MinimalSize = 0.1f;
                this.gestureName = gestureName;
                learningMachine = new LearningMachine(kbStream);
            }
            
        }

        public override void Add(SkeletonPoint position, KinectSensor sensor)
        {
            base.Add(position, sensor);

            if (path != null)
            {
                path.Points.Add(position.ToVector2());
            }
        }

        protected override void LookForGesture()
        {
            if (LearningMachine.Match(Entries.Select(e => new Vector2(e.Position.X, e.Position.Y)).ToList(), Epsilon, MinimalScore, MinimalSize))
                RaiseGestureDetected(gestureName);
        }

        public void StartRecordTemplate()
        {
            path = new RecordedPath(WindowSize);
        }

        public void EndRecordTemplate()
        {
            LearningMachine.AddPath(path);
            path = null;
        }

        public void SaveState(Stream kbStream)
        {
            LearningMachine.Persist(kbStream);
        }
    }
}
