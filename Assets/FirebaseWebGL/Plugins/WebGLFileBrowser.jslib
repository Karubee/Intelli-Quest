mergeInto(LibraryManager.library, {
  OpenFileDialog: function () {
    var input = document.createElement("input");
    input.type = "file";
    input.accept = "application/pdf"; // Only accept PDF files

    input.onchange = function (event) {
      var file = event.target.files[0];
      if (file) {
        var reader = new FileReader();
        reader.onload = function (e) {
          var data = e.target.result;
          var base64Data = btoa(data);
          var fileName = file.name; // Get the file name

          // Create an object with file name and base64 data
          var fileInfo = {
            name: fileName,
            data: base64Data
          };

          // Convert the object to JSON string
          var fileInfoString = JSON.stringify(fileInfo);

          // Hardcoded GameObject name for SendMessage
          if (window.unityInstance) {
            window.unityInstance.SendMessage(
              "PdfUploader", // Hardcoded GameObject name
              "OnFileSelected",
              fileInfoString // Send the file info as JSON string
            );
          } else {
            console.error("Unity instance is not available.");
          }
        };
        reader.readAsBinaryString(file); // Read file as binary string
      }
    };

    input.click(); // Simulate file input click
  },
});
