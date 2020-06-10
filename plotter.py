import numpy as np
import matplotlib.pyplot as plt
from datetime import datetime
from numpy import savetxt
import random

f=open("out.csv", "r")
fl=f.readlines()
for line in fl:
    line = line.replace("\n", " ")
trajectories = []
for i in range(0, int(round(len(fl)/2))):
    index = i*2
    trajectories.append([fl[index].split(","), fl[index+1].split(",")])
for element in trajectories:
    x = []
    y = []
    for f in element[0]:
        x.append(float(f))
    for f in element[1]:
        y.append(float(f))
    plt.plot(x, y, "o")
    plt.axis([-1,1,-1,1])
    plt.show()