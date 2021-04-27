package com.example.bonsai;

import android.content.res.AssetManager;
import android.util.Log;

import java.io.IOException;
import java.util.Arrays;
import java.util.Map;

import com.unity3d.player.UnityPlayer;

import fi.iki.elonen.NanoHTTPD;

public class NanoServer extends NanoHTTPD {
    public NanoServer() throws IOException {
        super(9696);
        start(NanoHTTPD.SOCKET_READ_TIMEOUT, false);
        Log.i("Bonsai", "Running server now at http://localhost:9696/ \\n");
    }
    public static void main(){
        try {
            new NanoServer();
        } catch (IOException ioe) {
            Log.e("Bonsai", "Could not start server: \n" + ioe);
        }
    }
    @Override
    public Response serve(IHTTPSession session) {
        String msg = "<html><body><h1>Hello server</h1>\n";
        Map<String, String> parms = session.getParms();
        if (parms.get("username") == null) {
            msg += "<form action='?' method='get'>\n  <p>Your name: <input type='text' name='username'></p>\n" + "</form>\n";
        } else {
            msg += "<p>Hello, " + parms.get("username") + "!</p>";
        }
        return newFixedLengthResponse(msg + "</body></html>\n");
    }
}
