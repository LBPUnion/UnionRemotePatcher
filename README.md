# UnionRemotePatcher
 Patch LittleBigPlanet EBOOT files on your PS3 system remotely. WILL BE DEPRECATED SOON. This is currently a seperate app from UnionPatcher but as I implement more features I may move these changes into my own fork, but for now it's far enough out of UnionPatcher's scope that I didn't want to dilute the project with it and potentially compromise reliability.

## THIS SOFTWARE IS CURRENTLY IN TESTING!
Things can go wrong. 

## What is it?
UnionRemotePatcher is a simple """multiplatform""" app built in Eto that utilizes UnionPatcher and SCETool (which is not contained in this repository) to remotely backup and patch your LittleBigPlanet EBOOT file over the network for connecting to Project Lighthouse servers. We're aiming to make this process as simple as possible, but we still need to do lots before we're there.

## Using UnionRemotePatcher
ASSUMING you have WebManMOD installed on your CFW or HFW PlayStation 3 system -
Look for a release in the Releases tab, and download it. Open it, fill out the boxes correctly, and patch!

## TODO
- [x] Obtain required dependencies over the Internet
- [x] Connect to a PS3 console to download, upload, and check for the existance of files
- [x] Decrypt, Patch & Encrypt LittleBigPlanet PS3 EBOOT files 
- [ ] Ask for FTP authentication info
- [x] Add compatibility with digital releases of LBP
- [ ] Encrypt for different devices and CFWs (psvita, DEX, etc.) without issues (right now our eboots manage to inflate in size for some reason, I'm definitely not doing something right)
- [ ] True multiplatform support (right now we're using scetool binaries downloaded from a third-party which are only available for Windows at this time, but Eto and the app itself should play fine on other operating systems 
- [ ] Less sketch (we're obtaining important files from an internet source that we don't have much control or assurance over and we'd like to improve this
