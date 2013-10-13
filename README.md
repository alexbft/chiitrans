# Chiitrans Lite
Chiitrans Lite is an automatic translation tool for Japanese visual novels. It extracts, parses and translates text into English on the fly.

Chiitrans Lite is the successor of the project [Chiitrans](http://code.google.com/p/chiitrans/).

Visit http://alexbft.github.io/chiitrans/ for more info.

# Differences between Chiitrans and Chiitrans Lite
## New features
* Using modified [ITH](http://code.google.com/p/interactive-text-hooker/) engine for text extraction
* That means support for multiple user hooks (AGTH codes)
* Japanese proper names dictionary ([JMnedict](http://www.csse.monash.edu.au/~jwb/enamdict_doc.html)) support
* Improved parsing algorithm
* Lots of usability improvements

## Removed features
* All online translation services removed. ATLAS is the only supported translation software.
* That means no more horrible Jp->En->Ru PROMT translations, too
* Parsing: MeCab support
* Parsing: WWWJDIC support
* No arbitrary text replacements
* No user dictionary words (except names)
* No crowdsourced translations
* "Fullscreen" mode

# System requirements
## Minimal
* Windows XP or later
* .NET Framework 4
* Internet Explorer 8+
* 500 MB of free memory

## Recommended
* Windows 7 or later
* .NET Framework 4
* Internet Explorer 10+
* 500 MB of free memory
* [ATLAS V14](http://www.fujitsu.com/global/services/software/translation/atlas/index.html) installed

# Other project mentions
* [JMDict](http://www.csse.monash.edu.au/~jwb/jmdict.html) - Japanese-English dictionary used in Chiitrans Lite
* [Translation Aggregator](http://www.hongfire.com/forum/showthread.php/94395-Translation-Aggregator-v0-4-9) - Insight for ATLAS integration, conjugations list, and more. Many thanks to author!
* [AGTH](https://sites.google.com/site/agthook/) - A text extraction tool used in the previous version of Chiitrans. In this version, AGTH is only needed for launching programs using AppLocale without annoying dialog boxes.
* [ITH](http://code.google.com/p/interactive-text-hooker/) - Original text extraction engine
* [Visual Nover Reader](https://code.google.com/p/annot-player/) - Modified text extraction engine (vnr\*.dll). The project is huge, I have used only these dll files from this project.
* [Locale Emulator](https://github.com/xupefei/Locale-Emulator) - An AppLocale alternative

# License
    Copyright 2013 alexbft

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this software except in compliance with the License.
    You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.