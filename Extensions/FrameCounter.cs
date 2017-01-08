using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XChunk.Extensions
{
 public class FrameCounter
{
    public FrameCounter()
    {
    }

    public long TotalFrames { get; private set; }
    public float TotalSeconds { get; private set; }
    public float AbnormalFramesPerSecond { get; private set; }
    public float CurrentFramesPerSecond { get; private set; }

    public const int MAXIMUM_SAMPLES = 100;

    private Queue<float> _sampleBuffer = new Queue<float>();

    public bool Update(float deltaTime)
    {
        CurrentFramesPerSecond = 1.0f / deltaTime;

        _sampleBuffer.Enqueue(CurrentFramesPerSecond);

        if (_sampleBuffer.Count > MAXIMUM_SAMPLES)
        {
            _sampleBuffer.Dequeue();
            AbnormalFramesPerSecond = _sampleBuffer.Average(i => i);
        } 
        else
        {
            AbnormalFramesPerSecond = CurrentFramesPerSecond;
        }

        TotalFrames++;
        TotalSeconds += deltaTime;
        return true;
    }
}
}
