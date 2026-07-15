# BRIO - A FFXIV plugin to enhance the GPosing experience. 
[![Build status](https://github.com/Etheirys/Brio/actions/workflows/build.yml/badge.svg)](https://github.com/Etheirys/Brio/actions/workflows/build.yml) ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/Etheirys/Brio/total?style=flat) [![Latest Release](https://img.shields.io/github/v/release/Etheirys/Brio)](https://github.com/Etheirys/Brio/releases/latest) ![GitHub License](https://img.shields.io/github/license/Etheirys/Brio?style=flat) ![Bluesky followers](https://img.shields.io/bluesky/followers/minmoose.bsky.social?style=flat&label=bluesky%20followers) ![Discord](https://img.shields.io/discord/1198316676865867776?label=discord) 

> Brio is currently in BETA, as such, there may be bugs. If you find any, please report them!

## Features
* Full Actor Posing
  * While animating
  * Adjust actor positions without them resetting
  * Overlay and graphical posing modes
  * Inverse Kinematics (IK) posing
  * Full-pose Mirroring/Fliping
  * Import and export poses to a `.pose`
* Actor Management - Creation and Deletion of up to 239 GPose Actors
  * Add/Remove/Blend any animation on a GPose actor (and adjust their speed!)
  * Spawn and attach Companions to actors (minions, mounts, and ornaments)
  * Edit Actor Appearances
  * Swap gear, weapons, and dyes
  * Change the Penumbra collection, Glamourer design & C+ Profile, applied to GPose actors
  * Full character customization (race, tribe, gender, and appearance sliders)
  * Advanced appearance and model shader/color options
  * Add/Remove Status Effects on GPose actors
* Vivacity - Brio's full Keyframe timeline, animate anything! Actors, world objects, cameras, and lights!
  * Keyframe positions, rotations, bones and even full poses, the camera, lights even the colors of them!
  * Play XAT's .xcp camera files
  * Has Bezier interpolation or step interpolation keyframes
* World Object spawning! From weapons, to furniture, or that rock over there. If you can see it, you can spawn it!
  * Spawn props, housing furniture, background world objects, and VFX
  * Browse it all in a searchable, categorized Object Catalog
* Creation of fully customizable lights!
* Create virtual cameras and move them freely beyond GPose's normal limits
  * Includes Free Cameras!
* Creation & Loading of MCDFs that can be loaded onto GPose actors
* Save/Load the entire GPose scenes (actors, world objects, lights, cameras, and environment)
* Browse your character and pose collection with a searchable, tag-based Library!
* Overlay reference images while posing
* Control Time/Weather in both the Overworld and GPose
* Change the active festivals, apply up to 8 at once! (ie, Moonfire Faire for fireworks)

## Installation
### You can install Brio in one of two ways, 

#### 📦 With the [Sea of Stars](https://github.com/Ottermandias/SeaOfStars) **(Recommended)** Custom Dalamud Repository.
  - Type `/xlsettings` in the chat window, then go to the **Experimental** tab.
  - Under the **Custom Plugin Repositories** section, add the following Dalamud repo:
  ```
  https://raw.githubusercontent.com/Ottermandias/SeaOfStars/main/repo.json
  ```
  - Click on the + button & ensure the ***Enabled*** box is checked on the repo.
  - ***Click on the save button in the bottom right***
  - Now open the **Dalamud Plugin Installer** by opening FFXIV's System Menu then pressing ***Dalamud Plugins***
  - In the Search box type `Brio`, find & click on ***Brio*** and then click `Install` after Dalamud has finished installing **Brio**, make sure the *Brio* plugin is Enabled in the Plugin Installer.
  - You now have **Brio** Installed, ***Brio will now open when you are in G-Pose***, you can also type `/brio` in chat to open the Brio Window.

#### 🏗️ With the [World Of Etheirys ](https://github.com/Etheirys/WorldOfEtheirys) Custom Dalamud Repository.
                      
  - Type `/xlsettings` in the chat window, then go to the **Experimental** tab.
  - Under the **Custom Plugin Repositories** section, add the following Dalamud repo:
  ```
  https://raw.githubusercontent.com/Etheirys/WorldOfEtheirys/main/repo.json
  ```
  - Click on the + button & ensure the ***Enabled*** box is checked on the repo.
  - ***Click on the save button in the bottom right***
  - Now open the **Dalamud Plugin Installer** by opening FFXIV's System Menu then pressing ***Dalamud Plugins***
  - In the Search box type `Brio`, find & click on ***Brio*** and then click `Install` after Dalamud has finished installing **Brio**, make sure the *Brio* plugin is Enabled in the Plugin Installer.
  - You now have **Brio** Installed, ***Brio will now open when you are in G-Pose***, you can also type `/brio` in chat to open the Brio Window.

## Support
Brio is still early in development so issues are to be expected.

If you encounter an issue, please either, visit us on the BRIO discord [World of Etheirys Discord](https://discord.gg/GCb4srgEaH ), [Aetherworks Discord](https://discord.gg/KvGJCCnG8t) or open an [issue](https://github.com/Etheirys/Brio/issues)!

## Authors 
**[Minmoose](https://github.com/Minmoose) - Maintainer & Developer.**

**[Asgard](https://github.com/AsgardXIV) - Original Maintainer & Developer.**

**Thank You, to all of our [Contributors](https://github.com/Etheirys/Brio/graphs/contributors)!**

## Acknowledgements
Brio wouldn't be possible without the tireless work of many devs across many projects.

A special thanks goes to:
* [Anamnesis](https://github.com/imchillin/Anamnesis)
* [Dynamis](https://github.com/Exter-N/Dynamis)
* [darkarchon](https://github.com/rootdarkarchon)
* [Ktisis](https://github.com/ktisis-tools/Ktisis)
* [Dalamud](https://github.com/goatcorp/Dalamud/)
* [Penumbra](https://github.com/xivdev/Penumbra)
* [Glamourer](https://github.com/Ottermandias/Glamourer)
* [FFXIVClientStructs](https://github.com/aers/FFXIVClientStructs)
* [VFXEditor](https://github.com/0ceal0t/Dalamud-VFXEditor)
* [Cammy](https://github.com/UnknownX7/Cammy)

Find out more [here](https://github.com/Etheirys/Brio/blob/main/Acknowledgements.md).

## License
Brio is licensed under the [GPL 3.0 license](https://github.com/Etheirys/Brio/blob/main/LICENSE).
