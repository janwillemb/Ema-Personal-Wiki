package com.janwillemboer.ema;

import java.io.File;
import java.io.FileNotFoundException;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.webkit.MimeTypeMap;

public class LocalFileContentProvider extends ContentProvider {
   public static final String URI_PREFIX = "content://com.janwillemboer.ema.localfile/";
   
   @Override
   public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
	   //URI localUri = URI.create("file://" + );
       File file = new File(new EmaActivityHelper(getContext()).getDal().Dir() + uri.getPath());

       ParcelFileDescriptor parcel = ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
       return parcel;
   }

   @Override
   public boolean onCreate() {
       return true;
   }

   @Override
   public int delete(Uri uri, String s, String[] as) {
       throw new UnsupportedOperationException();
   }

   @Override
   public String getType(Uri uri) {
	   String ext = MimeTypeMap.getFileExtensionFromUrl(uri.toString());
	   String type = MimeTypeMap.getSingleton().getMimeTypeFromExtension(ext.toLowerCase()); 
	   return type;
   }

   @Override
   public Uri insert(Uri uri, ContentValues contentvalues) {
       throw new UnsupportedOperationException();
   }

   @Override
   public Cursor query(Uri uri, String[] as, String s, String[] as1, String s1) {
       throw new UnsupportedOperationException();
   }

   @Override
   public int update(Uri uri, ContentValues contentvalues, String s, String[] as) {
       throw new UnsupportedOperationException();
   }

}