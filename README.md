= Rille.uTorrent.Extensions =
Post processing to uTorrent/Bittorrent

== What it does ==

Post processing of Torrents can never be easier. This is a tool that copies & unpacks finished torrents,
restarting torrents that stopped due to errors, and it deletes torrents when they're unpacked and seeding goal finishes.
It keeps tracks of torrents using the label, so DONT USE labels for anything else or it wont work.

== Features ==

* Copies non-archives and unpacks archives to a specified folder
* Restart torrents in Error status (example, when a NAS reboots you get a lot of errors)
* Support 7zip
* Behaviour is configured in the config.json file. You MUST edit this file!
* Output log to trace errors (c:\log) by default, can be changed in App.config (not config.json!)
* If you only want to process a folder (not using Bittorrent) you can do this by changing the operating mode

== Installation & Usage ==

* Unzip \releases\xx\package.7z to some folder
* Edit config.json and update all settings to what you want
* Configure BitTorrent/Utorrent:
	* Preferences - Advanced - Web UI: Enable it, set login & pass, set port
	* Prererences - Advanced - Filter on "token" set 'auth' to false and 'auth_filter' to 0
* Call the .exe file manually or
* Call the .exe file in BitTorrent/UTorrent whenever a torrent changes state
* Call the .exe every 10 minute, by creating a Task in Task Schedule (use the exported .xml)

Note: This installation package keeps the window hidden so you won't see anything!

== Future ==

* Better installation package
* Two executables, one for hidden operation and one for console
* There may be bugs but it works for me, feel free to report or merge request :-)