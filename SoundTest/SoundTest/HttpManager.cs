using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoundTest
{
    class HttpManager
    {
        private string wavContentFile;
        private string requestUrl;
        private int MaxRetries { get; set; }

        /// <summary>
        /// Default constructor. Populates POST address for WAV file upload and reference WAV file name.
        /// </summary>
        /// <param name="sendFileName">Name of WAV file to be sent to server.</param>
        public HttpManager(string sendFileName)
        {
            wavContentFile = sendFileName;
            requestUrl = "http://therentistoodamnhigh.co.uk/api.php";
            // Note that MaxRetries parameter could be part of the constructor arugments.
            // However, in this case, since it is up to the code author to define a retry strategy,
            // it is hard coded to a desired value.
            MaxRetries = 3;
        }

        /// <summary>
        /// Method which performs HTTP POST request transmission to remote server.
        /// </summary>
        /// <param name="wavFileContent">Byte array of the WAV file to be transmitted.</param>
        /// <param name="fileHash">SHA256 hash of the WAV file to be transmitted.</param>
        /// <returns>Response HTTP message from remote server, or a HTTP error code.</returns>
        private async Task<HttpResponseMessage> PerformPostRequest(HttpContent wavFileContent)
        {
            using (HttpClient client = new HttpClient())
            using (MultipartFormDataContent formData = new MultipartFormDataContent())
            {
                // Define the URL of the REST API method to be invoked on server side:
                HttpContent fileNameContent = new StringContent(wavContentFile);
                formData.Add(wavFileContent, "wavFile");
                formData.Add(fileNameContent, "name");
                HttpResponseMessage response = await client.PostAsync(requestUrl + "?action=add_file", formData);
                return response;
            }
        }

        /// <summary>
        /// Method which checks for errors in response to a previously executed HTTP POST request.
        /// </summary>
        /// <param name="response">HTTP response to be evaluated.</param>
        /// <returns>True if response is OK, otherwise returns false.</returns>
        private bool EvaluatePostResponse(HttpResponseMessage response)
        {
            // Check if the response contains a positive HTTP level response code.
            if (!response.IsSuccessStatusCode)
            {
                // Not good.
                // Let the user know with an error printout what happened.
                Console.WriteLine("ERROR: HTTP POST request for sample "
                                    + wavContentFile
                                    + " returned error code " +
                                    response.StatusCode.ToString());
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Performs managed transmission of specified WAV file and its SHA256 hash to remote server.
        /// </summary>
        /// <remarks>
        /// The method includes hash calculation for WAV file and execution of a retry strategy.
        /// </remarks>
        public async Task UploadWavFile()
        {
            // Read the contents of the .WAV file into a byte array for later use.
            // Note that this task could be optimised by skipping the part where the
            // WAV file is written to non-volatile memory. In principle, the parent RecordManager
            // instance could have passed a byte array to this class.
            // TO BE REVIEWED IN CASE OF PERFORMANCE ISSUES.
            // We expect our WAV file to be no larger than 20MB in size.
            byte[] wavFileData;
            using (FileStream wavFileStream = new FileStream(wavContentFile, FileMode.Open))
            {
                wavFileData = new byte[(int)wavFileStream.Length];
                try
                {
                    wavFileStream.Read(wavFileData, 0, (int)wavFileStream.Length);
                }
                catch (ArgumentException)
                {
                    Console.Error.WriteLine("HttpManager error: WAV file size exceeds allocated buffer size of 20MB.");
                }
            }

            // Create HttpContent objects to mimic form data for building a POST request.
            HttpContent wavFileContent = new ByteArrayContent(wavFileData);

            // Transmit the HTTP post request.
            HttpResponseMessage response = await PerformPostRequest(wavFileContent);

            // Define a retransmission mechanism in case of failures.
            // In our example, we use a simple limited re-transmission mechanism before giving up.
            for (int retryNumber = 1; retryNumber <= MaxRetries; retryNumber++)
            {
                bool isResponseOk = EvaluatePostResponse(response);
                if (isResponseOk == false)
                {
                    // Something went wrong. Let's try again.
                    Console.WriteLine("Performing re-transmission "
                                      + retryNumber.ToString()
                                      + " out of "
                                      + MaxRetries.ToString());
                    // Note that here we do not re-create a new HTTP request.
                    // This is intentional, as it is assumed that the request creation is always successful.
                    // To be changed in case of errors in this assumption found during testing.
                    response = await PerformPostRequest(wavFileContent);
                }
                else
                {
                    // Everything went well. Let the user know with a printout.
                    Console.WriteLine("POST request for sample "
                                      + wavContentFile
                                      + " acknowledged positively by server.");

                    break;
                }
            }
        }
    }
}
