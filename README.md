# PvP Tracker
<img src="https://raw.githubusercontent.com/wrath16/PvpStats/master/images/icon.png" width="256" height="256">

[![Download count](https://img.shields.io/endpoint?url=https://qzysathwfhebdai6xgauhz4q7m0mzmrf.lambda-url.us-east-1.on.aws/PvpStats)](https://github.com/wrath16/PvpStats)

Final Fantasy XIV Dalamud plugin for recording PvP match history.

## Examples
![image](https://raw.githubusercontent.com/wrath16/PvpStats/master/images/example1.PNG)
![image](https://raw.githubusercontent.com/wrath16/PvpStats/master/images/example2.PNG)
![image](https://raw.githubusercontent.com/wrath16/PvpStats/master/images/example3.PNG)

## Usage Instructions
* Install from main Dalamud repo.
* Matches are recorded automatically.
* Enter `/ccstats` to open the Crystalline Conflict stats window.
*  Enter `/flstats` to open the Frontline stats window.
*  Enter `/rwstats` to open the Rival Wings stats window.
* Enter `/pvpstatsconfig` or press the gear on the plugin description to access various settings.

## Known Issues
* Spectated Crystalline Conflict matches are not recorded.
* Rematches in Crystalline Conflict custom matches are not recorded.
* Rival Wings matches that end between 14:51 and 14:59 have skewed match timeline timestamps by a few seconds.
* Rival Wings matches recorded before v2.3.0.0 may have incorrect merc counts.
* Rival Wings matches recorded prior to game version 7.0 may have incorrect ceruleum counts for players with >255.
* Rival Wings matches recorded during game version 7.2 and prior to v2.3.4.1 have incorrect alliance Soaring stacks.
* Text may be clipped in some cases if using non-standard font settings.

## Feature Roadmap
May or may not eventually get implemented:
* More stats.
* More localization.
* UI improvements.
* Performance and reliability improvements.
