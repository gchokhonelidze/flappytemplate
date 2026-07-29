var DeviceInfoLib = {
	// Returns 1 for touch-first devices (phones, tablets), 0 for pointer devices.
	//
	// Deliberately not a user-agent test first: iPadOS reports itself as desktop Safari,
	// so UA sniffing classifies an iPad Pro as a PC and hands it 4x MSAA. The CSS media
	// features describe the actual input hardware and are what the platform guarantees.
	// UA is kept only as a fallback for browsers without matchMedia.
	IsMobileBrowserJS: function () {
		try {
			if (window.matchMedia) {
				// coarse pointer = finger; no hover = no mouse cursor to hover with.
				if (window.matchMedia('(pointer: coarse)').matches) return 1;
				if (window.matchMedia('(hover: none)').matches) return 1;
				return 0;
			}
			return /iPhone|iPad|iPod|Android|Mobile/i.test(navigator.userAgent) ? 1 : 0;
		} catch (e) {
			// Unknown - assume the cheaper path rather than risk a slideshow on mobile.
			return 1;
		}
	},
};

mergeInto(LibraryManager.library, DeviceInfoLib);
