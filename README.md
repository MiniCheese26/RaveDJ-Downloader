# RaveDJ-Downloader

![Image](https://i.imgur.com/8rnRwTv.png "Usage")

### Downloads videos from Rave.dj

### Usage: raveDjDownloader.exe \<URL1\> \<URL2\> \<URL3\> ...

* Downloads videos concurrently to speed up downloading
* Edit config.json to set the number of concurrent downloads, max is 5 and the a default save location
* By default the application creates a folder called Downloads in the application directory

### Dependencies

* Json.NET
* Konsole
* .Net Core 3.0

### Tampermonkey Script
#### This will add a button to all mashups allowing for easy access to downloading

```
// ==UserScript==
// @name         RaveDJ Downloader
// @namespace    http://tampermonkey.net/
// @version      0.1
// @description  Adds a download link the any rave.dj video
// @author       Loc
// @match        *://rave.dj/*
// @grant        GM_addStyle
// ==/UserScript==

window.addEventListener('load', waitForPlayer(addDownloadButton), false);

function addDownloadButton() {
    const videoUrl = document.getElementById("ForegroundPlayer_html5_api").src;
    const onclickCode = `window.open('${videoUrl}','_blank')`;

    const buttonNode = document.createElement('div');
    buttonNode.innerHTML = `<button id="download-button" type="button" onclick="${onclickCode}">Download Mashup</button>`;
    buttonNode.setAttribute('id', 'download-button-container');
    document.body.appendChild(buttonNode);
}

function waitForPlayer(callBack) {
    window.setTimeout(function () {
        const player = document.getElementById("ForegroundPlayer_html5_api");
        if (player && player.src) {
            callBack();
        } else {
         waitForPlayer(callBack);
        }
    }, 500)
}

// ty stackoverflow

GM_addStyle(`
#download-button-container {
position:               absolute;
top:                    0;
font-size:              20px;
background:             #333;
margin-top:             3.5rem;
margin-left:            1.0rem;
opacity:                0.9;
z-index:                1100;
padding:                5px 20px;
}
` );
```
