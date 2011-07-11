package com.janwillemboer.ema;

public class JavascriptInterop {

	private Listener mListener;
	public JavascriptInterop(Listener l) {
		mListener= l;
	}
	public void sendMessage(String command, String parameters)  {
		mListener.sendMessage(command, parameters);
	}
	
	public interface Listener {
		public void sendMessage(String command, String parameters);
	}
}
