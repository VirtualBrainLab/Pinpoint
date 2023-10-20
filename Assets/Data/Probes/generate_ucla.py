import pandas as pd
import numpy as np

# 128K

# electrodes are in a l/c/r three column set, l/r are identical

n_elec_lr = 10
lr_start_y = 50
lr_pitch_x = 25

n_elec_c = 12
c_start_y = 15
c_start_y_high = 15+10+15+10+15

pitch_y = 15+10


columns = ['electrode','x','y','z','w','h','d','default','all']
electrodes = pd.DataFrame(np.zeros((n_elec_lr*2 + n_elec_c, len(columns))), columns=columns)

# electrode # ordering is a bit weird, it's 0/1 on center, then 2l 3r 4c, etc
electrodes.iloc[0] = [0, -5, c_start_y, 0, 10, 10, 23, 1, 1]
electrodes.iloc[1] = [1, -5, c_start_y+pitch_y, 0, 10, 10, 23, 1, 1]

for ei in np.arange(0,30):
    type = np.mod(ei,3)
    row = np.floor(ei / 3)
    
    if type == 0:
        # left
        electrodes.iloc[ei+2] = [ei+2, -22.5, lr_start_y + row * pitch_y, 0, 10, 10, 23, 1, 1]

    elif type == 1:
        # right
        electrodes.iloc[ei+2] = [ei+2, 12.5, lr_start_y + row * pitch_y, 0, 10, 10, 23, 1, 1]

    elif type == 2:
        # center
        electrodes.iloc[ei+2] = [ei+2, -5, c_start_y_high + row * pitch_y, 0, 10, 10, 23, 1, 1]

electrodes.to_csv('./ucla_128k.csv', index=False)


# 256F

# electrodes are in a l/c/r three column set, l/r are identical

n_elec_lr = 42
lr_start_y = 75
lr_pitch_x = 20

n_elec_c = 44
c_start_y = 25
c_start_y_high = 55

pitch_y = 50


columns = ['electrode','x','y','z','w','h','d','default','all']
electrodes = pd.DataFrame(np.zeros((n_elec_lr*2 + n_elec_c, len(columns))), columns=columns)

# electrode # ordering is a bit weird, it's 0/1 on center, then 2l 3r 4c, etc
electrodes.iloc[0] = [0, -5, 30, 0, 10, 10, 23, 1, 1]
electrodes.iloc[1] = [1, -5, 45, 0, 10, 10, 23, 1, 1]

for ei in np.arange(0,126):
    type = np.mod(ei,3)
    row = np.floor(ei / 3)
    
    if type == 0:
        # center
        electrodes.iloc[ei+2] = [ei+2, -5, c_start_y_high + row * pitch_y, 0, 10, 10, 23, 1, 1]

    elif type == 1:
        # left
        electrodes.iloc[ei+2] = [ei+2, -20, lr_start_y + row * pitch_y, 0, 10, 10, 23, 1, 1]

    elif type == 2:
        # right
        electrodes.iloc[ei+2] = [ei+2, 10, lr_start_y + row * pitch_y, 0, 10, 10, 23, 1, 1]

electrodes.to_csv('./ucla_256f.csv', index=False)