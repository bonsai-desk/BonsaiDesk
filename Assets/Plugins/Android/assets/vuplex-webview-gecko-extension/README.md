This folder is part of Vuplex WebView for Android (Gecko)
and is automatically copied to this location
from Assets/Vuplex/WebView/Plugins/AndroidGecko/assets/
to ensure that Unity includes it in the compiled
APK's assets. Since this directory is copied automatically,
you can omit it from version control by adding the following
rule to your .gitignore file:

```
# This gets automatically gets copied to this location by the AndroidGeckoBuildScript.
Assets/Plugins/Android/assets/vuplex-webview-gecko-extension*
```
