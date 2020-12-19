Dissonance Mirror Integration
=============================

This package integrates Dissonance Voice Chat (https://assetstore.unity.com/packages/tools/audio/dissonance-voice-chat-70078?aid=1100lJDF) with the Mirror (https://assetstore.unity.com/packages/tools/network/mirror-129321?aid=1100lJDF) networking system. To use this package you must have purchased and installed Dissonance and Mirror - **you will encounter build errors until you have done this**!

By default Mirror uses the "Telepathy" network backend, this is unsuitable for realtime voice chat because it is TCP based. To use Dissonance with Mirror you should use a UDP based Mirror backend such as Ignorance (https://github.com/SoftwareGuy/Ignorance) or LiteNetLib4Mirror (https://github.com/MichalPetryka/LiteNetLib4Mirror).

The included demo scene (Assets/Dissonance/Integrations/MirrorIgnorance/Demo/MirrorIgnorance Demo) demonstrates the basics of a voice chat session running in a Mirror network session (using Telepathy to keep the demo simple). Consult the readme file in the demo folder for details on how to run the demo and what features it demonstrates.



Documentation
=============

Dissonance includes detailed documentation on how to properly install and use Dissonance, you can find this
documentation online at:

	https://placeholder-software.co.uk/Dissonance/docs

There is a compressed copy of this documentation included in the package for offline access. You can find this at:

	Assets/Dissonance/Offline Documentation~.zip

Extract the archive and open `index.html` to get started



Project Setup
=============

Because Dissonance is a realtime communication system you must set your project to run even when it does not have
focus. To do this go to:

	Edit -> Project Settings -> Player
	
Check the `Run In Background` box in the inspector.



Further Support
===============

If you encounter a bug or want to make a feature request please open an issue on the issue tracker:

	https://github.com/Placeholder-Software/Dissonance/issues

If you have any other questions ask on the discussion forum:

	https://www.reddit.com/r/dissonance_voip/
	
Or send us an email:

	mailto://admin@placeholder-software.co.uk