mergeInto(LibraryManager.library, {
  // Existing functions for uploading and downloading files
  UploadFileToStorage: function (path, data, objectName, callback, fallback) {
    var parsedPath = Pointer_stringify(path);
    var parsedData = Pointer_stringify(data);
    var parsedObjectName = Pointer_stringify(objectName);
    var parsedCallback = Pointer_stringify(callback);
    var parsedFallback = Pointer_stringify(fallback);

    try {
      firebase
        .storage()
        .ref(parsedPath)
        .put(base64ToArrayBuffer(parsedData))
        .then(function (snapshot) {
          return snapshot.ref.getDownloadURL();
        })
        .then(function (downloadUrl) {
          unityInstance.Module.SendMessage(
            parsedObjectName,
            parsedCallback,
            downloadUrl
          );
        })
        .catch(function (error) {
          unityInstance.Module.SendMessage(
            parsedObjectName,
            parsedFallback,
            JSON.stringify(error, Object.getOwnPropertyNames(error))
          );
        });
    } catch (error) {
      unityInstance.Module.SendMessage(
        parsedObjectName,
        parsedFallback,
        JSON.stringify(error, Object.getOwnPropertyNames(error))
      );
    }

    function base64ToArrayBuffer(base64) {
      var binary_string = window.atob(base64);
      var len = binary_string.length;
      var bytes = new Uint8Array(len);
      for (var i = 0; i < len; i++) {
        bytes[i] = binary_string.charCodeAt(i);
      }
      return bytes.buffer;
    }
  },

  DownloadFileFromStorage: function (path, objectName, callback, fallback) {
    var parsedPath = Pointer_stringify(path);
    var parsedObjectName = Pointer_stringify(objectName);
    var parsedCallback = Pointer_stringify(callback);
    var parsedFallback = Pointer_stringify(fallback);

    try {
      firebase
        .storage()
        .ref(parsedPath)
        .getDownloadURL()
        .then(function (url) {
          var xhr = new XMLHttpRequest();
          xhr.responseType = "arraybuffer";
          xhr.onload = function (event) {
            var data = xhr.response;
            unityInstance.Module.SendMessage(
              parsedObjectName,
              parsedCallback,
              arrayBufferToBase64(data)
            );
          };
          xhr.open("GET", url);
          xhr.send();
        })
        .catch(function (error) {
          unityInstance.Module.SendMessage(
            parsedObjectName,
            parsedFallback,
            JSON.stringify(error, Object.getOwnPropertyNames(error))
          );
        });
    } catch (error) {
      unityInstance.Module.SendMessage(
        parsedObjectName,
        parsedFallback,
        JSON.stringify(error, Object.getOwnPropertyNames(error))
      );
    }

    function arrayBufferToBase64(buffer) {
      var binary = "";
      var bytes = new Uint8Array(buffer);
      var len = bytes.byteLength;
      for (var i = 0; i < len; i++) {
        binary += String.fromCharCode(bytes[i]);
      }
      return window.btoa(binary);
    }
  },

  // New function to list files in Firebase Storage
  ListFilesFromStorage: function (path, objectName, callback, fallback) {
    var parsedPath = Pointer_stringify(path);
    var parsedObjectName = Pointer_stringify(objectName);
    var parsedCallback = Pointer_stringify(callback);
    var parsedFallback = Pointer_stringify(fallback);

    try {
      var storageRef = firebase.storage().ref(parsedPath);
      storageRef
        .listAll()
        .then(function (res) {
          var files = res.items.map(function (item) {
            return item.name;
          });
          var filesJson = JSON.stringify({ files: files });
          unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, filesJson);
        })
        .catch(function (error) {
          unityInstance.Module.SendMessage(
            parsedObjectName,
            parsedFallback,
            JSON.stringify(error, Object.getOwnPropertyNames(error))
          );
        });
    } catch (error) {
      unityInstance.Module.SendMessage(
        parsedObjectName,
        parsedFallback,
        JSON.stringify(error, Object.getOwnPropertyNames(error))
      );
    }
  },

  // New function to get download URL for a file in Firebase Storage
  GetFirebaseDownloadUrl: function (path, objectName, callback, fallback) {
    var parsedPath = Pointer_stringify(path);
    var parsedObjectName = Pointer_stringify(objectName);
    var parsedCallback = Pointer_stringify(callback);
    var parsedFallback = Pointer_stringify(fallback);

    try {
      firebase
        .storage()
        .ref(parsedPath)
        .getDownloadURL()
        .then(function (url) {
          unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, url);
        })
        .catch(function (error) {
          unityInstance.Module.SendMessage(
            parsedObjectName,
            parsedFallback,
            JSON.stringify(error, Object.getOwnPropertyNames(error))
          );
        });
    } catch (error) {
      unityInstance.Module.SendMessage(
        parsedObjectName,
        parsedFallback,
        JSON.stringify(error, Object.getOwnPropertyNames(error))
      );
    }
  },

  // New function to delete a file in Firebase Storage
  DeleteFile: function (path, objectName, callback, fallback) {
    var parsedPath = Pointer_stringify(path);
    var parsedObjectName = Pointer_stringify(objectName);
    var parsedCallback = Pointer_stringify(callback);
    var parsedFallback = Pointer_stringify(fallback);

    try {
      firebase
        .storage()
        .ref(parsedPath)
        .delete()
        .then(function () {
          unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "File deleted successfully.");
        })
        .catch(function (error) {
          unityInstance.Module.SendMessage(
            parsedObjectName,
            parsedFallback,
            JSON.stringify(error, Object.getOwnPropertyNames(error))
          );
        });
    } catch (error) {
      unityInstance.Module.SendMessage(
        parsedObjectName,
        parsedFallback,
        JSON.stringify(error, Object.getOwnPropertyNames(error))
      );
    }
  }
});
