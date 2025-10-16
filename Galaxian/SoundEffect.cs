using System;
using System.IO;
using NAudio.Wave;

namespace Galaxian
{
    public class SoundEffect : IDisposable
    {
        private byte[] soundData;

        public SoundEffect(string path)
        {
            using (var reader = new AudioFileReader(path))
            using (var ms = new MemoryStream())
            {
                WaveFileWriter.WriteWavFileToStream(ms, reader);
                soundData = ms.ToArray();
            }
        }

        public void Play()
        {
            var ms = new MemoryStream(soundData);
            var reader = new WaveFileReader(ms);
            var output = new WaveOutEvent();
            output.Init(reader);
            output.Play();
            output.PlaybackStopped += (s, e) =>
            {
                output.Dispose();
                reader.Dispose();
                ms.Dispose();
            };
        }

        public void Dispose()
        {
            soundData = null;
        }
    }
}
