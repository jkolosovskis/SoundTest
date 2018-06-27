using System;
using NAudio.Wave;
using System.Timers;

namespace SoundTest
{
    class RecordManager
    {
        WaveInEvent waveIn;
        private WaveFileWriter waveFileWriter;
        private WaveFormat waveFormat;
        private string outputFilePath;
        private Timer recordLimitTimer;
        private int recordLengthMs = 10 * 1000;
        private int instanceIdentifier;
        private HttpManager httpManager;
        public event EventHandler RaiseRecordFinishEvent;

        /// <summary>
        /// Default constructor. Sets up tools for recording audio and saving files to a temporary location.
        /// </summary>
        /// <param name="instanceNumber">Smple number to identify the particular instance of RecordManager.</param>
        public RecordManager(int instanceNumber)
        {
            // Let the user know that a new instance of RecordManager is created.
            Console.WriteLine("Creating RecordManager instance " + instanceNumber.ToString());
            // Set up object that will perform sound capture
            waveIn = new WaveInEvent();
            waveIn.BufferMilliseconds = 100;
            waveIn.NumberOfBuffers = 2;
            // Device number 0 - default recording device.
            waveIn.DeviceNumber = 0;
            Console.WriteLine("Using recording device " 
                              + WaveIn.GetCapabilities(waveIn.DeviceNumber).ProductName.ToString());
            Console.WriteLine("44.1kHz stereo WAV format supported? : " 
                              + WaveIn.GetCapabilities(waveIn.DeviceNumber)
                              .SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_44S16).ToString());
            // Specify recorded file storage parameters - format and location.
            instanceIdentifier = instanceNumber;
            waveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(waveIn.DeviceNumber).Channels);
            outputFilePath = "sample" + instanceIdentifier.ToString() + ".wav";
            waveFileWriter = new WaveFileWriter(outputFilePath, waveFormat);
            waveIn.WaveFormat = waveFormat;
            // Define a timer that will help in limiting the length of the record to
            // a specified length. Default - 10 seconds.
            recordLimitTimer = new Timer(recordLengthMs);
            // Specify the timer object to trigger only once.
            recordLimitTimer.AutoReset = false;
        }

        /// <summary>
        /// Starts recording audio and sets up a timer to automatically stop recording after a defined time.
        /// </summary>
        public void StartRecording()
        {
            // Start audio recording.
            Console.WriteLine("Started recording sample " + instanceIdentifier.ToString() + ".");
            waveIn.StartRecording();

            // Start timer for limiting record length.
            recordLimitTimer.Start();

            // Register an event handler to write recorded data to memory.
            // The DataAvailable event gets raised periodically by the NAudio library.
            waveIn.DataAvailable += (s, a) => WriteToBuffer(s, a);

            // Register an event handler when record length has reached its specified value.
            // See the definition of PerformFinishActions for details on what the method does.
            recordLimitTimer.Elapsed += (s, a) => PerformFinishActions();
        }

        /// <summary>
        /// Method for writing recorded data to a file.
        /// </summary>
        /// <param name="sender">Reference to the object that raised the event.</param>
        /// <param name="args">Set of WaveInEventArgs arguments for processing by the event handler.</param>
        /// <remarks>
        /// The write to memory operation here is described as a named method instead of a lambda function,
        /// since we will need to deregister it from the DataAvailable event handler.
        /// </remarks>
        private void WriteToBuffer(object sender, WaveInEventArgs args)
        {
            // Console.WriteLine(String.Format("Transferring {0} bytes of buffer data to file.", args.BytesRecorded));
            waveFileWriter.Write(args.Buffer, 0, args.BytesRecorded);
            waveFileWriter.Flush();
        }

        /// <summary>
        /// Method which performs a set of closure actions once a sound record of sufficient length is completed.
        /// </summary>
        private async void PerformFinishActions()
        {
            // Deregister the event handler that performed wave data writing to file.
            waveIn.DataAvailable -= (s, a) => WriteToBuffer(s, a);

            // Stop recording audio data.  
            Console.WriteLine("Stopping recording for RecordManager instance " + instanceIdentifier.ToString());
            waveIn.StopRecording();

            waveIn.Dispose();
            waveFileWriter.Dispose();

            // Raise an event to notify parent code that a new RecordManager instance can be created.
            // To avoid race conditions on access to system microphone by RecordManager instances,
            // a 50ms delay is added here.
            System.Threading.Thread.Sleep(50);
            RaiseRecordFinishEvent(this, new EventArgs());

            // Create a new instance of HttpManager to handle transfer of recorded file to server.
            // Then instruct the httpManager instance to start file transfer.
            httpManager = new HttpManager(outputFilePath);
            await httpManager.UploadWavFile();
        }
    }
}
