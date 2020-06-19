using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.Sensors;
using UnityEditor;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

public class CleverPedestrianAgent : Agent
{
    public struct Coords
    {
        public Coords(float x, float y)
        {
            X = x;
            Y = y;
        }
        public float X { get; }
        public float Y { get; }
    }

    private List<Coords>[] pid;
    public float FieldX = 20.0f;
    public float FieldZ = 20.0f;
    public float speed = 0.05f;
    public Transform target;
    private Vector3 lastTransform;
    private Vector3 bestTransform; 
    private GameObject[] pedestrians;
    private int[] pIndex;
    private int[] cIndex;
    private bool[] inv;
    public float rangeMult = 10.0f;
    private float negRew;
    private StreamWriter writer1;
    private StreamWriter writer2;
    public bool writeTrajectories = false;
    private List<Coords> steps1;
    private List<Coords> steps2;
    void Start()
    {
        pedestrians = GameObject.FindGameObjectsWithTag("pedestrian");
        pIndex = new int[pedestrians.Length];
        cIndex = new int[pedestrians.Length];
        inv = new bool[pedestrians.Length];
        int counter = 0;
        string line;
        string[] s_file = new string[4];
        System.IO.StreamReader file = new System.IO.StreamReader("Assets/pixel_pos_interpolate_hotel.csv");
        if(writeTrajectories)
        {
            writer1 = new StreamWriter("Assets/out1.csv");
            writer2 = new StreamWriter("Assets/out2.csv");
            steps1 = new List<Coords>();
            steps2 = new List<Coords>();
        }
        while ((line = file.ReadLine()) != null)
        {
            s_file[counter] = line;
            s_file[counter] = s_file[counter].Replace("\n", "");
            counter++;
        }
        file.Close();
        string[] s_pid = s_file[1].Split(',');
        string[] s_xc = s_file[2].Split(',');
        string[] s_yc = s_file[3].Split(',');
        int maxId = 0;
        for (int i = 0; i < s_pid.Length; i++)
        {
            if (Int32.Parse(s_pid[i]) > maxId)
            {
                maxId = Int32.Parse(s_pid[i]);
            }
        }
        pid = new List<Coords>[maxId];
        for (int i = 0; i < s_pid.Length; i++)
        {
            if(Int32.Parse(s_pid[i]) != 0)
            {
                if (pid[Int32.Parse(s_pid[i]) - 1] == null)
                {
                    pid[Int32.Parse(s_pid[i]) - 1] = new List<Coords>();
                }
                pid[Int32.Parse(s_pid[i]) - 1].Add(new Coords(float.Parse(s_xc[i]), float.Parse(s_yc[i])));
            }
        }
        
    }

