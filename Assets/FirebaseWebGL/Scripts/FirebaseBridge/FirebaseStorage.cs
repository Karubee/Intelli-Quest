using System.Runtime.InteropServices;

namespace FirebaseWebGL.Scripts.FirebaseBridge
{
    public static class FirebaseStorage
    {
        /// <summary>
        /// Uploads a byte array to storage
        /// </summary>
        /// <param name="path"> Storage path </param>
        /// <param name="data"> Bytes to upload encoded in a base 64 string </param>
        /// <param name="objectName"> Name of the gameobject to call the callback/fallback of </param>
        /// <param name="callback"> Name of the method to call when the operation was successful. Method must have signature: void Method(string output) </param>
        /// <param name="fallback"> Name of the method to call when the operation was unsuccessful. Method must have signature: void Method(string output). Will return a serialized FirebaseError object </param>
        [DllImport("__Internal")]
        public static extern void UploadFileToStorage(string path, string data, string objectName, string callback, string fallback);

        /// <summary>
        /// Downloads a byte array from storage
        /// </summary>
        /// <param name="path"> Storage path </param>
        /// <param name="objectName"> Name of the gameobject to call the callback/fallback of </param>
        /// <param name="callback"> Name of the method to call when the operation was successful. Method must have signature: void Method(string output). Will return a base 64 encoded string </param>
        /// <param name="fallback"> Name of the method to call when the operation was unsuccessful. Method must have signature: void Method(string output). Will return a serialized FirebaseError object </param>
        [DllImport("__Internal")]
        
        public static extern void DownloadFileFromStorage(string path, string objectName, string callback, string fallback);
        /// <summary>
        /// Lists all files in the specified storage path.
        /// </summary>
        /// <param name="path"> Storage path to list files from </param>
        /// <param name="objectName"> Name of the gameobject to call the callback/fallback of </param>
        /// <param name="callback"> Name of the method to call when the operation was successful. Method must have signature: void Method(string output) </param>
        /// <param name="fallback"> Name of the method to call when the operation was unsuccessful. Method must have signature: void Method(string output). Will return a serialized FirebaseError object </param>
        [DllImport("__Internal")]
        public static extern void ListFilesFromStorage(string path, string objectName, string callback, string fallback);

        /// <summary>
        /// Gets the download URL for a specified file in storage.
        /// </summary>
        /// <param name="path"> Storage path of the file </param>
        /// <param name="objectName"> Name of the gameobject to call the callback/fallback of </param>
        /// <param name="callback"> Name of the method to call when the operation was successful. Method must have signature: void Method(string output) </param>
        /// <param name="fallback"> Name of the method to call when the operation was unsuccessful. Method must have signature: void Method(string output). Will return a serialized FirebaseError object </param>
        [DllImport("__Internal")]
        public static extern void GetFirebaseDownloadUrl(string path, string objectName, string callback, string fallback);

        /// <summary>
        /// Deletes a file from Firebase Storage
        /// </summary>
        /// <param name="path"> The path of the file to delete </param>
        /// <param name="objectName"> Name of the GameObject to call the callback/fallback on </param>
        /// <param name="callback"> Name of the method to call when the operation is successful. Method must have signature: void Method(string message) </param>
        /// <param name="fallback"> Name of the method to call when the operation is unsuccessful. Method must have signature: void Method(string errorMessage) </param>
        [DllImport("__Internal")]
        public static extern void DeleteFile(string path, string objectName, string callback, string fallback);
    }
}