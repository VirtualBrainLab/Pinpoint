To define a new probe electrode layout, add a new CSV file.

Imagine you are looking down onto the surface of the probe with the probe shanks pointing down. Start counting electrodes from the bottom-left shank. Go by shank (L->R), then row (B->T), then column (L->R). So elecrtodes 0,1 are the first row, 2,3 the next row, etc.

Provide an x (right), y (up), and z (depth, usually 0) coordinate for the bottom left corner as well as the width, height (going up), and depth of each electrode.

You can confirm that electrodes are correctly placed by turning on the Graphics setting for "Electrodes in scene" (off by default in WebGL). 



To define a new probe selection layer, add a new TXT file. 

The file should have a single line with a 0 or 1 for every electrode channel, indicating whether it is included in the selection. For Neuropixels probes the selection array should sum to 384.

A script is included to convert IMRO files to selection layers.

We include some sensible defaults for NP1/21/24 probes.