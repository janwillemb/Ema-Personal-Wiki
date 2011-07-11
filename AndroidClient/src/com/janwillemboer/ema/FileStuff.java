package com.janwillemboer.ema;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;

public class FileStuff {

	public static final String ENCODING = "UTF-8";
	public static final int BUFFER_SIZE = 8192;

	public static void writeContents(File f, String contents)
			throws IOException {
		BufferedWriter bw = null;
		try {
			FileOutputStream fos = new FileOutputStream(f);
			OutputStreamWriter osw = new OutputStreamWriter(fos, ENCODING);
			bw = new BufferedWriter(osw, BUFFER_SIZE);
			bw.write(contents);
		} finally {
			if (bw != null) {
				try {
					bw.close();
				} catch (IOException e) {
				}
				bw = null;
			}
		}
	}
	
	public static String readContents(File f) throws IOException {
		BufferedReader br = null;
		try {
			FileInputStream fis = new FileInputStream(f);
			InputStreamReader isr = new InputStreamReader(fis, ENCODING);

			br = new BufferedReader(isr, BUFFER_SIZE);

			StringBuilder sb = new StringBuilder();
			while (true) {
				String line = br.readLine();
				if (line == null) {
					break;
				}
				sb.append(line);
				sb.append("\n");
			}

			return sb.toString();
		} finally {
			if (br != null) {
				try {
					br.close();
				} catch (IOException e) {
				}

				br = null;
			}
		}
		
	}
}
