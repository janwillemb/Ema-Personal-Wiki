package com.janwillemboer.ema.dropbox;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileOutputStream;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.io.OutputStream;
import java.util.HashMap;
import java.util.Map;

import org.apache.http.HttpEntity;
import org.apache.http.HttpResponse;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import android.os.Handler;
import android.os.Message;

import com.dropbox.client.DropboxAPI;
import com.dropbox.client.DropboxAPI.Entry;
import com.dropbox.client.DropboxClient;
import com.dropbox.client.DropboxException;
import com.janwillemboer.ema.EmaActivityHelper;
import com.janwillemboer.ema.R;
import com.janwillemboer.ema.WikiPage;

public class Sync {

	private final static String METADATA_FILE = "sync-metadata.json";

	private DropboxAPI mApi;
	private DropboxClient mClient;

	private EmaActivityHelper mHelper;
	private File mLocalDir;
	private Handler mProgressUpdateHandler;
	private JSONObject mSyncMetadata;

	public Sync(EmaActivityHelper helper, DropboxAPI api, DropboxClient client) {
		mApi = api;
		mClient = client;
		mHelper = helper;

		mLocalDir = mHelper.getDal().Dir();

		File syncMetadataFile = new File(mLocalDir, METADATA_FILE);

		if (!syncMetadataFile.exists()) {
			mSyncMetadata = new JSONObject();
			return;
		}

		BufferedReader br = null;
		try {
			br = new BufferedReader(new FileReader(syncMetadataFile), 4096);
			char[] buffer = new char[(int) syncMetadataFile.length()];
			br.read(buffer);

			mSyncMetadata = new JSONObject(new String(buffer));
		} catch (Exception e) {
			throw new RuntimeException(e);
		} finally {
			if (br != null) {
				try {
					br.close();
				} catch (Exception e) {
				}
			}
		}

	}

	public void setStatusHandler(Handler progressUpdate) {
		mProgressUpdateHandler = progressUpdate;
	}

	public boolean perform() throws IOException {
		Entry syncDir = mApi.metadata("dropbox", "/PersonalWiki", 1000, null,
				true);

		if (!syncDir.is_dir) {
			syncDir = mApi.createFolder("dropbox", "/PersonalWiki");
		}

		showProgress(1, 0);

		// create a list of all local files
		Map<String, SyncFileInfo> files = new HashMap<String, SyncFileInfo>();
		for (WikiPage p : mHelper.getDal().fetchAll()) {
			SyncFileInfo fi = new SyncFileInfo(p.getFile(), mSyncMetadata,
					mLocalDir);
			files.put(fi.getName(), fi);
		}
		// add info for remote files
		for (Entry f : syncDir.contents) {
			if (!f.is_dir) {
				SyncFileInfo fi;
				if (files.containsKey(f.fileName())) {
					fi = files.get(f.fileName());
				} else {
					fi = new SyncFileInfo(new File(mLocalDir, f.fileName()),
							mSyncMetadata, mLocalDir);
					files.put(fi.getName(), fi);
				}
				fi.setRemoteFile(f);
			}
		}

		int totalFiles = files.size();
		int count = 0;

		boolean syncedSomething = false;
		// do sync for all files
		for (SyncFileInfo fi : files.values()) {
			count++;
			showProgress(totalFiles, count);

			if (fi.needsUpload()) {
				mApi.putFile("dropbox", syncDir.path, fi.getFile());

				// update local sync info
				Entry syncedFile = mApi.metadata("dropbox", syncDir.path + "/"
						+ fi.getName(), 1000, null, true);
				fi.setSyncedWithRemoteDate(syncedFile.modified);
				fi.setSyncedWithLocalDate(fi.getFile().lastModified());
				syncedSomething = true;

			} else if (fi.needsDownload()) {
				
				OutputStream os = null;
				HttpEntity entity = null;
				try {
					//FileDownload fd = mApi.getFileStream("dropbox",
					//		fi.getRemotePath(), null);
					
					HttpResponse response = null;
					try {
						response = mClient.getFile("dropbox",
								fi.getRemotePath());
					} catch (DropboxException e) {
						throw new IOException(e.getLocalizedMessage());
					}
					entity = response.getEntity();
					os = new FileOutputStream(fi.getFile());
					entity.writeTo(os);

					// update local sync info
					File updatedFile = new File(fi.getFile().getAbsolutePath());
					fi.setSyncedWithLocalDate(updatedFile.lastModified());
					fi.setSyncedWithRemoteDate(fi.getRemoteChangeDate());
					syncedSomething = true;
				} finally {
					try {
						if (os != null) {
							os.close();
						}
						if (entity != null) {
							entity.consumeContent(); // closes connections
						}
					} catch (IOException e) {
					}
				}
			}
		}

		updateSyncInfo(files);
		return syncedSomething;
	}

	private void showProgress(int totalFiles, int count) {
		Message updateMessage = new Message();
		updateMessage.obj = mHelper.getContext()
				.getText(R.string.synchronizing).toString();
		updateMessage.arg1 = (int) ((((double) count) / ((double) totalFiles)) * 100);
		mProgressUpdateHandler.sendMessage(updateMessage);
	}

	private void updateSyncInfo(Map<String, SyncFileInfo> files)
			throws IOException {

		try {
			mSyncMetadata = new JSONObject();
			JSONArray filesArray = new JSONArray();

			for (SyncFileInfo file : files.values()) {
				filesArray.put(file.getJSON());
			}

			mSyncMetadata.put("files", filesArray);

			File syncMetadataFile = new File(mLocalDir, METADATA_FILE);
			BufferedWriter bw = null;
			try {
				bw = new BufferedWriter(new FileWriter(syncMetadataFile), 4096);
				bw.write(mSyncMetadata.toString(2));
			} finally {
				if (bw != null) {
					try {
						bw.close();
					} catch (Exception e) {
					}
				}
			}

		} catch (JSONException e) {
			throw new RuntimeException(e);
		}
	}
}