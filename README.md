# Neuropixels Trajectory Planner

This is a tool for planning Neuropixels recordings with up to sixteen 1.0, 2.0, or up to four 4-shank 2.0 probes. Based on the trajectory explorer by Andy Peters https://github.com/petersaj/neuropixels_trajectory_explorer. 

![neuropixels trajectory planner](https://github.com/SteinmetzLab/NPTrajectoryPlanner/raw/main/Images/2021_12_6_v0.1.1.png)

## Known issues

### CCF -> in vivo issues

The CCF coordinates that are returned by this tool are not identical to the target you will get in vivo. The CCF brain appears to be rotated slightly (tilted backward) and squashed along the DV axis (about 95% of in vivo size). Once these issues are resolved Andy and I plan to update both tools to correctly account for this error.

## Install

Download the Windows executable from https://drive.google.com/drive/folders/1QfUc2-Q9fWa_ncNJnP0fzvpdwQkYC3LF?usp=sharing

Run the NPTrajectoryPlanner executable. The 25 um CCF 2017 atlas is included. Email Dan (dbirman@uw.edu) if you need a Mac executable.

## Setting up a probe

To set up a new probe, select the button corresponding to the probe type (NP1/NP2/NP2.4). By default the probe is set to the IBL bregma coordinate (CCF: AP 5.4f, ML 5.739f, DV 0.332f) and the AP/ML distance is relative to that position.

To target a specific brain area it's best to place your probe in that brain area by adjusting the translation first, then adjust the rotations, then drive your probe into the brain -- in the same way that you would do a rea recording. Right now, rotations go **around** the insertion position (see roadmap for details).

### Translation

Use [W/A/S/D] to move the probe to the insertion point. Hold **shift** to move faster along any axis.

Once your probe is at the insertion point, adjust the rotation angles.

### Azimuth

Azimuth is the angle of the probe manipulator relative to the brain. Use [Q/E] to control azimuth.

![Azimuth example](https://github.com/SteinmetzLab/NPTrajectoryPlanner/raw/main/Images/azimuth.gif)

### Elevation

Elevation is the angle of the probe on the manipulator, and is restricted to the range 0 (horizontal) to 90 (vertical). Use [R/F] to control elevation.

![Azimuth example](https://github.com/SteinmetzLab/NPTrajectoryPlanner/raw/main/Images/elevation.gif)

### Depth

Use [Z/X] to insert the probe.

Note that the rotation point is the insertion coordinate with depth==0 (i.e. the point the tip was at before you inserted the probe). I'll change this in a future release, but for now if you rotate the probe after inserting it will rotate around that insertion point.

### Recording region

Once the probe is at the position and angles you want, change the recording region size (in the settings) and position (using [T/G]) to match what you plan to do in your recording and adjust the insertion depth accordingly.

### Export coordinates

Clicking on the coordinates shown at the bottom of the screen copies them to the clipboard.

## Bugs

Please report issues on the Github issues page.

## Roadmap

**[0.1]** Parity with the existing allenCCF Matlab tools (see: https://github.com/cortex-lab/allenCCF) with additional tools for multiple probes and collisions

[0.2] Lock probe to area, spawn probes in default positions, add additional rig parts

[0.3] Search for automated collision solutions with implant area constraints for <=4 probes

Detailed development items are on the project page: https://github.com/SteinmetzLab/NPTrajectoryPlanner/projects

## References

CCF Atlas downloaded from http://download.alleninstitute.org/informatics-archive/current-release/mouse_ccf/annotation/ 
