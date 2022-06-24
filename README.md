<div id="top"></div>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/Bonsai-Desk/BonsaiDesk">
    <img src="images/logo.png" alt="Logo" width="80" height="80">
  </a>

<h3 align="center">Bonsai Desk</h3>

  <p align="center">
    Found a video that your friend would enjoy? Meet up in Bonsai Desk and watch it together. Build together with blocks using hand tracked physics while your video is playing.
    <br />
    <a href="https://www.oculus.com/experiences/quest/2311379172319221/"><strong>Download »</strong></a>
    <br />
    <br />
    <a href="https://www.oculus.com/experiences/quest/2311379172319221/">View Demo</a>
    ·
    <a href="https://github.com/Bonsai-Desk/BonsaiDesk/issues">Report Bug</a>
    ·
    <a href="https://github.com/Bonsai-Desk/BonsaiDesk/issues">Request Feature</a>
  </p>
</div>

[![Product Name Screen Shot][product-screenshot]](https://www.oculus.com/experiences/quest/2311379172319221/)

## Getting Started

Bonsai Desk uses a web browser made by [Vuplex](https://assetstore.unity.com/publishers/40309) for most of the UI. This asset is paid, and required for Bonsai Desk to work currently. It may be possible to strip out the web browser from Bonsai Desk so it can run without this asset.

Bonsai Desk also uses custom servers and third party services for matchmaking, multiplayer, and voice chat which may or may not still be operational by the time you are reading this.

During the setup process, agree to any popups from Unity asking to update or upgrade packages (spatializer plugin, etc.) and any popups asking to restart Unity.

1. Clone the repo
   ```sh
   git clone https://github.com/Bonsai-Desk/BonsaiDesk
   ```
2. Open Bonsai Desk using Unity 2019.4.25f1
   - Set the platform to Android in Unity Hub before you open the project for the first time so you do not have to import assets twice
3. Add [Oculus Integration SDK v23.1](https://developer.oculus.com/downloads/package/unity-integration-archive/23.1/) to the project
    - When importing the oculus sdk, you can leave out the Avatar and LipSync folder since they are not using and will make importing take longer
4. Add [Vivox SDK v5.14.1](https://developer.vivox.com/downloads/link/d5402431b89313eb326c55045b407a73) to the project
    - You will need to [create a free vivox account](https://developer.vivox.com/register) to download this
5. Add [3D WebView for Android with Gecko Engine (Web Browser)](https://assetstore.unity.com/packages/tools/gui/3d-webview-for-android-with-gecko-engine-web-browser-158778) to the project
6. (optional) Add [3D WebView for Windows and macOS (Web Browser)](https://assetstore.unity.com/packages/tools/gui/3d-webview-for-windows-and-macos-web-browser-154144) to the project
    - This step is only necessary if you want the UI to work in the editor
7. Connect an Oculus headset to the computer, activate Oculus Link, then press play in Unity
    - If you stay in your Oculus home after pressing play in Unity, try restarting Unity while keeping Oculus link connected, then press play again

## Contributing

Bonsai Desk is no longer being actively developed, but contributions are welcome

## License

Distributed under the MIT License. See `LICENSE` for more information.

## Contact

Join the [Discord server](https://discord.com/invite/K3jMY7nv9k)

[contributors-shield]: https://img.shields.io/github/contributors/Bonsai-Desk/BonsaiDesk.svg?style=for-the-badge
[contributors-url]: https://github.com/Bonsai-Desk/BonsaiDesk/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Bonsai-Desk/BonsaiDesk.svg?style=for-the-badge
[forks-url]: https://github.com/Bonsai-Desk/BonsaiDesk/network/members
[stars-shield]: https://img.shields.io/github/stars/Bonsai-Desk/BonsaiDesk.svg?style=for-the-badge
[stars-url]: https://github.com/Bonsai-Desk/BonsaiDesk/stargazers
[issues-shield]: https://img.shields.io/github/issues/Bonsai-Desk/BonsaiDesk.svg?style=for-the-badge
[issues-url]: https://github.com/Bonsai-Desk/BonsaiDesk/issues
[license-shield]: https://img.shields.io/github/license/Bonsai-Desk/BonsaiDesk.svg?style=for-the-badge
[license-url]: https://github.com/Bonsai-Desk/BonsaiDesk/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/linkedin_username
[product-screenshot]: images/screenshot.jpg