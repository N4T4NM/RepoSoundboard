# R.E.P.O Soundboard
A builtin soundboard for R.E.P.O

### WARNING
Right now, this project is more of a proof of concept, but I'll continue to work on it to improve performance and fix bugs.

### ISSUES
- Only **.wav** files supported
- Resampler code has performance issues
- The loopback audio has a delay
- Only works on singleplayer

It is recommended that you use a tool like ffmpeg to generate a **mono** audio file with a sample rate of **48 Khz**.

To do that with ffmpeg run the command `ffmpeg -i <audio file> -ac 1 -ar 48000 <output wav file>`

### Usage
You can manage the sounds by opening the in-game settings menu and clicking on **`SOUNDBOARD`**