= Rille.uTorrent.Extensions

Post processing to uTorrent/Bittorrent

== What it does 

Post processing of Torrents can never be easier. This is a tool that copies & unpacks finished torrents,
restarting torrents that stopped due to errors, and it deletes torrents when they're unpacked and seeding goal finishes.
It keeps tracks of torrents using the label, so DONT USE labels for anything else or it wont work.

Use at own risk. 

== Features 

* Restart torrents in Error status (example, when a NAS reboots you get a lot of errors)
* Copies non-archives and unpacks archives to a specified folder
* Supports only 7zip for now
* Behaviour is configured in the config.json file. You MUST edit this file!
* Output log to trace errors (c:\log) by default, can be changed in App.config (not config.json!)
* One exe that has no console (*Hidden.exe) and one for a visible console. Your choice!
* If you only want to process a folder (not using Bittorrent) you can do this by changing the operating mode

== Installation & Usage 

* Unzip \releases\xxxx.zip to some folder
* Configure BitTorrent/Utorrent:
	* Preferences - Advanced - Web UI: Enable it, set login & pass, set port
	* Prererences - Advanced - Filter on "token" set 'auth' to false and 'auth_filter' to 0 (otherwise integration will fail)
* Edit config.json and update all settings to what you want
* Clear all labels for all torrents (this program relies on it!)
* Call the .exe file manually or
* Call the .exe file in BitTorrent/UTorrent whenever a torrent changes state (might clog the calling of the file)
* Call the .exe every 10 minute, by creating a Task in Task Schedule (use the exported .xml, edit it first)

Note: This installation package keeps the window hidden so you won't see anything!

== Future & TODO

* I'm using it so I'll be updating it when need be. For example the torrent-change-state calling is spamming.
* There may be bugs but it works for me, feel free to report or merge request :-)