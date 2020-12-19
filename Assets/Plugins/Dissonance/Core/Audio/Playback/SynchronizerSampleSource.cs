using System;
using System.Diagnostics;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    internal class SynchronizerSampleSource
        : ISampleSource, IRateProvider
    {
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof(SynchronizerSampleSource).Name);
        private static readonly float[] DesyncFixBuffer = new float[1024];

        private readonly ISampleSource _upstream;
        private readonly TimeSpan _resetDesyncTime;
        private readonly IDecoderPipeline _pipeline;
        private readonly Stopwatch _timer = new Stopwatch();

        public TimeSpan IdealPlaybackPosition
        {
            get
            {
                return _timer.Elapsed;
            }
        }

        private long _totalSamplesRead;
        public TimeSpan PlaybackPosition
        {
            get
            {
                return TimeSpan.FromSeconds(_totalSamplesRead / (double)WaveFormat.SampleRate);
            }
        }

        private DesyncCalculator _desync;
        
        public TimeSpan Desync
        {
            get { return TimeSpan.FromMilliseconds(_desync.DesyncMilliseconds); }
        }

        public WaveFormat WaveFormat
        {
            get { return _upstream.WaveFormat; }
        }

        public float PlaybackRate
        {
            get; private set;
        }

        public SyncState State
        {
            get
            {
                return new SyncState(PlaybackPosition, IdealPlaybackPosition, Desync, PlaybackRate);
            }
        }

        public SynchronizerSampleSource(ISampleSource upstream, TimeSpan resetDesyncTime, IDecoderPipeline pipeline)
        {
            _upstream = upstream;
            _resetDesyncTime = resetDesyncTime;
            _pipeline = pipeline;
        }

        public void Prepare(SessionContext context)
        {
            _timer.Reset();

            _desync = new DesyncCalculator();
            PlaybackRate = 1;
            _totalSamplesRead = 0;

            _upstream.Prepare(context);
        }

        public void Reset()
        {
            _timer.Reset();

            _upstream.Reset();
        }

        public bool Read(ArraySegment<float> samples)
        {
            // Start the timer when the first sample is read. All subsequent timing will be based off this.
            if (!_timer.IsRunning)
            {
                _timer.Reset();
                _timer.Start();
            }

            // We always read the amount of requested data, update the count of total data read now
            _totalSamplesRead += samples.Count;

            // If the buffer is too small slighty increase the count of samples read (by 0.1ms) Desync compensation will think it is ahead of
            // where it should be and will slow down playback, which will cause the buffer to grow.
            if (_pipeline.BufferCount < 1)
                _totalSamplesRead += WaveFormat.SampleRate / 10000;

            // Calculate how out of sync playback is (based on actual samples read vs time passed)
            _desync.Update(IdealPlaybackPosition, PlaybackPosition);

            // If playback rate is too fast, slow down immediately (to prevent exhausting the buffer). If playback speed is too slow, ramp up over the next few frames.
            var corrected = _desync.CorrectedPlaybackSpeed;
            PlaybackRate = corrected < PlaybackRate
                         ? corrected
                         : Mathf.LerpUnclamped(PlaybackRate, _desync.CorrectedPlaybackSpeed, 0.25f);

            // Skip audio if necessary to resync the audio stream
            int skippedSamples;
            int skippedMilliseconds;
            var complete = Skip(_desync.DesyncMilliseconds, out skippedSamples, out skippedMilliseconds);
            if (skippedSamples > 0)
            {
                _totalSamplesRead += skippedSamples;
                _desync.Skip(skippedMilliseconds);
            }

            // If skipping completed the session return silence
            if (complete)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                Array.Clear(samples.Array, samples.Offset, samples.Count);
                return true;
            }

            // Read from upstream
            return _upstream.Read(samples);
        }

        private bool Skip(int desyncMilliseconds, out int deltaSamples, out int deltaDesyncMilliseconds)
        {
            // We're too far behind where we ought to be to resync with speed adjustment. Skip ahead to where we should be.
            if (desyncMilliseconds > _resetDesyncTime.TotalMilliseconds)
            {
                Log.Warn("Playback desync ({0}ms) beyond recoverable threshold; resetting stream to current time", desyncMilliseconds);

                deltaSamples = desyncMilliseconds * WaveFormat.SampleRate / 1000;
                deltaDesyncMilliseconds = -desyncMilliseconds;

                // Configure playback rate to normal speed before discarding the data to ensure we discard exactly as much data as we expect
                PlaybackRate = 1;

                //Read out a load of data and discard it, forcing ourselves back into sync
                //If reading completes the session exit out.
                var toRead = deltaSamples;
                while (toRead > 0)
                {
                    var count = Math.Min(toRead, DesyncFixBuffer.Length);
                    if (_upstream.Read(new ArraySegment<float>(DesyncFixBuffer, 0, count)))
                        return true;
                    toRead -= count;
                }

                //We completed all the reads so obviously none of the reads finished the session
                return false;
            }

            // We're a long way ahead of where we should be. There's no way to correct that.
            // Silence could be inserted to make up the time if this becomes an issue, but it should be very rare.
            if (desyncMilliseconds < -_resetDesyncTime.TotalMilliseconds)
            {
                Log.Error("Playback desync ({0}ms) AHEAD beyond recoverable threshold", desyncMilliseconds);
            }

            deltaSamples = 0;
            deltaDesyncMilliseconds = 0;
            return false;
        }
    }

    public struct SyncState
    {
        public readonly TimeSpan ActualPlaybackPosition;
        public readonly TimeSpan IdealPlaybackPosition;
        public readonly TimeSpan Desync;
        public readonly float CompensatedPlaybackSpeed;

        public SyncState(TimeSpan actualPlaybackPosition, TimeSpan idealPlaybackPosition, TimeSpan desync, float compensatedPlaybackSpeed)
            : this()
        {
            ActualPlaybackPosition = actualPlaybackPosition;
            IdealPlaybackPosition = idealPlaybackPosition;
            Desync = desync;
            CompensatedPlaybackSpeed = compensatedPlaybackSpeed;
        }
    }
}
