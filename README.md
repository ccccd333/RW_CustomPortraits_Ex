[![Steam Workshop|Really Custom Portraits](https://img.shields.io/steam/subscriptions/2956572955?style=for-the-badge&logo=steam&label=Really%20Custom%20Portraits&labelColor=blue
)](https://steamcommunity.com/sharedfiles/filedetails/?id=2956572955)
[![Last Update Badge](https://img.shields.io/steam/update-date/2956572955?style=for-the-badge&label=Last%20update)](https://github.com/Tea-Cup/RW_CustomPortraits/releases/latest)

Adds a portrait to the selected pawns and animals.  
**This mod does not generate images, only displays them.**

# ![How to use](https://i.postimg.cc/jqk53P2R/h-How-To-Use.png)

Portraits can be set in "Health" tab of a pawn.  
Images are loaded from "CustomPortraits" folder in RimWorld root directory.  
Something like: .../steamapps/common/RimWorld/CustomPortraits  
You can use a button in mod settings to find it quickly.  
.jpg, .jpeg, .png images are supported.  
Nested directories are supported.  
No need to restart for every file.  
Textures loaded on demand every time you open portrait dropdown.

# ![Features](https://i.postimg.cc/zBzB6kTG/h-Features.png)

- Images of any size.
- Transparency.
- Multiple portraits for each pawn.
- A lot of customization options and increasing.
- Available for animals on def-basis. (only 1.4 and 1.5 versions)

# ![Settings](https://i.postimg.cc/t4F4gc5g/h-Settings.png)

5 position modes:

- Next to Inspector - Display above inspector tabs or next to the inspector tab window.
- Colonist Bar - Display custom portrait instead of colonist render in the bar on top of the screen.
- Top Right - Just a corner of the screen.
- Actions - To the right of the inspector, right before pawn commands buttons.
- Custom - Specify coordinates and alignment yourself.

_Multiple modes can be active at once, each with it's own portrait if you want._

**Next to Inspector** mode places portrait above inspector tabs, but it will be hidden when the tab is opened.  
You can toggle portrait display when inspector tab is open and when turned on, you can choose one of 4 places around the window it will occupy.

**Colonist Bar** mode replaces pawn image in colonist bar on the top of the screen with their portrait.

**Top Right** mode is pretty useless. It's still there for back-compatibility.

**Actions** mode places portrait on the right of the inspector panel (the one in bottom left) and can be further adjusted with offset values:  
Left offset: amount of pixels away from the inspector.  
Bottom offset: amount of pixels away from the bottom menu bar.  
By default, portraits of this mode won't change size on mouse hover, but it can be turned on.

**Custom** mode allows you to put portrait in any part of the screen.  
You can specify precise coordinates of an anchor point, and portrait alignment relative to it.  
So if you set alignment point to Bottom Right one, portrait will expand up and left from those coordinates.

Default size and size on hover can be adjusted. If you do not want portrait to get bigger on mouse hover, make it equal to default size.

There's no background, and no border. Draw them on the portrait image if you'd like.

# ![Animals](https://i.postimg.cc/fbjy8PtP/hAnimals.png)

You can set portraits for animal races using a magnifying glass button next to X in animal info card ( i - button).

The portrait will be assigned to the animal def and will be visible when selecting any individual of the same def.

Implemented only for 1.4 and 1.5 versions of the game.

# ![Compatibility](https://i.postimg.cc/3NWwJJSM/h-Compatibility.png)

- **Safe** to add to an existing game.
- **Safe** to remove from an existing game.

Errors may be produced on load without a mod. Those can be ignored.

**Colonist Bar** mode is compatible with:

- [CM Color Coded Mood Bar](https://steamcommunity.com/sharedfiles/filedetails/?id=2006605356)
- [[LTO] Colony Groups](https://steamcommunity.com/sharedfiles/filedetails/?id=2345493945)
- [Pawn Badge Fan Fork [Adopted]](https://steamcommunity.com/sharedfiles/filedetails/?id=2526040241)
- [Job In Bar](https://steamcommunity.com/sharedfiles/filedetails/?id=2086300611)
- [Owl's Colonist Bar (dev)](https://steamcommunity.com/workshop/filedetails/?id=2623453038) (weapons may look funny tho)
- [[NL] Dynamic Portraits](https://steamcommunity.com/sharedfiles/filedetails/?id=2253730555)

Other mods changing colonist bar may or may not work.

Disabled if [[NL] Custom Portraits](https://steamcommunity.com/sharedfiles/filedetails/?id=1569605867) mod is detected.

# ![Screenshots](https://i.postimg.cc/BQLvrbPH/h-Screenshots.png)

HUD: [RimHUD](https://steamcommunity.com/sharedfiles/filedetails/?id=1508850027)  
Apparel, Weapon, Pawn race: [Kurin, The Three Tailed Fox [Deluxe Edition]](https://steamcommunity.com/sharedfiles/filedetails/?id=2670355481)  
Additionally: [[NL] Facial Animation - WIP](https://steamcommunity.com/sharedfiles/filedetails/?id=1635901197), [Camera+](https://steamcommunity.com/sharedfiles/filedetails/?id=867467808), [Graphics Settings+](https://steamcommunity.com/sharedfiles/filedetails/?id=1678847247)  
Portrait images: MidJourney

# ![Similar Mods](https://i.postimg.cc/vHJmby5j/h-Similar-Mods.png)

_Normalize presentation of alternatives!_

- [Bottom Left Portrait](https://steamcommunity.com/sharedfiles/filedetails/?id=2887600947)
- [Colonist Portraits](https://steamcommunity.com/sharedfiles/filedetails/?id=2898119330)
- [Portraits of the Rim](https://steamcommunity.com/sharedfiles/filedetails/?id=2937991425)
- [Avatar](https://steamcommunity.com/sharedfiles/filedetails/?id=3111373293)

# ![Changelog](https://i.postimg.cc/k4T4mtyF/h-Changelog.png)

### 06.08.25

- Tested on 1.6.4535.
- Enabled compatibility for CM Color Coded Mood Bar and[LTO] Colony Groups.
- Verified compatibility with Pawn Badge Fan Fork, Job In Bar, [NL] Dynamic Portraits, [NL] Custom Portraits.
- Added mod setting to remove console spam. (Default: disabled)

#### 12.06.25

- 1.6.**4488** support.

#### 12.09.24

- Source uploaded to GitHub (link in About.xml)
- Reduced mod size to 0.46 Mb (from 0.6 Mb). Now there are no extra files in there.

#### 09.09.24

- **1.3 and 1.4 versions will not receive new features, only bugfixes.**
- Moved portrait button from "Bio" tab to "Health".
- It is now possible to set portraits to corpses, mechanoids and other pawns that have a "Health" tab.

#### 26.06.24

- Changed the way directory is located. Should fix it on Linux. [1.5 only]
- Greatly reduced total mod size. (3.7 Mb to 0.6 Mb)

#### 28.03.24

- Portraits for animals

#### 21.03.24

- 1.5 support

#### 24.01.24

- Close portrait dialog on entering World tab (Planet view)
- Do not draw portraits other than Colonist Bar when on Planet View.

#### 18.01.24

- 1.3 support.
- Better file picker with nested directories support.
- Hotfix for 1.3: TypeLoadException and MissingMethodException fix.

#### 02.06.23

- [[NL] Custom Portraits](https://steamcommunity.com/sharedfiles/filedetails/?id=1569605867) _partial_ compatibility.

#### 20.04.23

- [Owl's Colonist Bar (dev)](https://steamcommunity.com/workshop/filedetails/?id=2623453038) compatibility.

#### 09.04.23

- Option to select different image for each portrait position.
- Fixed issue with [Map Preview](https://steamcommunity.com/sharedfiles/filedetails/?id=2800857642) mod.
- Fixed issue with Toxalopes.

#### 08.04.23

- Refresh button added to settings.
- [CM Color Coded Mood Bar](https://steamcommunity.com/sharedfiles/filedetails/?id=2006605356) compatibility.
- [[LTO] Colony Groups](https://steamcommunity.com/sharedfiles/filedetails/?id=2345493945) compatibility.

#### 04.04.23

- Added **Actions** position mode.
- Allowed for multiple modes to be active at the same time.
- Reorganized mod settings.

#### 03.04.23

- Mod settings added.

#### 02.04.23

- Initial upload.