    public override void OnEpisodeBegin()
    {
        int firstRand = 0;
        do
        {
            firstRand = UnityEngine.Random.Range(0, pid.Length);
        } while (pid[firstRand] == null);
        float spawnX = pid[firstRand][0].X * (FieldX + rangeMult);
        float spawnZ = pid[firstRand][0].Y * (FieldX + rangeMult);
        float goalX = pid[firstRand][pid[firstRand].Count - 1].X * (FieldX + rangeMult);
        float goalZ = pid[firstRand][pid[firstRand].Count - 1].Y * (FieldX + rangeMult);
        this.transform.localPosition = new Vector3(spawnX, 1.25f, spawnZ);
        if (writeTrajectories)
        {
            steps1 = new List<Coords>();
            steps2 = new List<Coords>();
            for (int i = 0; i < pid[firstRand].Count; i++)
            {
                steps2.Add(new Coords(pid[firstRand][i].X, pid[firstRand][i].Y));
            }
            steps1.Add(new Coords(this.transform.localPosition.x / (FieldX + rangeMult), this.transform.localPosition.z / (FieldZ + rangeMult)));
        }
        target.localPosition = new Vector3(goalX, 0.5f, goalZ);
        bestTransform = this.transform.localPosition;
        negRew = 0.0f;
        List<int> randomValues = new List<int>();
        for(int i = 0; i<pedestrians.Length; i++)
        {
            int randomVal = 0;
            do
            {
                randomVal = UnityEngine.Random.Range(0, pid.Length);
            } while ((!(randomValues.Count == 0) && randomValues.Contains(randomVal)) || pid[randomVal] == null || (i < pedestrians.Length/2 && Math.Abs(pid[randomVal][0].X * (FieldX + rangeMult) - spawnX) < 1 && Math.Abs(pid[randomVal][0].Y * (FieldX + rangeMult) - spawnZ) < 1) || (i >= pedestrians.Length / 2 && Math.Abs(pid[randomVal][0].Y * (FieldX + rangeMult) - spawnX) < 1 && Math.Abs(pid[randomVal][0].X * (FieldX + rangeMult) - spawnZ) < 1));
            randomValues.Add(randomVal);
            pIndex[i] = randomVal;
            cIndex[i] = 0;
            if(i < pedestrians.Length/2)
            {
                pedestrians[i].transform.localPosition = new Vector3(pid[pIndex[i]][cIndex[i]].X * (FieldX + rangeMult), 1.25f, pid[pIndex[i]][cIndex[i]].Y * (FieldX + rangeMult));
            }
            else
            {
                pedestrians[i].transform.localPosition = new Vector3(pid[pIndex[i]][cIndex[i]].Y * (FieldX + rangeMult), 1.25f, pid[pIndex[i]][cIndex[i]].X * (FieldX + rangeMult));
            }
            cIndex[i]++;
            inv[i] = true;
        }
        /*
        float spawnX = UnityEngine.Random.Range(-18.0f, 18.0f);
        this.transform.localPosition = new Vector3(spawnX, 1.25f, -18);
        if (writeTrajectories)
        {
            steps = new List<Coords>();
            steps.Add(new Coords(this.transform.localPosition.x / FieldX, this.transform.localPosition.z / FieldZ));
        }
        target.localPosition = new Vector3(-spawnX, 0.5f, 18);
        bestTransform = this.transform.localPosition;
        negRew = 0.0f;
        */
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(new Vector2(this.transform.localPosition.x / FieldX, this.transform.localPosition.z / FieldZ));
        sensor.AddObservation(new Vector2(target.localPosition.x / FieldX, target.localPosition.z / FieldZ));
        //sensor.AddObservation(StepCount / (float)maxStep);
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("pedestrian"))
        {
            SetReward(-0.2f);
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("wall"))
        {
            SetReward(-0.2f);
            EndEpisode();
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        if (StepCount % 1 == 0) {
            for (int i = 0; i < pedestrians.Length; i++)
            {
                if (i < pedestrians.Length / 2)
                {
                    pedestrians[i].transform.localPosition = new Vector3(pid[pIndex[i]][cIndex[i]].X * (FieldX + rangeMult), 1.25f, pid[pIndex[i]][cIndex[i]].Y * (FieldX + rangeMult));
                }
                else
                {
                    pedestrians[i].transform.localPosition = new Vector3(pid[pIndex[i]][cIndex[i]].Y * (FieldX + rangeMult), 1.25f, pid[pIndex[i]][cIndex[i]].X * (FieldX + rangeMult));
                }
                if (pid[pIndex[i]].Count - 1 == cIndex[i] || cIndex[i] == 0)
                {
                    inv[i] = !inv[i];
                }
                if (inv[i])
                {
                    cIndex[i]++;
                }
                else
                {
                    cIndex[i]--;
                }
            }
        }
        //negRew = -0.0002f;
        //UnityEngine.Debug.Log(negRew);
        //AddReward(negRew);
        Vector2 correctVector = new Vector2(target.localPosition.x, target.localPosition.z) - new Vector2(this.transform.localPosition.x, this.transform.localPosition.z);
        correctVector = correctVector.normalized;
        lastTransform = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, this.transform.localPosition.z);
        Vector2 dirVec = new Vector2(vectorAction[0], vectorAction[1]);
        dirVec = dirVec.normalized;
        float correctnessValue = Vector2.Dot(dirVec, correctVector);
        Vector3 toSum = new Vector3(dirVec.x*speed, 0.0f, dirVec.y*speed);
        //this.transform.localPosition = new Vector3(vectorAction[0] * FieldX, 1.25f, vectorAction[1] * FieldZ);
        this.transform.localPosition = new Vector3(this.transform.localPosition.x + toSum.x, 1.25f, this.transform.localPosition.z + toSum.z);
        /*
        if (Vector3.Distance(this.transform.localPosition, lastTransform) < 0.1f)
        {
            AddReward(0.01f);
        }
        */
        if (writeTrajectories)
        {
            steps1.Add(new Coords(this.transform.localPosition.x/(FieldX + rangeMult), this.transform.localPosition.z/(FieldZ + rangeMult)));
        }
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, target.localPosition);
        if (distanceToTarget < Vector3.Distance(bestTransform, target.localPosition))
        {
            //AddReward(0.001f);
            if(correctnessValue > 0.0f)
            {
                correctnessValue = Mathf.Pow(correctnessValue, 4.0f);
            }
            AddReward(correctnessValue * 0.001f);
            bestTransform = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, this.transform.localPosition.z);
        }
        else 
        {
            if (correctnessValue < 0.0f)
            {
                AddReward(correctnessValue * 0.001f);
            }
            AddReward(-0.0002f);
        }
        if (distanceToTarget < 2.0f)
        {
            SetReward(1.0f);
            if (writeTrajectories)
            {
                string xString = "";
                string yString = "";
                for(int i = 0; i < steps1.Count; i++)
                {
                    if(i==0)
                    {
                        xString = xString + steps1[i].X;
                        yString = yString + steps1[i].Y;
                    }
                    else
                    {
                        xString = xString + "," + steps1[i].X;
                        yString = yString + "," + steps1[i].Y;
                    }
                }
                writer1.WriteLine(xString);
                writer1.WriteLine(yString);
                writer1.Flush();
                xString = "";
                yString = "";
                for (int i = 0; i < steps2.Count; i++)
                {
                    if (i == 0)
                    {
                        xString = xString + steps2[i].X;
                        yString = yString + steps2[i].Y;
                    }
                    else
                    {
                        xString = xString + "," + steps2[i].X;
                        yString = yString + "," + steps2[i].Y;
                    }
                }
                writer2.WriteLine(xString);
                writer2.WriteLine(yString);
                writer2.Flush();
            }
            EndEpisode();
        }

    }

    public override float[] Heuristic()
    {
        /*
        float[] actionsOut = new float[2];
        actionsOut[0] = this.transform.localPosition.x;
        actionsOut[1] = this.transform.localPosition.z;
        if (Input.GetKey(KeyCode.D))
        {
            actionsOut[0] = this.transform.localPosition.x + 0.1f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            actionsOut[1] = this.transform.localPosition.z + 0.1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            actionsOut[0] = this.transform.localPosition.x - 0.1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            actionsOut[1] = this.transform.localPosition.z - 0.1f;
        }
        return actionsOut;
        */
        float[] actionsOut = new float[2];
        actionsOut[0] = 0.0f;
        actionsOut[1] = 0.0f;
        if (Input.GetKey(KeyCode.D))
        {
            actionsOut[0] = 1.0f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            actionsOut[1] = 1.0f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            actionsOut[0] = -1.0f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            actionsOut[1] = -1.0f;
        }
        return actionsOut;
    }
}
