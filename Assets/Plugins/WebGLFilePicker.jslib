mergeInto(LibraryManager.library, {

    OpenBrowserFilePicker: function() {
        // Create a hidden HTML file uploader
        var fileInput = document.getElementById('fileUploader');
        if (!fileInput) {
            fileInput = document.createElement('input');
            fileInput.setAttribute('type', 'file');
            fileInput.setAttribute('id', 'fileUploader');
            // Only accept .glb files!
            fileInput.setAttribute('accept', '.glb, .gltf');
            fileInput.style.display = 'none';
            document.body.appendChild(fileInput);

            fileInput.onchange = function(event) {
                var file = event.target.files[0];
                if (file) {
                    // Turn the file into a temporary web URL
                    var objectUrl = URL.createObjectURL(file);
                    
                    // SEND IT TO UNITY! 
                    // Note: "BrushController" MUST be the exact name of your GameObject in Unity
                    SendMessage('BrushController', 'OnFileSelected', objectUrl + '|' + file.name);
                }
                // Reset it so they can upload again
                fileInput.value = null; 
            };
        }
        // Force the browser to click the hidden button
        fileInput.click();
    }
});