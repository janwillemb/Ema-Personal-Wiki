package com.janwillemboer.ema;

public class ViewPageCommand {

	public ViewPageCommand(String name) {
		PageName = name;
	}

	public ViewPageCommand(String name, int totalHeight, int scrollPos) {
		this(name);
		setScrollPos(totalHeight, scrollPos);
	}

	public ViewPageCommand(String name, double scrollPos) {
		this(name);
		RelativeScrollPosition = scrollPos;
	}

	public String PageName;
	public double RelativeScrollPosition;

	public int getScrollPos(int height) {
		return (int) (height * RelativeScrollPosition);
	}

	public void setScrollPos(int height, int pos) {
		if (height == 0) {
			RelativeScrollPosition = 0;
			return;
		}
		RelativeScrollPosition = ((double) pos) / ((double) height);
	}
}
