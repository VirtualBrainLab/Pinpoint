import pandas as pd
import numpy as np

n_electrodes = 960
n_cols = 2

columns = ['electrode','x','y','z','w','h','d','default','all','bank0','double_length']
# neuropixels 1.0 1-shank
electrodes = pd.DataFrame(np.zeros((n_electrodes, len(columns))), columns=columns)

# assume for now that the tip length is 200 um (they say 175, but it looks more like 200)
tip_length = 175

pitch_row = 20
pitch_col = 32

# the first column starts to the left of the tip (since the tip is chisel-shaped)
# I'm not sure if it's right, but the probes appear to be 60 um wide in the electrode region, we'll center at 30

# odd rows start at 12um + 2um left of center
# even rows start at 16um + 12 um left of center
col_start = [-(12 + 2), -(16 + 12 +2)]

width = 12
height = 12
depth = 24

# only one shank
count = 0

# row
for r in np.arange(n_electrodes/n_cols):
    y_pos = tip_length + r * pitch_row
    # column
    for c in np.arange(n_cols):
        x_pos = col_start[int(np.mod(r, 2))] + c * pitch_col

        # also compute the selection layers
        bank0 = count < 384
        bank0odd_bank1even = 1 if (c==1 and count < 384) or (c==0 and count >= 384 and count < 768) else 0

        # set a default selection layer and an all layer
        default = bank0
        all = True

        electrodes.iloc[count] = [count, x_pos, y_pos, 0, width, height, depth, default, all, bank0, bank0odd_bank1even]
        count += 1

electrodes.to_csv('./neuropixels_1.csv', index=False)


# neuropixels 2.0 4-shank // we're just going to generate the channel map for a single shank
n_electrodes = 1280
n_cols = 2

columns = ['electrode','x','y','z','w','h','d','default','all','bank0','double_length','bank1','bank2','bank3','bank4']
electrodes = pd.DataFrame(np.zeros((n_electrodes, len(columns))), columns=columns)

tip_length = 175

pitch_col = 32
pitch_row = 15

# the first row starts *above* the tip (since the tip is chisel-shaped)
col_start = -30

width = 12
height = 12
depth = 24

count = 0

# # shank
# for s in np.arange(n_shanks):

# row
for r in np.arange(n_electrodes/n_cols):
    y_pos = tip_length + r * pitch_row
    # column
    for c in np.arange(n_cols):
        x_pos = col_start + c * pitch_col

        # compute selection layers
        bank0 = count < 96
        double_length = 1 if (c==1 and count < 96) or (c==0 and count >= 96 and count < (96*2)) else 0
        bank1 = count >= 96 and count < (96*2)
        bank2 = count >= (96*2) and count < (96*3)
        bank3 = count >= (96*3) and count < (96*4)
        bank4 = count >= (96*4) and count < (96*5)

        default = bank0
        all = True

        electrodes.iloc[count] = [count, x_pos, y_pos, 0, width, height, depth, default, all,
                                  bank0, double_length, bank1, bank2, bank3, bank4]
        count += 1

electrodes.to_csv('./neuropixels_2.csv', index=False)