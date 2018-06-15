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
        private static UInt64 NumberOfTestIterations;
        private static string SoundFilePath;

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
            SoundFilePath = Directory.GetCurrentDirectory() + @args[0];
            if (File.Exists(@SoundFilePath) == false)
            {
                Console.Error.WriteLine("Could not resolve provided sound file reference.");
                Console.Error.WriteLine("Expected reference file format: <relative_path>/<filename.extension>");
                return false;
            }

            // Check if the second argument is a natural number and can fit in a 64bit uint.
            try
            {
                NumberOfTestIterations = Convert.ToUInt64(args[1]);
            }
            catch (FormatException)
            {
                Console.Error.WriteLine("Could not convert second argument to a valid natural number.");
                return false;
            }
            catch (OverflowException)
            {
                Console.Error.WriteLine("Specified number of test cycles exceeds capacity of a 64 bit unsigned integer.");
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

            // Create instances of our playback, recording and HTTP managers.

            PlaybackManager playbackManager = new PlaybackManager(SoundFilePath);


            // If we got until here, it means that we are ready to start working.
            // Let's start with setting up playback of the specified audio file.

            playbackManager.PlaySound();
        }
    }
}
