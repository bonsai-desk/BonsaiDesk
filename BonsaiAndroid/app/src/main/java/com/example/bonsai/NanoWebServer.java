package com.example.bonsai;

import android.content.res.AssetManager;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.util.Timer;
import java.util.TimerTask;

import fi.iki.elonen.SimpleWebServer;

public class NanoWebServer extends SimpleWebServer {
    boolean ready = false;

    public NanoWebServer(String host, int port, File wwwroot, boolean quiet) throws IOException {
        super(host, port, wwwroot, quiet);
        start();
        ready = true;
        Log.i("Bonsai", String.format("Running server now at %s:%d/", host, port));
    }

    private static void copyRecursive(AssetManager assetManager, String root, String assetPath) {
        String filePath = root + "/" + assetPath;
        String[] buildFiles = null;
        try {
            buildFiles = assetManager.list(assetPath);
        } catch (IOException eio) {
            Log.e("Bonsai", "Failure on path: " + assetPath);
            Log.e("Bonsai", "IOException: " + eio);
        }

        if (buildFiles != null) {
            if (buildFiles.length == 0) {
                // it's a file
                try {
                    InputStream inputStream = assetManager.open(assetPath);
                    CopyFile(inputStream, filePath);
                } catch (IOException eio) {
                    Log.e("Bonsai", "Failure open: " + filePath);
                    Log.e("Bonsai", "IOException: " + eio);
                }
            } else {
                // it's a folder
                File dir = new File(filePath);
                boolean success = dir.mkdir();
                if (success) {
                    for (String buildFile : buildFiles) {
                        copyRecursive(assetManager, root, assetPath + "/" + buildFile);
                    }
                } else {
                    Log.e("Bonsai", "Failed to make dir: " + filePath);
                }
            }
        }
    }

    static void deleteDir(File file) {
        File[] contents = file.listFiles();
        if (contents != null) {
            for (File f : contents) {
                if (!Files.isSymbolicLink(f.toPath())) {
                    deleteDir(f);
                }
            }
        }
        file.delete();
    }

    public static NanoWebServer main(String htmlFolderName) {
        File filesDir = UnityPlayer.currentActivity.getFilesDir();
        AssetManager assetManager = UnityPlayer.currentActivity.getAssets();

        String bonsaiFolderPath = filesDir + "/bonsai";
        File bonsaiFolder = new File(bonsaiFolderPath);

        if (bonsaiFolder.exists()) {
            deleteDir(bonsaiFolder);
        }

        if (!bonsaiFolder.exists()) {
            bonsaiFolder.mkdirs();
        }

        copyRecursive(assetManager, bonsaiFolderPath, htmlFolderName);

        File htmlFolder = new File(bonsaiFolderPath + "/" + htmlFolderName);

        try {
            return new NanoWebServer(null, 9696, htmlFolder, false);
        } catch (IOException ioe) {
            Log.e("Bonsai", "Could not start server: \n" + ioe);
        }
        return null;
    }

    public void ShutDown() {
        Log.i("Bonsai", "Shutting down web server");
        stop();
    }

    private static void CopyFile(InputStream inputStream, String filePath) {
        try {
            Files.copy(inputStream, Paths.get(filePath), StandardCopyOption.REPLACE_EXISTING);
        } catch (IOException eio) {
            Log.e("Bonsai", eio.toString());
        }
    }

    public void Pause() {
        if (ready) {
            Log.i("Bonsai", "Stop web-server");
            stop();
        } else {
            Log.w("Bonsai", "Trying to pause server when not ready");
        }
    }

    public void Resume() {
        try {
            if (ready) {
                Log.i("Bonsai", "Start web-server");
                start();
            } else {
                Log.w("Bonsai", "Tried to resume when not ready");
            }
        } catch (IOException ioe) {
            Log.e("Bonsai", ioe.toString());
        }
    }
}

