package com.janwillemboer.ema.dropbox;

import java.io.File;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import com.dropbox.client2.DropboxAPI.Entry;

public class SyncFileInfo {

	private String mName;
	private JSONObject mSyncInfo;
	private JSONObject mSyncMetadata;

	private final static String JSON_NAME = "name";
	
	public SyncFileInfo(File f, JSONObject syncMetadata, File localDir) {
		mSyncMetadata = syncMetadata;
		mName = f.getName();

		// find the sync info of this file
		try {
			JSONArray files = mSyncMetadata.getJSONArray("files");
			for (int i = 0; i < files.length(); i++) {
				JSONObject file = files.getJSONObject(i);
				if (file.getString(JSON_NAME).equalsIgnoreCase(mName)) {
					mSyncInfo = file;
					break;
				}
			}
		} catch (JSONException e) {
		}

		setLocalFile(new File(localDir, mName));
	}

	private File mLocalFile;
	private long mLocalChangeDate;
	private long mLocalRevisionSyncedDate;
	private String mLocalRevision;
	
	private final static String LOCAL_REVISION_SYNCED_DATE = "localRevisionSynced";
	private final static String LOCAL_REVISION = "localRevision";

	private void setLocalFile(File value) {
		mLocalFile = value;
		if (value.exists()) {
			mLocalChangeDate = mLocalFile.lastModified();

			if (mSyncInfo != null) {
				mLocalRevisionSyncedDate = mSyncInfo.optLong(LOCAL_REVISION_SYNCED_DATE);
				mLocalRevision = mSyncInfo.optString(LOCAL_REVISION);
			}
		}
	}

	private String mRemotePath;
	private String mRemoteRevision;

	public void setRemoteFile(Entry value) {
		mRemotePath = value.path;
		mRemoteRevision = value.rev;
	}

	public JSONObject getJSON() {
		JSONObject json = new JSONObject();
		try {
			json.put(JSON_NAME, mName);
			json.put(LOCAL_REVISION, mLocalRevision);
			json.put(LOCAL_REVISION_SYNCED_DATE, mLocalRevisionSyncedDate);
		} catch (JSONException e) {
			throw new RuntimeException(e);
		}
		return json;
	}

	public String getName() {
		return mName;
	}

	public File getFile() {
		return mLocalFile;
	}

	public String getRemotePath() {
		return mRemotePath;
	}

	public boolean needsDownload() {
		if (mLocalFile == null || !mLocalFile.exists()) {
			return true;
		}
		if (mRemotePath == null || mRemotePath.length() == 0) {
			return false;
		}
		
		boolean retval = (!mRemoteRevision.equals(mLocalRevision));
		return retval;
	}

	public boolean needsUpload() {
		if (mRemotePath == null || mRemotePath.length() == 0) {
			return true;
		}
		if (mLocalFile == null || !mLocalFile.exists()) {
			return false;
		}

		if (!mRemoteRevision.equals(mLocalRevision)) {
			return false;
		}
		
		boolean retval = (mLocalChangeDate > mLocalRevisionSyncedDate);
		return retval;
	}

	public void setLocalVersionTo(String rev) {
		mLocalRevision = rev;
	}

	public void updatedLocal() {
		mLocalFile = new File(mLocalFile.getAbsolutePath());
		mLocalRevision = mRemoteRevision;
		mLocalChangeDate = mLocalFile.lastModified();
		mLocalRevisionSyncedDate = mLocalChangeDate;
	}
	
	public void updatedRemote(Entry e) {
		setRemoteFile(e);
		updatedLocal();
	}
}

