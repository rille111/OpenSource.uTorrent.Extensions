# Rille.uTorrent.Extensions

Post processing to uTorrent/Bittorrent

__The code is not elegant, since this is a hobby project that took too much time I just need to finish functionality and therefore code quality is suboptimal. But, the code is free for you to refactor. :) .__

## What it does 

Post processing of Torrents can never be easier. This is a tool that copies & unpacks finished torrents,
restarting torrents that stopped due to errors, and it deletes torrents when they're unpacked and seeding goal finishes.
It keeps tracks of torrents using the label, so DONT USE labels for anything else or it wont work.

_Use at your own risk._

## Features 

* Restart torrents in Error status (example, when a NAS reboots you get a lot of errors)
* Copies non-archives and unpacks archives to a specified folder
* Supports only 7zip for now
* You can have different seeding goals set on different trackers (regex will match trackers)
* Behaviour is configured in the config.json file. You MUST edit this file!
* Output log to trace errors (c:\log) by default, can be changed in App.config (not config.json!)
* One exe that has no console (*Hidden.exe) and one for a visible console. Your choice!
* If you only want to process a folder (not using Bittorrent) you can do this by changing the operating mode (Untested in this version! Dangerous!)
* Process either one torrent using an argument (torrent hash) or process all by not providing any argument

## Installation & Usage 

* Unzip \releases\xxxx.zip to some folder
* Configure BitTorrent/Utorrent:
	* Preferences - Advanced - Web UI: Enable it, set login & pass, set port
	* Prererences - Advanced - Filter on "token" set 'auth' to false and 'auth_filter' to 0 (otherwise integration will fail)
* Edit config.json and update all settings to what you want
* Clear all labels for all torrents (this program relies on it!)
* Call the chosen .exe file manually or
* Call the chosen .exe file in BitTorrent/UTorrent whenever a torrent changes state (might clog the calling of the file), see below
* Call the chosen .exe every 10 minute, by creating a Task in Task Schedule (use the exported .xml, edit it first) to loop through all torrents

### Call from BitTorrent whenever one torrent changes state

You can call the executables and pass in a torrent hash as an argument in order to 
tell the program to only process one torrent instead of the whole list.
This is suitable for the BitTorrent client to do, whereas a Task will loop through all torrents.

* Preferences - Advanced - Run a program
	* Run this program when a torrent changes state, write this:
	* _C:\Programs\Rille.uTorrent.Extensions\Rille.uTorrent.Extensions.PostProcess.Hidden.exe "%I"_

## Future & TODO

* I'm using it so I'll be updating it when need be. For example the torrent-change-state calling is spamming.
* There may be bugs but it works for me, feel free to report or merge request :-)
