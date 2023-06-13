## FFBitrateViewer — yet another program for video file bitrate visualization

FFBitrateViewer is a FFProbe GUI that purpose is to visualize frames` bitrate extracted by FFProbe.
The program allows you to select multiple files without dealing with command line and get frames bitrate for all of them in one go.

Well, and build shiny interactive graphs of course:
<p align="center"><img src="screenshots/screenshot.0.8.0.png" width="900"/></p>


## Features
- Processing up to 12 files in one go;
- Brief media info for files;
- Easy to use UI: drag & drop files from Explorer onto files list or use file chooser;
- Graphs can be zoomed in/out with mouse wheel (try it over graph or graph's axes), panned with right mouse button and saved as SVG or PNG;
- FFProbe commands issued by FFBitrateViewer can be saved to log file (`FFBitrateViewer.log`);
- Free and **Open Source**. No registration, banners, tracking etc.


## Latest version: 
- Latest Beta: [0.8.0 beta 1](https://github.com/fifonik/FFBitrateViewer/releases/tag/v0.8.0-beta.1)


## Requirements
- Windows OS;
- .NET 7.0 or later. The program should ask you to download and install it if required.
- FFProbe.exe (a part of FFMpeg package). You have to download it from [official ffmpeg web site](https://ffmpeg.org/download.html);
  You can try static build for simplicity, however, for real usage I'd recommend to use shared build.


## How to use
- Unpack into a folder;
- Put FFProbe.exe (and accompanied dll files if you use shared build) into the program folder or make it available through system %PATH%;
- Run the program;
- Use UI to add files (you can drag & drop files from Explorer or use file choosers);
- Click “Start” button.


## Troubleshooting
- Close FFBitrateViewer and delete `FFBitrateViewer.log`;
- Run the program with option `-log-level=debug`;
- Add file;
- Click “Start” button;
- Take screenshot (Alt+PrnScr or Win+Shift+S and paste it into image editor and save as PNG);
- Close the program;
- Analyze `FFBitrateViewer.log`. You can try to run the ffprobe command directly;
- Upload archived `FFBitrateViewer.log` with screenshot to dropbox (or similar) and share the link.


## Author
fifonik
