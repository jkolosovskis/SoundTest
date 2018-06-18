using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTest
{
    static class SoundTest
    {
        private static int numberOfTestIterations;
        private static string soundFilePath;
        private static int currentRecordManagerInstance = 0;
        private static List<RecordManager> recordManagerList = new List<RecordManager>();

        /// <summary>
        /// Performs starting checks to see if the program was correctly launched.
        /// </summary>
        /// <param name="args">Set of arguments supplied when program was launched.</param>
        private static bool Init(string[] args)
        {
            // Start by letting the user know how to use this program.
            Console.WriteLine("Welcome to SoundTest.");
            Console.WriteLine("Expected use: SoundTest.exe <relative path of playback WAV file>"
                              + "<number of test iterations>");
            Console.WriteLine("Arguments for application supplied during launch:");
            Console.WriteLine("Playback file: " + args[0]);
            Console.WriteLine("Number of test iterations: " + args[1]);

            // Check if correct number of arguments has been supplied.
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Too few arguments supplied. At least 2 arguments expected.");
                return false;
            }

            // Check if the specified file exists.
            // We don't check for correct file format here, only that the file path is valid.
            soundFilePath = Directory.GetCurrentDirectory() + @args[0];
            if (File.Exists(@soundFilePath) == false)
            {
                Console.Error.WriteLine("Could not resolve provided sound file reference.");
                Console.Error.WriteLine("Expected reference file format: <relative_path>/<filename.extension>");
                return false;
            }

            // Check if the second argument is a natural number and can fit in a 64bit uint.
            try
            {
                numberOfTestIterations = Convert.ToInt32(args[1]);
                if (numberOfTestIterations < 1)
                {
                    // We received zero or a negative number. That's no good.
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                Console.Error.WriteLine("Could not convert second argument to a valid non-zero natural number.");
                return false;
            }
            catch (OverflowException)
            {
                Console.Error.WriteLine("Specified number of test cycles exceeds capacity of a 32 bit signed integer.");
                return false;
            }

            return true;
        }

        static void Main(string[] args)
        {
            if (Init(args) == false)
            {
                // Something failed in the way how the program was launched.
                // Our way of recovery is by asking the user to restart the application and re-specify
                // running arguments.
                Console.WriteLine("Press any key to terminate application.");
                Console.ReadKey();
                return;
            }

            // Create instances of our playback and recording managers.
            PlaybackManager playbackManager = new PlaybackManager(soundFilePath);
            recordManagerList.Add(new RecordManager(currentRecordManagerInstance));

            // If we got until here, it means that we are ready to start working.
            // Start playback of the specified audio file.
            playbackManager.PlaySound();

            // Start recording external audio via system micropohone.
            recordManagerList[currentRecordManagerInstance].StartRecording();

            // Set up an event handler to automatically create new instances of
            // RecordManager to continuously record external audio as soon
            // as previous instance has finished its work.
            recordManagerList[currentRecordManagerInstance].RaiseRecordFinishEvent += (s, a) =>
                HandleRecordFinishEvent(s, a);
        }
        static void HandleRecordFinishEvent(object sender, EventArgs args)
        {
            // Increment the record manager instance number and create a new instance.
            currentRecordManagerInstance++;
            recordManagerList.Add(new RecordManager(currentRecordManagerInstance));

            // Register an event handler for the new record manager finish event.
            recordManagerList[currentRecordManagerInstance].RaiseRecordFinishEvent += (s, a) =>
                HandleRecordFinishEvent(s, a);

            // Deregister the event handler for the previous RecordManager instance finish event.
            // Not strictly necessary, but good practice to do so in this case.
            recordManagerList[currentRecordManagerInstance - 1].RaiseRecordFinishEvent -= (s, a) =>
                HandleRecordFinishEvent(s, a);
        }
    }
}
