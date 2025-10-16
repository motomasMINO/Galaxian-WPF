using System;
using NAudio.Wave;

namespace Galaxian
{
    public class Sound
    {
        private string filePath;
        private WaveOutEvent? outputDevice;
        private AudioFileReader? audioFile;
        private LoopStream? loopStream;

        public Sound(string path)
        {
            filePath = path;
        }

        public void Play()
        {
            Stop(); // ä˘Ç…çƒê∂íÜÇ»ÇÁí‚é~

            outputDevice = new WaveOutEvent();
            audioFile = new AudioFileReader(filePath);
            outputDevice.Init(audioFile);
            outputDevice.Play();
        }

        public void Loop()
        {
            Stop(); // ä˘Ç…çƒê∂íÜÇ»ÇÁí‚é~

            outputDevice = new WaveOutEvent();
            audioFile = new AudioFileReader(filePath);
            loopStream = new LoopStream(audioFile);
            outputDevice.Init(loopStream);
            outputDevice.Play();
        }

        public void Stop()
        {
            outputDevice?.Stop();
            outputDevice?.Dispose();
            outputDevice = null;

            audioFile?.Dispose();
            audioFile = null;

            loopStream?.Dispose();
            loopStream = null;
        }
    }
}
