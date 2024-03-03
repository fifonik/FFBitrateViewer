## FFBitrateViewer — yet another program for video file bitrate visualization

FFBitrateViewer is a FFProbe GUI that purpose is to visualize frames` bitrate extracted by FFProbe.
It is inspired by [Bitrate Viewer](https://web.archive.org/web/20160730053853/http://www.winhoros.de/docs/bitrate-viewer/) (link to Web Archive as the program's web-site and forum are long dead).
FFBitrateViewer allows you to select multiple files without dealing with command line and get “per frame” or “per second” info for all of them in one go.

Well, and play with interactive graphs (powered by OxyPlot):
<p align="center"><img src="screenshots/screenshot.0.8.0.png" width="900"/></p>


## Features
- Processing up to 12 files in one go;
- Brief media info for all files (hover mouse over media info to see more details);
- Easy to use UI: files can be added with file chooser or dropped from Windows Explorer, files can be re-ordered using Drag & Drop;
- Graphs can be zoomed in/out with mouse wheel (try it over graph and/or axes), panned with right mouse button and saved as SVG or PNG;
- FFProbe commands issued by FFBitrateViewer can be saved to log file (`FFBitrateViewer.log`);
- No registration, banners, tracking etc;
- Free;
- **Open Source** ([MIT License](LICENSE.txt)).


## Latest version: 
- Latest Beta: [0.8.0 beta 2](https://github.com/fifonik/FFBitrateViewer/releases/tag/v0.8.0-beta.2)


## Requirements
- Windows OS
- .NET 7.0 or later. The program should ask you to download and install it if required.
- FFProbe.exe (a part of FFMpeg package). You have to download it from [official ffmpeg web site](https://ffmpeg.org/download.html).
  You can use sibgle file static build for simplicity, however, for real usage I'd recommend to make shared build accessible in %PATH%.


## How to use
- Unpack into a folder;
- Put FFProbe.exe (and accompanied dll files if you use shared build) into the program folder or make it available through system %PATH%;
- Run the program;
- Use UI to add files;
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

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/fifonik)
