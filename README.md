# Neuropixels Trajectory Planner

This is a tool for planning Neuropixels recordings with up to sixteen 1.0, 2.0, or up to four 4-shank 2.0 probes. Based on the trajectory explorer by Andy Peters https://github.com/petersaj/neuropixels_trajectory_explorer. 

![Azimuth example](https://github.com/dbirman/NPTrajectoryPlanner/raw/main/Images/2021_12_6_v0.1.1.png)

## Known issues

### CCF -> in vivo issues

The CCF coordinates that are returned by this tool are not identical to the target you will get in vivo. The CCF brain appears to be rotated slightly (tilted backward) and squashed along the DV axis (about 95% of in vivo size). Once these issues are resolved Andy and I plan to update both tools to correctly account for this error.

## Install

Download the most recent version from the ![releases page](https://github.com/dbirman/NPTrajectoryPlanner/releases).

Currently we are only building releases for Windows and Linux using the 25 um CCF 2017 atlas. If you need a Mac executable or a version using the 10um atlas please email Dan (dbirman@uw.edu).

### Additional linux instructions

To run the linux executable you need to go to the unzipped folder and run `chmod +x` on the .x86_64 file. Some users may run into permissions issues, in which case running ` chown -R yourusername .` from within the folder should repair those.

## Setting up a probe

To set up a new probe, select the button corresponding to the probe type (NP1/NP2/NP2.4). By default the probe is set to the IBL bregma coordinate (CCF: AP 5.4f, ML 5.739f, DV 0.332f) and the AP/ML distance is relative to that position.

To target a specific brain area it's best to place your probe in that brain area by adjusting the translation first, then adjust the rotations, then drive your probe into the brain -- in the same way that you would do a rea recording. Right now, rotations go **around** the insertion position (see roadmap for details).

### Translation

Use [W/A/S/D] to move the probe to the insertion point. Hold **shift** to move faster along any axis.

Once your probe is at the insertion point, adjust the rotation angles.

### Azimuth

Azimuth is the angle of the probe manipulator relative to the brain. Use [Q/E] to control azimuth.

![Azimuth example](https://github.com/dbirman/NPTrajectoryPlanner/raw/main/Images/azimuth.gif)

### Elevation

Elevation is the angle of the probe on the manipulator, and is restricted to the range 0 (horizontal) to 90 (vertical). Use [R/F] to control elevation.

![Azimuth example](https://github.com/dbirman/NPTrajectoryPlanner/raw/main/Images/elevation.gif)

### Depth

Use [Z/X] to insert the probe.

Note that the rotation point is the insertion coordinate with depth==0 (i.e. the point the tip was at before you inserted the probe). I'll change this in a future release, but for now if you rotate the probe after inserting it will rotate around that insertion point.

### Spin

Use [1/3] to spin the probe along the axis of the probe shank. Note that NP2.4 probes spin around the leftmost probe shank.

### Recording region

Once the probe is at the position and angles you want, change the recording region size (in the settings) and position (using [T/G]) to match what you plan to do in your recording and adjust the insertion depth accordingly.

### Export coordinates

Clicking on the coordinates shown at the bottom of the screen copies them to the clipboard. If the azimuth angle is not at 0 use the convert AP/ML to probe setting to export the position along the probe forward/side axis.

### Using coordinates for surgery

To use your coordinates for a surgery, rotate the manipulator to match the azimuth angle and set the probe elevation angle. Then move your probe tip (the left-most probe tip on a 2.4 when facing upwards) to Bregma and zero your manipulator. Move the probe along its forward and side axes according to the exported coordinates (or along AP/ML if you kept azimuth at 0). Assuming you exported your coordinates starting at the brain surface, drive the probe forward until the tip touches the brain, zero the depth axis, and continue forward until you reach the pre-specified depth.

In v0.3 we will release tools that display what the expected channel activity should look like when you performing a live recording, to help ensure accurate targeting. Coming soon!

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

CCF Atlas downloaded from http://download.alleninstitute.org/informatics-archive/current-release/mouse_ccf/annotation/ 
