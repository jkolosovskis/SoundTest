﻿using System;
using System.IO;
using System.Collections.Generic;

namespace SoundTest
{
    static class SoundTest
    {
        static int CurrentRecordManagerInstance { get; set; }

        private static int numberOfTestIterations;
        private static string soundFilePath;
        private static List<RecordManager> recordManagerList = new List<RecordManager>();
        private static PlaybackManager playbackManager;

        /// <summary>
        /// Performs starting checks to see if the program was correctly launched.
        /// </summary>
        /// <param name="args">Set of arguments supplied when program was launched.</param>
        private static bool Init(string[] args)
        {
            // Start by letting the user know how to use this program.
            Console.WriteLine("Welcome to SoundTest.");
            Console.WriteLine("Expected use: SoundTest.exe <relative path of playback WAV file> "
                              + "<number of test iterations>");
            Console.WriteLine("Arguments for application supplied during launch:");
            // Check if correct number of arguments has been supplied.
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Too few arguments supplied. At least 2 arguments expected.");
                return false;
            }
            // Let the user know how the program understood its arguments input.
            Console.WriteLine("Playback file: " + args[0]);
            Console.WriteLine("Number of test iterations: " + args[1]);

            // Check if the specified file exists.
            // We don't check for correct file format here, only that the file path is valid.
            soundFilePath = Directory.GetCurrentDirectory() + @"\" + @args[0];
            if (File.Exists(@soundFilePath) == false)
            {
                Console.Error.WriteLine("Could not resolve provided sound file reference.");
                Console.Error.WriteLine("Current working directory: " + Directory.GetCurrentDirectory());
                Console.Error.WriteLine("File target interpreted as " + soundFilePath);
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

            // Initialise the current record sample counter to zero.
            CurrentRecordManagerInstance = 0;

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

            // Send a command to clear all existing external server file entries.
            // Note that the call is not awaited, which is intentional.
            ServerInitialiser.ClearWavfilesDatabase();

            // Create instances of our playback and recording managers.
            playbackManager = new PlaybackManager(soundFilePath);
            recordManagerList.Add(new RecordManager(CurrentRecordManagerInstance));

            // If we got until here, it means that we are ready to start working.
            // Start playback of the specified audio file.
            playbackManager.PlaySound();

            // Start recording external audio via system micropohone.
            recordManagerList[CurrentRecordManagerInstance].StartRecording();

            // Set up an event handler to automatically create new instances of
            // RecordManager to continuously record external audio as soon
            // as previous instance has finished its work.
            recordManagerList[CurrentRecordManagerInstance].RaiseRecordFinishEvent += (s, a) =>
                HandleRecordFinishEvent(s, a);

            // Wait for user input in order to freeze the main thread and allow early quit
            // from the application if necessary.
            Console.WriteLine("Press any key to quit the application.");
            Console.ReadKey();
        }

        /// <summary>
        /// Event handler function to be used for managing recording of subsequent samples.
        /// </summary>
        /// <param name="sender">Reference to the caller object.</param>
        /// <param name="args">Generic event arguments instance (no data expected to be passed).</param>
        static void HandleRecordFinishEvent(object sender, EventArgs args)
        {
            // Increment the record manager instance number and create a new instance,
            // if necessary.
            CurrentRecordManagerInstance++;
            if (CurrentRecordManagerInstance < numberOfTestIterations)
            {
                recordManagerList.Add(new RecordManager(CurrentRecordManagerInstance));

                // Instruct the new record manager to start recording.
                recordManagerList[CurrentRecordManagerInstance].StartRecording();

                // Register an event handler for the new record manager finish event.
                recordManagerList[CurrentRecordManagerInstance].RaiseRecordFinishEvent += (s, a) =>
                    HandleRecordFinishEvent(s, a);
            }
            else
            {
                // Stop sound playback as soon as the specified amount of work is done.
                playbackManager.StopSound();
                Console.WriteLine("All record / playback iterations completed.");
            }
         
            // Deregister the event handler for the previous RecordManager instance finish event.
            // Not strictly necessary, but good practice to do so in this case.
            recordManagerList[CurrentRecordManagerInstance - 1].RaiseRecordFinishEvent -= (s, a) =>
                HandleRecordFinishEvent(s, a);
        }
    }
}
