import logging
import tensorflow as tf
import numpy as np
from tensorflow.python.client import device_lib
from tensorflow.keras.layers import MaxPool2D, Dropout
from sklearn import preprocessing
from sklearn.model_selection import train_test_split
import matplotlib.pyplot as plt
from tensorflow.keras import Model
from tensorflow.keras.layers import Softmax
from tensorflow.keras.layers import BatchNormalization
from datetime import datetime
from numpy import savetxt
import random
from tensorflow.python.keras import backend as K
from tensorflow import keras

def split_input_target(chunk):
    input_text = chunk[:-1]
    target_text = chunk[1:]
    return input_text, target_text

def build_model():
    model = tf.keras.Sequential()
    model.add(tf.keras.layers.InputLayer(input_shape=[30,2]))
    model.add(tf.keras.layers.LSTM(32, return_sequences=True))
    model.add(tf.keras.layers.Dense(2))
    model.compile(loss='mae', optimizer='adam')
    return model

f=open("ucy/zara/zara01/pixel_pos_interpolate.csv", "r")
fl=f.readlines()
for line in fl:
    line.replace("\n", "")
frameids=fl[0].split(",")
frameids=[int(i) for i in frameids]
personids=fl[1].split(",")
personids=[int(i) for i in personids]
pixel_pos_x=fl[2].split(",")
pixel_pos_x=[float(i) for i in pixel_pos_x]
pixel_pos_y=fl[3].split(",")
pixel_pos_y=[float(i) for i in pixel_pos_y]
maxpersonid=max(personids)
dataset_raw = [[] for j in range(maxpersonid)]
for i in range(1, maxpersonid+1):
    for j in range(0, len(personids)):
        if personids[j] == i:
            dataset_raw[i-1].append([pixel_pos_x[j], pixel_pos_y[j]])
seq_len = 31
batch_size = 64
full_dataset = []
for i in range(0, len(dataset_raw)):
    index = 0
    while(index+seq_len <= len(dataset_raw[i])):
        for j in range(index, index+seq_len):
            full_dataset.append(dataset_raw[i][j])
        index = index+seq_len
sequences = tf.data.Dataset.from_tensor_slices(np.array(full_dataset)).batch(seq_len, drop_remainder=True)
dataset = sequences.map(split_input_target)
batched_dataset = dataset.shuffle(len(full_dataset)).batch(batch_size, drop_remainder=True)
lstm = build_model()
history = lstm.fit(batched_dataset, epochs=200)
for input, target in batched_dataset.shuffle(10).take(1):
    print(input)
    pred=lstm(input)
    for i in range(0, 10):
        print(pred[i])
        print(target[i])
while True:
    rand_x = random.randint(-500, 500)/1000.0
    rand_y = random.randint(-500, 500)/1000.0
    seq = [[rand_x, rand_y]]
    seq_data = tf.data.Dataset.from_tensor_slices(np.array(seq)).batch(2).batch(1)
    randomSample = random.randint(0, len(dataset_raw)-1)
    seqIndex = 0
    #for i in range(0, len(dataset_raw[randomSample])):
    for i in range(0, 200):
        for input in seq_data.take(1):
            print(input)
            pred = lstm(input)
            print(pred)
        seq.append(pred[0][len(pred[0])-1])
        tmpSeq = []
        if len(seq) < 30:
            seq_data = tf.data.Dataset.from_tensor_slices(np.array(seq)).batch(len(seq)).batch(1)
        else:
            for i in range(seqIndex, seqIndex+30):
                tmpSeq.append(seq[seqIndex])
            seqIndex += 1
            seq_data = tf.data.Dataset.from_tensor_slices(np.array(tmpSeq)).batch(len(tmpSeq)).batch(1)
    outArray = []
    seq_data = tf.data.Dataset.from_tensor_slices(np.array(seq)).batch(len(seq)).batch(1)
    for input in seq_data.take(1):
        outArray = tf.make_ndarray(tf.make_tensor_proto(input))
    outArray = outArray[0]
    x = []
    y = []
    xt = []
    yt = []
    for point in outArray:
        x.append(point[0])
        y.append(point[1])
    for point in dataset_raw[randomSample]:
        xt.append(point[0])
        yt.append(point[1])
    plt.plot(x, y, 'o')
    #plt.plot(xt, yt, 'ro')
    plt.axis([-1,1,-1,1])
    plt.show()
    #lstm.save("save.h5")
    #lstm.save("save", save_format="tf")