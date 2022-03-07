# Neuropixels Trajectory Planner

This is a tool for planning Neuropixels recordings with up to sixteen 1.0, 2.0, or up to four 4-shank 2.0 probes.

![Azimuth example](https://github.com/dbirman/NPTrajectoryPlanner/raw/main/Images/2021_12_6_v0.1.1.png)

## Known issues

### CCF -> in vivo issues

The CCF coordinates returned by this tool are not identical to the in vivo mouse brain. We know for sure that the CCF atlas is stretched along the DV axis (in vivo = 0.952 * CCF) and squashed on the AP axis (in vivo 1.087 * CCF). In addition it appears the lamda-bregma angle in the CCF space is rotated by about 5 degrees. These differences are **not** currently implemented in the trajectory planner.

In release v0.6 we will add the **s**tereo**t**axic coordinates as a settings option.

## Install

The easiest way to use the trajectory planner is through our web app: http://data.virtualbrainlab.org/NPTrajectoryPlanner/

### Standalone builds

You can also download a Desktop build from the ![releases page](https://github.com/dbirman/NPTrajectoryPlanner/releases).

While we develop this application only the Windows build is guaranteed to be stable. If you need a build for linux, mac, we can build them for you on request.

<!-- ### Additional linux instructions

To run the linux executable you need to go to the unzipped folder and run `chmod +x` on the .x86_64 file. Some users may run into permissions issues, in which case running ` chown -R yourusername .` from within the folder should repair those. -->

<!-- ### Additional mac instructions

The mac executable currently only runs on MacOS **Mojave** and earlier. You will probably have a security issue because the app is unsigned. Go to Systems Preferences > Security & Privacy > General and allow the file to "run anyway".  -->

## Instructions for use

To set up a new probe, select the button in the bottom right corresponding to the probe type (NP1/NP2/NP2.4). By default the probe's (0,0,0) coordinate is set to the IBL bregma coordinate (CCF: AP 5.4f, ML 5.739f, DV 0.332f).

Note that when targeting a brain region the rotations go **around** the insertion point on the brain surface. 

At any time you can press [M] to open the manual coordinate entry window and adjust the probe position by hand. 

### Translation

Use [W/A/S/D] to move the probe along the AP or ML axis. Hold **shift** to move faster along any axis.

### Azimuth

Azimuth is the angle of the probe manipulator relative to the brain. Use [Q/E] to control azimuth.

(note the az example is out of date, I will update soon)
![Azimuth example](https://github.com/dbirman/NPTrajectoryPlanner/raw/main/Images/azimuth.gif)

### Elevation

Elevation is the angle of the probe on the manipulator, and is restricted to the range 0 (vertical) to 90 (horizontal). Use [R/F] to control elevation.

(note the elevation example is out of date, I will update soon)
![Azimuth example](https://github.com/dbirman/NPTrajectoryPlanner/raw/main/Images/elevation.gif)

### Depth

Use [Z/X] to insert the probe.

Note that the rotation point is the insertion coordinate with depth==0 (i.e. the point the tip was at before you inserted the probe). I'll change this in a future release, but for now if you rotate the probe after inserting it will rotate around that insertion point.

### Spin

Use [1/3] to spin the probe along the axis of the probe shank. Note that NP2.4 probes spin around the leftmost probe shank.

### Recording region

Once the probe is at the position and angles you want, change the recording region size (in the settings) and position (using [T/G]) to match what you plan to do in your recording and adjust the insertion depth accordingly.

### Export coordinates

Clicking on the coordinates shown at the bottom of the screen copies them to the clipboard. If the azimuth angle is not at 0/90/180 use the convert AP/ML to probe setting to export the position along the probe forward/side axis.

### Active probe

When setting up multi-probe insertions you can click the probe panel, the probe model, or the probe coordinate text to set that probe to active. Pressing [Backspace] removes the active probe. 

## Using coordinates for surgery

To use your coordinates for a surgery, rotate the manipulator to match the azimuth angle and set the probe elevation angle. Then move your probe tip (the left-most probe tip on a 2.4 when facing upwards) to Bregma and zero your manipulator.

To translate the probe you can either use the AP/ML coordinates (if you kept the azimuth angle at a multiple of 90 degrees) or you can turn on the "Convert AP/ML to probe axis" setting, which exports the translation coordinates in the probe's forward and side axes.

To insert the probe, drive the probe forward until the tip touches the brain, zero the depth axis, and continue forward at a slow speed until you reach the specified depth.

In a future release we will add a display of the expected channel activity for a live recording, to help ensure accurate targeting. Coming soon!

## Saving and loading probes

You can save the coordinates of an insertion for future use by clicking on the coordinate text. This copies the active probe coordinates to a string. This string can also be used to manually position a probe by pressing [M] and entering the string (coming in v0.6!). 

## Settings

**Reset active probe** - Returns the active probe to the original starting position (Bregma by default)

**Spawn IBL probes** - Places two NP1.0 probes in the configuration used by the IBL

**Probe collisions** - Prevents probe shanks and holders from intersecting when moved

**Set (0,0,0) to Bregma** - On: smpets the 0,0,0 coordinate to Bregma, or to the 0,0,0 CCF coordinate when unchecked (default: on)

**Depth from brain surface** - On: measures insertion depth from the brain surface, Off: from the DV=0 plane (default: on)

**Sagittal/Coronal slices** - On: displays slices aligned to the probe tip (default: off)

**Display area acronyms** - On: show only acronyms for brain areas (default: off)

**Areas include layers** - On: include the layers (e.g. in cortex) (default: on)

**Rig** - Dropdown with options for rigs to display

**Recording region only** - On: display only the areas within the recording region, Off: show the areas along the whole probe shank (default: on)

**Recording region size** - Slider controls the size of the recording region. Defaults to a set of options that depend on the probe.

**Display In-Plane Slice** - On: shows a slice of cortex that is "in-plane" with the probe, note that this uses a camera that is looking at the probe from the front of the brain towards the back (default: on)

**Convert AP/ML to probe** - On: when the probe manipulator is off of the 0/90 axis the AP/ML positions are not useful for calculating the insertion point relative to Bregma, turn this on to display the position along the probe forward/side axes (default: off)

## Bugs

Please report issues on the [issues page](https://github.com/dbirman/NPTrajectoryPlanner/issues).

## References

Based on the trajectory explorer by Andy Peters https://github.com/petersaj/neuropixels_trajectory_explorer. 
CCF Atlas downloaded from http://download.alleninstitute.org/informatics-archive/current-release/mouse_ccf/annotation/ 
